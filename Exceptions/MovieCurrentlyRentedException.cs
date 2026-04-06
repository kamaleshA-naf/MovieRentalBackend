using System.Diagnostics;

namespace MovieRentalApp.Exceptions
{
    [DebuggerNonUserCode]
    public class MovieCurrentlyRentedException : Exception
    {
        public int ActiveRentalCount { get; }

        public MovieCurrentlyRentedException(string movieTitle, int activeCount)
            : base($"This movie cannot be deleted because it is currently rented by {activeCount} customer{(activeCount > 1 ? "s" : "")}.")
        {
            ActiveRentalCount = activeCount;
        }
    }
}
