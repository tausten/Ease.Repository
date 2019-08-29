//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using ChangeTracking;

namespace Ease.Repository
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
