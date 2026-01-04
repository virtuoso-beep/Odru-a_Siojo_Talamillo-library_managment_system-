using System;
using System.Data;
using MySql.Data.MySqlClient;
namespace Library_Management_System.Helper
{
    public static class StoredProcedureHelper
    {
        public static MySqlCommand CreateCommand(string procedureName, MySqlConnection connection)
        {
            if (string.IsNullOrWhiteSpace(procedureName))
                throw new ArgumentException("Procedure name cannot be null or empty", nameof(procedureName));
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            var command = new MySqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };
            return command;
        }
        public static void AddParameter(MySqlCommand command, string parameterName, object value)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(parameterName))
                throw new ArgumentException("Parameter name cannot be null or empty", nameof(parameterName));
            if (!parameterName.StartsWith("@"))
                parameterName = "@" + parameterName;
            command.Parameters.AddWithValue(parameterName, value ?? DBNull.Value);
        }
        public static void AddOutputParameter(MySqlCommand command, string parameterName, MySqlDbType dbType, int size = 0)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(parameterName))
                throw new ArgumentException("Parameter name cannot be null or empty", nameof(parameterName));
            if (!parameterName.StartsWith("@"))
                parameterName = "@" + parameterName;
            var parameter = command.Parameters.Add(parameterName, dbType);
            parameter.Direction = ParameterDirection.Output;
            if (size > 0)
                parameter.Size = size;
        }
        public static T GetOutputParameter<T>(MySqlCommand command, string parameterName)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(parameterName))
                throw new ArgumentException("Parameter name cannot be null or empty", nameof(parameterName));
            if (!parameterName.StartsWith("@"))
                parameterName = "@" + parameterName;
            var value = command.Parameters[parameterName].Value;
            if (value == null || value == DBNull.Value)
                return default(T);
            return (T)Convert.ChangeType(value, typeof(T));
        }
        public static T ExecuteScalar<T>(string procedureName, params (string name, object value)[] parameters)
        {
            using (var connection = MYSqlHelper.CreateConnection())
            {
                using (var command = CreateCommand(procedureName, connection))
                {
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            AddParameter(command, param.name, param.value);
                        }
                    }
                    var result = command.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                        return default(T);
                    return (T)Convert.ChangeType(result, typeof(T));
                }
            }
        }
        public static int ExecuteNonQuery(string procedureName, params (string name, object value)[] parameters)
        {
            using (var connection = MYSqlHelper.CreateConnection())
            {
                using (var command = CreateCommand(procedureName, connection))
                {
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            AddParameter(command, param.name, param.value);
                        }
                    }
                    return command.ExecuteNonQuery();
                }
            }
        }
    }
}
