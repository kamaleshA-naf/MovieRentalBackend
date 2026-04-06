using System.Diagnostics;

namespace MovieRentalApp.Exceptions
{
    [DebuggerNonUserCode]
    public class BusinessRuleViolationException : Exception
    {
        public BusinessRuleViolationException(string message)
            : base(message) { }
    }
}
