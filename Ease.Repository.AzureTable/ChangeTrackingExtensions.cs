using ChangeTracking;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ease.Repository.AzureTable
{
    public static class ChangeTrackingExtensions
    {
        public static T CurrentState<T>(this T mayBeTracked) where T : class
        {
            var tracked = mayBeTracked as IChangeTrackable<T>;
            return tracked?.GetCurrent() ?? mayBeTracked;
        }
    }
}
