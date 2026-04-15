using System;

namespace Neptuo.Recollections.Entries
{
    public class TrackImportValidationException : Exception
    {
        public TrackImportValidationException()
            : base("The uploaded GPX file doesn't contain a valid track.")
        { }
    }
}
