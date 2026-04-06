using System.Diagnostics;

namespace MovieRentalApp.Exceptions
{
    [DebuggerNonUserCode]
    public class DuplicateEntityException : Exception
    {
        public DuplicateEntityException(string message)
            : base(message) { }
    }
}
