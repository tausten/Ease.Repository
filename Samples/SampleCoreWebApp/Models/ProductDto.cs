using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleCoreWebApp.Models
{
    public class ProductDto
    {
        public string PartitionKey { get; set; }
        public Guid Id { get; set; }
        public virtual string ProductName { get; set; }
        public virtual string ProductDescription { get; set; }
    }
}
