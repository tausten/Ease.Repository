using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ease.Repository;
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

        // GET: Customer
        public IActionResult Index()
        {
            var c = _customerRepository.List();

            var mapped = c.Select(x => new CustomerDto
            {
                Id = Guid.Parse(x.RowKey),
                FirstName = x.FirstName,
                LastName = x.LastName,
                Birthday = x.Birthday,
                FavoriteProductId = string.IsNullOrWhiteSpace(x.FavoriteProduct) ? default(Guid?) : Guid.Parse(x.FavoriteProduct)
            }).ToList();

            return View(mapped);
        }

        // GET: Customer/Details/5
        public IActionResult Details(Guid id /*NOTE: Need the PartitionKey for Azure too...*/)
        {

            return View();
        }

        // GET: Customer/Create
        public IActionResult Create()
        {
            var defaultCustomer = new CustomerDto();
            return View(defaultCustomer);
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerDto customer)
        {
            try
            {
                var c = new CustomerAzureTableEntity
                {
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Birthday = customer.Birthday,
                    FavoriteProduct = customer.FavoriteProductId.ToString()
                };

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

        // GET: Customer/Edit/5
        public IActionResult Edit(Guid id /*NOTE: Need the PartitionKey for Azure too...*/)
        {
            return View();
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Guid id /*NOTE: Need the PartitionKey for Azure too...*/, IFormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Customer/Delete/5
        public IActionResult Delete(Guid id /*NOTE: Need the PartitionKey for Azure too...*/)
        {
            return View();
        }

        // POST: Customer/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id /*NOTE: Need the PartitionKey for Azure too...*/, IFormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}