using System;
namespace Library_Management_System.Helper
{
    public class LibraryException : Exception
    {
        public LibraryException() : base() { }
        public LibraryException(string message) : base(message) { }
        public LibraryException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}
