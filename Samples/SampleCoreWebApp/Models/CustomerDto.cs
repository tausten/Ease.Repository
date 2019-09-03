using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;

namespace SampleCoreWebApp.Models
{
    public class CustomerDto
    {
        [HiddenInput]
        public string PartitionKey { get; set; }

        public Guid Id { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }

        [DataType(DataType.Date)]
        public virtual DateTime Birthday { get; set; }
        public virtual Guid? FavoriteProductId { get; set; }
    }
}
