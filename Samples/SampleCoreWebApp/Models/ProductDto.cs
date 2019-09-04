//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

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
