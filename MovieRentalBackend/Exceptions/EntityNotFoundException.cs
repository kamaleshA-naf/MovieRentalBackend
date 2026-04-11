using System.Diagnostics;

namespace MovieRentalApp.Exceptions
{
    [DebuggerNonUserCode]
    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(string entityName, object key)
            : base($"{entityName} with ID '{key}' was not found.") { }

        public EntityNotFoundException(string message)
            : base(message) { }
    }
}
