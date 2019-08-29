//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using Ease.Util.Disposably;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ChangeTracking;
using Microsoft.Azure.Cosmos.Table;
using Ease.Repository.AzureTable.Collections.Generic;
using System.Collections.Concurrent;

namespace Ease.Repository.AzureTable
{
    /// <summary>
    /// Implementation of the unit of work for AzureTable repository pattern.
    /// </summary>
    public class AzureTableUnitOfWork<TContext> : SafeDisposable, IBestEffortUnitOfWork
    {
        private class EntityBookKeeping
        {
            /// <summary>
            /// Counter of how many edits have been made to the entity (so that we can know how many placeholder
            /// _pendingActions to skip past before rolling up the changes and sending a true final update to the store)
            /// </summary>
            public int EditCount = 0;

            /// <summary>
            /// If we've actually sent the edits to the repo, then this will hold a copy of the original in case
            /// we need to undo the changes.
            /// </summary>
            public object CopyOfOriginalForUndo;

            /// <summary>
            /// Remember the specific handler for this entity so we can gracefully unregister the handler later
            /// </summary>
            public PropertyChangedEventHandler ChangeEventHandler;
        }

        // TODO: Expose ability to Linq query 1st-level cache of entities (i.e. so that repository implementations
        // can avoid running back to the store and getting stale data if we've got items that may be edited, etc...)

        // TODO: Sort out how to handle batching of like operations (if they're grouped together in sequence)

        // TODO: CONSIDER: On Begin(...) unit of work, provide IEnumerable<> of repositories that will then 
        // pre-configure the various handlers so that things like RegisterAdd|Update|Delete operations don't have to 
        // take them... and a mix of repos can be provided... some with async support, some without, some with 
        // batch operation support, some without...!!   and UnitOfWork itself can be injected into repositories
        // !!!!!! actually, expose methods on the IUnitOfWork for registration of "this" so that no explicit 
        // Begin() is needed..!

        private readonly ConcurrentDictionary<Type, IStoreWriter> _storeWriters = new ConcurrentDictionary<Type, IStoreWriter>();

        private readonly MapOfHashSets<Type, object> _addedEntities = new MapOfHashSets<Type, object>();
        private readonly MapOfHashSets<Type, object> _deletedEntities = new MapOfHashSets<Type, object>();

        private readonly Queue<Func<Task>> _pendingActions = new Queue<Func<Task>>();
        private readonly Stack<Func<Task>> _undoActions = new Stack<Func<Task>>();

        private readonly Dictionary<object, EntityBookKeeping> _bookKeeping = new Dictionary<object, EntityBookKeeping>();

        public AzureTableUnitOfWork(TContext context)
        {
            Context = context;
        }

        public TContext Context { get; private set; }

        public void RegisterStoreFor<TEntity>(IStoreWriter storeWriter)
        {
            _storeWriters[typeof(TEntity)] = storeWriter;
        }

        public TEntity RegisterAdd<TEntity>(TEntity entity) where TEntity : class, new()
        {
            var storeWriter = GetStoreWriterFor<TEntity>();
            _addedEntities.Add(typeof(TEntity), entity);
            AddPendingAction(
                () => storeWriter.Add(entity),
                () => storeWriter.Delete(entity)
            );

            var trackedEntity = entity.AsTrackable();
            GetBookKeepingFor(trackedEntity);

            return trackedEntity;
        }

        public IEnumerable<TEntity> RegisterForUpdates<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, new()
        {
            var trackedEntities = new List<TEntity>();
            foreach (var entity in entities)
            {
                var trackedEntity = TrackEntityForUpdates(entity);
                trackedEntities.Add(trackedEntity);
            }
            return trackedEntities;
        }

        public void RegisterDelete<TEntity>(TEntity entity) where TEntity : class, new()
        {
            var storeWriter = GetStoreWriterFor<TEntity>();
            _deletedEntities.Add(typeof(TEntity), entity);
            AddPendingAction(
                () => storeWriter.Delete(entity),
                () => storeWriter.Add(entity)
                );
        }

        public async Task CompleteAsync()
        {
            // Intentionally execute these in sequence. We will assume order matters.
            while (_pendingActions.Any())
            {
                var pending = _pendingActions.Dequeue();
                var actionTask = pending();
                actionTask.Start();
                await actionTask.ConfigureAwait(false);
            }

            // Ensure that we've unregistered property change event handlers
            foreach (var entityBookKeeping in _bookKeeping)
            {
                var notifyPropertyChanged = entityBookKeeping.Key as INotifyPropertyChanged;
                notifyPropertyChanged.PropertyChanged -= entityBookKeeping.Value.ChangeEventHandler;
                entityBookKeeping.Value.ChangeEventHandler = null;
                entityBookKeeping.Value.CopyOfOriginalForUndo = null;
            }

            // TODO: If anything went wrong, then we need to work through the undo actions

            // If all went well, then we can clear the undo actions
            _undoActions.Clear();
        }

