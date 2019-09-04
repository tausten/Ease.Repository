//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System;
using System.Threading.Tasks;

namespace Ease.Repository
{
    /// <summary>
    /// Interface for general unit of work for the repository pattern.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Wrap up the unit of work (i.e. success path). If a <see cref="IUnitOfWork"/> is `Dispose`d before it has
        /// been `Complete`d, then the work is considered abandoned and should be undone. After the unit of work has
        /// been `Complete`d, it is invalid to continue with more work prior to `Dispose`, and `Complete` may
        /// perform an implicit `Dispose` to help enforce this.
        /// </summary>
        /// <returns>The task managing the async execution of pending work.</returns>
        Task CompleteAsync();
    }
}