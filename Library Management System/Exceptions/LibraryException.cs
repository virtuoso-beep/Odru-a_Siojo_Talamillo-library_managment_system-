using System;

namespace Library_Management_System.Exceptions
{
    /// <summary>
    /// Base exception class for all library management system exceptions
    /// </summary>
    public class LibraryException : Exception
    {
        public LibraryException() : base()
        {
        }

        public LibraryException(string message) : base(message)
        {
        }

        public LibraryException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