        private void AddPendingAction(Action action, Action undoAction = null)
        {
            _pendingActions.Enqueue(() => new Task(action));
            if (null != undoAction)
            {
                AddUndoAction(undoAction);
            }
        }

        private void AddUndoAction(Action undoAction)
        {
            _undoActions.Push(() => new Task(undoAction));
        }

        private EntityBookKeeping GetBookKeepingFor(object entity)
        {
            EntityBookKeeping result;
            if (!_bookKeeping.TryGetValue(entity, out result))
            {
                result = new EntityBookKeeping();
                _bookKeeping[entity] = result;
            }
            return result;
        }

        private static readonly HashSet<string> ChangeTrackingPropertyNames = new HashSet<string>(
                typeof(IChangeTrackable).GetProperties().Select(x => x.Name)
                    .Concat(typeof(IChangeTrackableCollection<>).GetProperties().Select(x => x.Name))
                    .Concat(typeof(IBindingList).GetProperties().Select(x => x.Name))
                    .Distinct()
                );

        private static bool IsChangeTrackingPropertyUpdate(PropertyChangedEventArgs args)
        {
            return ChangeTrackingPropertyNames.Contains(args.PropertyName);
        }

        private IStoreWriter GetStoreWriterFor<TEntity>()
        {
            IStoreWriter storeWriter;
            if(!_storeWriters.TryGetValue(typeof(TEntity), out storeWriter))
            {
                throw new InvalidOperationException($"{nameof(IStoreWriter)} for Entity Type [{typeof(TEntity).Name}] is not registered.");
            }
            return storeWriter;
        }

        private TEntity TrackEntityForUpdates<TEntity>(TEntity entity) where TEntity : class, new()
        {
            Action<TEntity> updateAction = GetStoreWriterFor<TEntity>().Update;
            Action<TEntity> undoAction = GetStoreWriterFor<TEntity>().Update;

            var trackedEntity = entity is IChangeTrackable<TEntity> ? entity : entity.AsTrackable();
            var notifyPropertyChanged = trackedEntity as INotifyPropertyChanged;
            var outerBookKeeping = GetBookKeepingFor(trackedEntity);

            // Don't double-register change event handling
            if (null == outerBookKeeping.ChangeEventHandler)
            {
                outerBookKeeping.ChangeEventHandler = (changedObject, args) =>
                {
                    if (!IsChangeTrackingPropertyUpdate(args))
                    {
                        var changedEntity = changedObject as ITableEntity;
                        GetBookKeepingFor(changedEntity).EditCount++;

                        // Track the update action
                        AddPendingAction(() =>
                        {
                            var bookKeeping = GetBookKeepingFor(changedEntity);
                            if (--bookKeeping.EditCount <= 0)
                            {
                                // Sock away the original so we can use it for undoing later
                                var trackable = trackedEntity.CastToIChangeTrackable();
                                bookKeeping.CopyOfOriginalForUndo = trackable.GetOriginal();

                                // Stop change notification handling
                                notifyPropertyChanged.PropertyChanged -= outerBookKeeping.ChangeEventHandler;
                                outerBookKeeping.ChangeEventHandler = null;

                                // Apply the pending changes to the object
                                trackable.AcceptChanges();
                                var updatedEntity = trackable.GetCurrent();
                                updateAction(updatedEntity);

                                // Only if we're actually executing the update action do we track the undo action
                                AddUndoAction(() =>
                                {
                                    if (null != bookKeeping.CopyOfOriginalForUndo)
                                    {
                                        undoAction(bookKeeping.CopyOfOriginalForUndo as TEntity);
                                    }
                                });
                            }

                            // Else there are future edits, so we'll skip this one and just wait till we've rolled them
                            // all up into a single operation.
                        });
                    }
                };

                // Handle the change count
                notifyPropertyChanged.PropertyChanged += outerBookKeeping.ChangeEventHandler;
            }

            return trackedEntity;
        }

        protected override void DisposeManagedObjects() { }

        protected override void NullifyLargeFields()
        {
            base.NullifyLargeFields();
            _addedEntities.Clear();
            _deletedEntities.Clear();
            _undoActions.Clear();
            _pendingActions.Clear();
            // Don't forget to unwire the change event handlers.
            foreach (var entityBookKeeping in _bookKeeping)
            {
                var notifyPropertyChanged = entityBookKeeping.Key as INotifyPropertyChanged;
                notifyPropertyChanged.PropertyChanged -= entityBookKeeping.Value.ChangeEventHandler;
            }
            _bookKeeping.Clear();
        }
    }
}