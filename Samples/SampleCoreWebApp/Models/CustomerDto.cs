using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleCoreWebApp.Models
{
    public class CustomerDto
    {
        public Guid Id { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual DateTime Birthday { get; set; }
        public virtual Guid? FavoriteProductId { get; set; }
    }
}
