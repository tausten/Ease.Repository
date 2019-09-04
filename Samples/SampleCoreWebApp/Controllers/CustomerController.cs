//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Ease.Repository;
using Microsoft.AspNetCore.Mvc;
using SampleCoreWebApp.Models;
using SampleDataLayer;

namespace SampleCoreWebApp.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICustomerAzureTableRepository _customerRepository;

        public CustomerController(IUnitOfWork unitOfWork, ICustomerAzureTableRepository customerRepository)
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
        private static CustomerDto MapEntityToDto(CustomerAzureTableEntity entity, CustomerDto dto = null)
        {
            dto = dto ?? new CustomerDto();

            dto.PartitionKey = entity.PartitionKey;
            dto.Id = entity.RowKey.ToGuidId().Value;
            dto.FirstName = entity.FirstName;
            dto.LastName = entity.LastName;
            dto.Birthday = entity.Birthday;
            dto.FavoriteProductId = entity.FavoriteProduct.ToGuidId();

            return dto;
        }

        private static CustomerAzureTableEntity MapDtoToEntity(CustomerDto dto, CustomerAzureTableEntity entity = null)
        {
            entity = entity ?? new CustomerAzureTableEntity();

            entity.PartitionKey = dto.PartitionKey;
            entity.RowKey = dto.Id.ToStringId();
            entity.FirstName = dto.FirstName;
            entity.LastName = dto.LastName;
            entity.Birthday = dto.Birthday;
            entity.FavoriteProduct = dto.FavoriteProductId.ToStringId();

            return entity;
        }
        #endregion Model mapping

        public IActionResult Details(string partitionKey, Guid id)
        {
            var mapped = GetCustomerDtoByIds(partitionKey, id);
            return View(mapped);
        }

        private CustomerDto GetCustomerDtoByIds(string partitionKey, Guid id)
        {
            var c = _customerRepository.Get(id.ToCompositeKeyFor(partitionKey));
            var mapped = MapEntityToDto(c);
            return mapped;
        }
        private CustomerAzureTableEntity GetCustomerEntityByIds(string partitionKey, Guid id)
        {
            return _customerRepository.Get(id.ToCompositeKeyFor(partitionKey));
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
                _customerRepository.Delete(customerIds.Id.ToCompositeKeyFor(customerIds.PartitionKey));
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