using System;

namespace Library_Management_System.Exceptions
{
    /// <summary>
    /// Exception thrown when database operations fail
    /// </summary>
    public class DatabaseException : LibraryException
    {
        public string SqlQuery { get; }

        public DatabaseException(string message) : base(message)
        {
        }

        public DatabaseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public DatabaseException(string message, string sqlQuery, Exception innerException) 
            : base($"{message}. SQL: {sqlQuery}", innerException)
        {
            SqlQuery = sqlQuery;
        }
    }
}

