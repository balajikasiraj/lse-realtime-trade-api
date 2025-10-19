using System;

namespace Lse.Application.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string? message = null) : base(message ?? "Resource not found") { }
    }
}
