using System;

namespace Lse.Application.Exceptions
{
    public class ValidationException : Exception
    {
        public ValidationException(string? message = null) : base(message ?? "Validation failed") { }
    }
}
