using System;
using System.Data;
using MySql.Data.MySqlClient;
using LMS_Library_Management_System.Helper;

namespace LMS_Library_Management_System.DBContext
{
    public class LibraryDbContext : IDisposable
    {
        private MySqlConnection _connection;
        private bool _disposed = false;

        public LibraryDbContext()
        {
            _connection = MYSqlHelper.CreateConnection();
        }

        public MySqlConnection Connection
        {
            get { return _connection; }
        }

        public void Open()
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }

        public void Close()
        {
            if (_connection.State != ConnectionState.Closed)
            {
                _connection.Close();
            }
        }

        public MySqlCommand CreateCommand()
        {
            return _connection.CreateCommand();
        }

        public MySqlCommand CreateCommand(string query)
        {
            var command = _connection.CreateCommand();
            command.CommandText = query;
            return command;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_connection != null)
                    {
                        if (_connection.State == ConnectionState.Open)
                        {
                            _connection.Close();
                        }
                        _connection.Dispose();
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

