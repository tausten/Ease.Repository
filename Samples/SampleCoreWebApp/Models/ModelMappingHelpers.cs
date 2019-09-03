using Ease.Repository.AzureTable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleCoreWebApp.Models
{
    public static class ModelMappingHelpers
    {
        public static string ToStringId(this Guid? id)
        {
            string result = null;
            if (id.HasValue && default != id.Value)
            {
                result = id?.ToString().ToUpperInvariant();
            }

            return result;
        }
        public static string ToStringId(this Guid id)
        {
            return ToStringId((Guid?)id);
        }

        public static Guid? ToGuidId(this string id)
        {
            return string.IsNullOrWhiteSpace(id) ? default(Guid?) : Guid.Parse(id);
        }

        public static AzureTableEntityKey ToCompositeKeyFor(this Guid id, string partitionKey)
        {
            return new AzureTableEntityKey { PartitionKey = partitionKey, RowKey = id.ToStringId() };
        }
    }
}
