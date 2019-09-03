using Microsoft.AspNetCore.Mvc;
using System;

namespace SampleCoreWebApp.Models
{
    public class ProductDto
    {
        [HiddenInput]
        public string PartitionKey { get; set; }
        public Guid Id { get; set; }
        public virtual string ProductName { get; set; }
        public virtual string ProductDescription { get; set; }
    }
}
