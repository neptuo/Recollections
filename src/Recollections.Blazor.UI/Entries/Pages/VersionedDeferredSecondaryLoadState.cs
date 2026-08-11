using System.Collections.Generic;
using System.Threading;

namespace Neptuo.Recollections.Entries.Pages
{
    internal sealed class VersionedDeferredSecondaryLoadState<T>
    {
        private long loadVersion;
        private long pendingVersion;
        private bool shouldLoadSecondaryData;
        private T secondaryDataArgument = default!;

        public long BeginLoad()
            => Interlocked.Increment(ref loadVersion);

        public long CurrentVersion
            => Volatile.Read(ref loadVersion);

        public bool IsCurrent(long version)
            => version == CurrentVersion;

        public bool IsCurrent(long version, T secondaryDataArgument)
            => version == CurrentVersion
                && EqualityComparer<T>.Default.Equals(this.secondaryDataArgument, secondaryDataArgument);

        public void ScheduleSecondaryLoad(T secondaryDataArgument)
        {
            this.secondaryDataArgument = secondaryDataArgument;
            pendingVersion = CurrentVersion;
            shouldLoadSecondaryData = true;
        }

        public bool TryConsumeSecondaryLoad(out long version, out T secondaryDataArgument)
        {
            if (!shouldLoadSecondaryData)
            {
                version = default;
                secondaryDataArgument = default!;
                return false;
            }

            shouldLoadSecondaryData = false;
            version = pendingVersion;
            secondaryDataArgument = this.secondaryDataArgument;
            pendingVersion = default;
            return true;
        }
    }
}
