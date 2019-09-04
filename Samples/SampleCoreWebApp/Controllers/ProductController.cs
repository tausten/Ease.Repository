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
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductAzureTableRepository _productRepository;

        public ProductController(IUnitOfWork unitOfWork, IProductAzureTableRepository productRepository)
        {
            _unitOfWork = unitOfWork;
            _productRepository = productRepository;
        }

        public IActionResult Index()
        {
            var c = _productRepository.List();
            var mapped = c.Select(x => MapEntityToDto(x)).ToList();
            return View(mapped);
        }

        #region Model mapping - put this wherever you'd normally gather your mapping code
        private static ProductDto MapEntityToDto(ProductAzureTableEntity entity, ProductDto dto = null)
        {
            dto = dto ?? new ProductDto();

            dto.PartitionKey = entity.PartitionKey;
            dto.Id = entity.RowKey.ToGuidId().Value;
            dto.ProductName = entity.ProductName;
            dto.ProductDescription = entity.ProductDescription;

            return dto;
        }

        private static ProductAzureTableEntity MapDtoToEntity(ProductDto dto, ProductAzureTableEntity entity = null)
        {
            entity = entity ?? new ProductAzureTableEntity();

            entity.PartitionKey = dto.PartitionKey;
            entity.RowKey = dto.Id.ToStringId();
            entity.ProductName = dto.ProductName;
            entity.ProductDescription = dto.ProductDescription;

            return entity;
        }
        #endregion Model mapping

        public IActionResult Details(string partitionKey, Guid id)
        {
            var mapped = GetProductDtoByIds(partitionKey, id);
            return View(mapped);
        }

        private ProductDto GetProductDtoByIds(string partitionKey, Guid id)
        {
            var c = _productRepository.Get(id.ToCompositeKeyFor(partitionKey));
            var mapped = MapEntityToDto(c);
            return mapped;
        }
        private ProductAzureTableEntity GetproductEntityByIds(string partitionKey, Guid id)
        {
            return _productRepository.Get(id.ToCompositeKeyFor(partitionKey));
        }

        public IActionResult Create()
        {
            var defaultproduct = new ProductDto();
            return View(defaultproduct);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductDto product)
        {
            try
            {
                var c = MapDtoToEntity(product);

                _productRepository.Add(c);

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
            var mapped = GetProductDtoByIds(partitionKey, id);
            return View(mapped);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductDto product)
        {
            try
            {
                var entity = GetproductEntityByIds(product.PartitionKey, product.Id);
                if (null != entity)
                {
                    MapDtoToEntity(product, entity);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "product not found.");
                    throw new InvalidOperationException();
                }

                await _unitOfWork.CompleteAsync();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(product);
            }
        }

        public IActionResult Delete(string partitionKey, Guid id)
        {
            var dto = GetProductDtoByIds(partitionKey, id);
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(ProductDto productIds)
        {
            try
            {
                _productRepository.Delete(productIds.Id.ToCompositeKeyFor(productIds.PartitionKey));
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