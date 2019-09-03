using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ease.Repository;
using Ease.Repository.AzureTable;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SampleCoreWebApp.Models;
using SampleDataLayer;

namespace SampleCoreWebApp.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CustomerAzureTableRepository _customerRepository;

        public CustomerController(IUnitOfWork unitOfWork, CustomerAzureTableRepository customerRepository)
        {
            _unitOfWork = unitOfWork;
            _customerRepository = customerRepository;
        }

        public IActionResult Index()
        {
            var c = _customerRepository.List();
            var mapped = c.Select(x => MapEntityToDto(x)).ToList();
            return View(mapped);
        }

        #region Model mapping - put this wherever you'd normally gather your mapping code
        private static string GuidToStringId(Guid? id)
        {
            string result = null;
            if (id.HasValue && default != id.Value)
            {
                result = id?.ToString().ToUpperInvariant();
            }

            return result;
        }

        private static Guid? StringToGuidId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? default(Guid?) : Guid.Parse(id);
        }

        private static CustomerDto MapEntityToDto(CustomerAzureTableEntity entity, CustomerDto dto = null)
        {
            dto = dto ?? new CustomerDto();

            dto.PartitionKey = entity.PartitionKey;
            dto.Id = StringToGuidId(entity.RowKey).Value;
            dto.FirstName = entity.FirstName;
            dto.LastName = entity.LastName;
            dto.Birthday = entity.Birthday;
            dto.FavoriteProductId = StringToGuidId(entity.FavoriteProduct);

            return dto;
        }

        private static CustomerAzureTableEntity MapDtoToEntity(CustomerDto dto, CustomerAzureTableEntity entity = null)
        {
            entity = entity ?? new CustomerAzureTableEntity();

            entity.PartitionKey = dto.PartitionKey;
            entity.RowKey = GuidToStringId(dto.Id);
            entity.FirstName = dto.FirstName;
            entity.LastName = dto.LastName;
            entity.Birthday = dto.Birthday;
            entity.FavoriteProduct = GuidToStringId(dto.FavoriteProductId);

            return entity;
        }
        #endregion Model mapping

        public IActionResult Details(string partitionKey, Guid id)
        {
            var mapped = GetCustomerDtoByIds(partitionKey, id);
            return View(mapped);
        }

        private static AzureTableEntityKey KeyFromIds(string partitionKey, Guid id)
        {
            return new AzureTableEntityKey { PartitionKey = partitionKey, RowKey = GuidToStringId(id) };
        }

        private CustomerDto GetCustomerDtoByIds(string partitionKey, Guid id)
        {
            var c = _customerRepository.Get(KeyFromIds(partitionKey, id));
            var mapped = MapEntityToDto(c);
            return mapped;
        }
        private CustomerAzureTableEntity GetCustomerEntityByIds(string partitionKey, Guid id)
        {
            return _customerRepository.Get(KeyFromIds(partitionKey, id));
        }

        public IActionResult Create()
        {
            var defaultCustomer = new CustomerDto();
            return View(defaultCustomer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerDto customer)
        {
            try
            {
                var c = MapDtoToEntity(customer);

                _customerRepository.Add(c);

                // NOTE: There're many other ways of managing the completion / rollback of the unit of work..  
                // Use your favorite method here (whether this is some hook into the request pipeline, or wrapping the body of the action 
                // in a handler, etc...  To keep the sample simple, I'm directly completing the unit here.
                await _unitOfWork.CompleteAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        public IActionResult Edit(string partitionKey, Guid id)
        {
            var mapped = GetCustomerDtoByIds(partitionKey, id);
            return View(mapped);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerDto customer)
        {
            try
            {
                var entity = GetCustomerEntityByIds(customer.PartitionKey, customer.Id);
                if (null != entity)
                {
                    MapDtoToEntity(customer, entity);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Customer not found.");
                    throw new InvalidOperationException();
                }

                await _unitOfWork.CompleteAsync();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(customer);
            }
        }

        public IActionResult Delete(string partitionKey, Guid id)
        {
            var dto = GetCustomerDtoByIds(partitionKey, id);
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(CustomerDto customerIds)
        {
            try
            {
                _customerRepository.Delete(KeyFromIds(customerIds.PartitionKey, customerIds.Id));
                await _unitOfWork.CompleteAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return RedirectToAction(nameof(Index));
            }
        }
    }
}