using System.Diagnostics;

namespace MovieRentalApp.Exceptions
{
    [DebuggerNonUserCode]
    public class UnableToCreateEntityException : Exception
    {
        public UnableToCreateEntityException(string entityName)
            : base($"Unable to create {entityName}. Please try again.") { }

        public UnableToCreateEntityException(string entityName, string reason)
            : base($"Unable to create {entityName}: {reason}") { }
    }
}
