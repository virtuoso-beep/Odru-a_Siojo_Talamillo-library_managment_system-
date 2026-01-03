using System;
using System.Collections.Generic;

namespace Library_Management_System.Exceptions
{
    /// <summary>
    /// Exception thrown when validation fails
    /// </summary>
    public class ValidationException : LibraryException
    {
        public List<string> ValidationErrors { get; }

        public ValidationException(string message) : base(message)
        {
            ValidationErrors = new List<string> { message };
        }

        public ValidationException(List<string> errors) : base(string.Join("; ", errors))
        {
            ValidationErrors = errors ?? new List<string>();
        }

        public ValidationException(string message, List<string> errors) : base(message)
        {
            ValidationErrors = errors ?? new List<string>();
        }
    }
}

