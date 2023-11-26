using GenericRepository_Pattern.Extensions;
using GenericRepository_Pattern.Interfaces;
using GenericRepository_Pattern.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GenericRepository_Pattern.DataAccess_Library
{
    public class GenericDataAccess_ADONET<T> : IDisposable, IGenericDataAccess_ADONET<T> where T : Entity
    {
        readonly string BaseConnectionString;
        private bool disposedValue;

        private List<SqlParameter> DictionaryToSqlParameters(Dictionary<string, object> parameters)
        {
            var sqlParameters = new List<SqlParameter>();

            foreach (var parameter in parameters)
            {
                var key = parameter.Key;
                if (!key[0].Equals("@")) key = $"@{key}";

                sqlParameters.Add(new SqlParameter(key, parameter.Value));
            }

            return sqlParameters;
        }

        private List<SqlParameter> ObjectToSqlParameters(object parameters)
        {
            var sqlParameters = new List<SqlParameter>();

            var properties = parameters.GetType().GetProperties();
            foreach (var property in properties)
            {
                var key = property.Name;
                if (!key[0].Equals("@")) key = $"@{key}";

                sqlParameters.Add(new SqlParameter(key, property.GetValue(parameters)));
            }

            return sqlParameters;
        }

        private string KeyPropertyName()
        {
            var properties = typeof(T).GetProperties().Where(p => p.GetCustomAttribute<KeyAttribute>() != null);

            if (properties.Any()) return properties.FirstOrDefault().Name;

            var entityType = typeof(T);
            var entityName = entityType.Name;

            var property = entityType.GetProperty("Id");
            if (property == null) property = entityType.GetProperty("id");
            if (property == null) property = entityType.GetProperty($"{entityName}Id");
            if (property == null) property = entityType.GetProperty($"{entityName}_Id");
            if (property == null) property = entityType.GetProperty($"{entityName}_id");

            if (property != null) return property.Name;

            return string.Empty;
        }

        private string TableName()
        {
            return typeof(T).GetCustomAttribute<TableAttribute>() == null ? typeof(T).Name : typeof(T).GetCustomAttribute<TableAttribute>().Name;
        }

        private string Columns(bool excludeKey = false)
        {
            var type = typeof(T);
            var columns = string.Join(", ", type.GetProperties()
                .Where(p => !excludeKey || !p.IsDefined(typeof(KeyAttribute)))
                .Select(p =>
                {
                    var columnAttr = p.GetCustomAttribute<ColumnAttribute>();
                    return columnAttr != null ? columnAttr.Name : p.Name;
                }));

            return columns;
        }

        private string PropertyNames(bool excludeKey = false)
        {
            var properties = typeof(T).GetProperties()
                .Where(p => !excludeKey || p.GetCustomAttribute<KeyAttribute>() == null);

            var values = string.Join(", ", properties.Select(p =>
            {
                return $"@{p.Name}";
            }));

            return values;
        }

        private string StoredProcedureFunctionName(QueryType queryType, string schema = "dbo")
        {
            string functionName;

            switch (queryType)
            {
                case QueryType.select:
                    functionName = $"[{schema}].[Select_{TableName()}]";
                    break;
                case QueryType.update:
                    functionName = $"[{schema}].[Update_{TableName()}]";
                    break;
                case QueryType.insert:
                    functionName = $"[{schema}].[Insert_{TableName()}]";
                    break;
                case QueryType.count:
                    functionName = $"[{schema}].[Select_{TableName()}_Count]";
                    break;
                case QueryType.any:
                    functionName = $"[{schema}].[Select_{TableName()}_Any]";
                    break;
                case QueryType.single:
                    functionName = $"[{schema}].[Select_{TableName()}_Single]";
                    break;
                default:
                    functionName = $"[{schema}].[Select_{TableName()}]";
                    break;
            }

            return functionName;
        }

        private string GenericQuery(CommandType commandType = CommandType.Text, QueryType queryType = QueryType.select, List<SqlParameter> parameters = null, string schema = "dbo")
        {
            var query = "";
            if (commandType == CommandType.StoredProcedure) query = StoredProcedureFunctionName(queryType, schema);

            if (queryType == QueryType.select) query = $"SELECT * FROM {schema}.[{TableName()}]";
            if (queryType == QueryType.count) query = $"SELECT COUNT(*) FROM {schema}.[{TableName()}]";
            if (queryType == QueryType.any) query = $"SELECT 1 FROM {schema}.[{TableName()}]";
            if (queryType == QueryType.single) query = $"SELECT * FROM {schema}.[{TableName()}]";
            if (queryType == QueryType.insert) query = $"INSERT INTO {schema}.[{TableName()}] ({Columns(excludeKey: true)}) VALUES ({PropertyNames(excludeKey: true)})";
            if (queryType == QueryType.update)
            {
                query = $"UPDATE {schema}.[{TableName()}] SET ";

                var keyProperty = KeyPropertyName();

                foreach (var parameter in parameters)
                {
                    var columnName = parameter.ParameterName.Replace("@", "");

                    query += $"{columnName}=@{columnName},";
                }

                query = query.Remove(query.Length - 1, 1);

                query += $" WHERE {keyProperty}=@{keyProperty}";
            }

            if (parameters != null && parameters.Count > 0 && (queryType != QueryType.insert && queryType != QueryType.update))
            {
                query += " WHERE ";
                int index = 0;
                foreach (var parameter in parameters)
                {
                    string Name = parameter.ParameterName;
                    if (Name.Contains("@")) Name = Name.Replace("@", "");

                    if (index == 0) query += $"{Name}=@{Name}";
                    else query += $" AND {Name}=@{Name}";

                    index++;
                }
            }

            return query;
        }

        public GenericDataAccess_ADONET(string ConnectionString)
        {
            BaseConnectionString = ConnectionString;
        }

        public virtual T GetByParameters(List<SqlParameter> parameters, string query = null, CommandType commandType = CommandType.Text)
        {
            if (parameters == null || parameters.Count == 0) throw new Exception("List<SqlParameter> sqlParameters cannot be null or empty.");

            T dataObject;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                if (string.IsNullOrEmpty(query)) query = GenericQuery(commandType, QueryType.single, parameters);
                using (var sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;
                    foreach (var parameter in parameters)
                    {
                        sqlCommand.Parameters.Add(parameter);
                    }

                    dataObject = sqlCommand.ExecuteReader(CommandBehavior.SingleRow).ReadSingle<T>();
                }

                sqlConnection.Close();
            }

            return dataObject;
        }

        public virtual T GetByParameters(object parameters, string query = null, CommandType commandType = CommandType.Text)
        {
            if (parameters == null || parameters.GetType().GetProperties().Length == 0) throw new Exception("parameters cannot be null or empty.");

            var sqlParameters = ObjectToSqlParameters(parameters);
            T dataObject;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                if (string.IsNullOrEmpty(query)) query = GenericQuery(commandType, QueryType.single, sqlParameters);
                using (var sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;
                    foreach (var parameter in sqlParameters)
                    {
                        sqlCommand.Parameters.Add(parameter);
                    }

                    dataObject = sqlCommand.ExecuteReader(CommandBehavior.SingleRow).ReadSingle<T>();
                }

                sqlConnection.Close();
            }

            return dataObject;
        }

        public virtual T GetByParameters(Dictionary<string, object> parameters, string query = null, CommandType commandType = CommandType.Text)
        {
            if (parameters == null || parameters.Count == 0) throw new Exception("Dictionary<string, object> parameters cannot be null or empty.");

            var sqlParameters = DictionaryToSqlParameters(parameters);
            T dataObject;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                if (string.IsNullOrEmpty(query)) query = GenericQuery(commandType, QueryType.single, sqlParameters);
                using (var sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;
                    foreach (var parameter in sqlParameters)
                    {
                        sqlCommand.Parameters.Add(parameter);
                    }

                    dataObject = sqlCommand.ExecuteReader(CommandBehavior.SingleRow).ReadSingle<T>();
                }

                sqlConnection.Close();
            }

            return dataObject;
        }

        public virtual T GetById(int id, string query = null, CommandType commandType = CommandType.Text)
        {
            if (id <= 0) throw new Exception("id cannot be <= 0.");

            T dataObject;

            var sqlParameters = new List<SqlParameter>
            {
                new SqlParameter($"@{KeyPropertyName()}", id)
            };

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                if (string.IsNullOrEmpty(query)) query = GenericQuery(commandType, QueryType.single, sqlParameters);
                using (var sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;

                    foreach (var parameter in sqlParameters)
                    {
                        sqlCommand.Parameters.Add(parameter);
                    }

                    dataObject = sqlCommand.ExecuteReader(CommandBehavior.SingleRow).ReadSingle<T>();
                }

                sqlConnection.Close();
            }

            return dataObject;
        }

        public virtual IEnumerable<T> GetAll(string query = null, CommandType commandType = CommandType.Text)
        {
            IEnumerable<T> data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                if (string.IsNullOrEmpty(query)) query = GenericQuery(commandType, QueryType.select);
                using (var sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;

                    data = sqlCommand.ExecuteReader().ReadList<T>();
                }

                sqlConnection.Close();
            }

            return data;
        }

        public virtual IEnumerable<T> GetAllByParameters(List<SqlParameter> parameters, string query = null, CommandType commandType = CommandType.Text)
        {
            if (parameters == null || parameters.Count == 0) throw new Exception("parameters cannot be null or empty.");
            if (commandType == CommandType.Text && query.Contains("WHERE") && !query.Contains("@")) throw new Exception("string query value cannot be allowed when there is 'where' keyword with no parameters. Vulneruble to Sql Injection Attacks.");

            IEnumerable<T> dataObject;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                if (string.IsNullOrEmpty(query)) query = GenericQuery(commandType, QueryType.select, parameters);
                using (var sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;
                    foreach (var parameter in parameters)
                    {
                        sqlCommand.Parameters.Add(parameter);
                    }

                    dataObject = sqlCommand.ExecuteReader().ReadList<T>();
                }

                sqlConnection.Close();
            }

            return dataObject;
        }

        public virtual IEnumerable<T> GetAllByParameters(object parameters, string query = null, CommandType commandType = CommandType.Text)
        {
            if (parameters == null || parameters.GetType().GetProperties().Length == 0) throw new Exception("parameters cannot be null or empty.");
            if (commandType == CommandType.Text && query.Contains("WHERE") && !query.Contains("@")) throw new Exception("string query value cannot be allowed when there is 'where' keyword with no parameters. Vulneruble to Sql Injection Attacks.");

            IEnumerable<T> dataObject;

            var sqlParameters = ObjectToSqlParameters(parameters);

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                if (string.IsNullOrEmpty(query)) query = GenericQuery(commandType, QueryType.select, sqlParameters);
                using (var sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;

                    foreach (var parameter in sqlParameters)
                    {
                        sqlCommand.Parameters.Add(parameter);
                    }

                    dataObject = sqlCommand.ExecuteReader().ReadList<T>();
                }

                sqlConnection.Close();
            }

            return dataObject;
        }

        public virtual IEnumerable<T> GetAllByParameters(Dictionary<string, object> parameters, string query = null, CommandType commandType = CommandType.Text)
        {
            if (parameters == null || parameters.Count == 0) throw new Exception("parameters cannot be null or empty.");
            if (commandType == CommandType.Text && query.Contains("WHERE") && !query.Contains("@")) throw new Exception("string query value cannot be allowed when there is 'where' keyword with no parameters. Vulneruble to Sql Injection Attacks.");

            IEnumerable<T> dataObject;

            var sqlParameters = DictionaryToSqlParameters(parameters);

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                if (string.IsNullOrEmpty(query)) query = GenericQuery(commandType, QueryType.select, sqlParameters);
                using (var sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;

                    foreach (var parameter in sqlParameters)
                    {
                        sqlCommand.Parameters.Add(parameter);
                    }

                    dataObject = sqlCommand.ExecuteReader().ReadList<T>();
                }

                sqlConnection.Close();
            }

            return dataObject;
        }

        public virtual bool InsertOrUpdate(int id, List<SqlParameter> parameters, string query = null, CommandType commandType = CommandType.Text)
        {
            if (id < 0) throw new Exception("id cannot be lower than 0.");
            if (parameters == null || parameters.Count == 0) throw new Exception("parameters cannot be null or empty.");
            if (commandType == CommandType.Text && query.Contains("WHERE") && !query.Contains("@")) throw new Exception("string query value cannot be allowed when there is 'where' keyword with no parameters. Vulneruble to Sql Injection Attacks.");

            bool result = false;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                if (string.IsNullOrEmpty(query) && id == 0) query = GenericQuery(commandType, QueryType.insert, parameters);
                else if (string.IsNullOrEmpty(query) && id > 0)
                {
                    parameters.Add(new SqlParameter($"@{KeyPropertyName()}", id));
                    query = GenericQuery(commandType, QueryType.update, parameters);
                }

                using (var sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;

                    foreach (var parameter in parameters)
                    {
                        sqlCommand.Parameters.Add(parameter);
                    }

                    result = sqlCommand.ExecuteNonQuery() > 0 ? true : false;
                }

                sqlConnection.Close();
            }

            return result;
        }

        public virtual bool InsertOrUpdate(int id, object parameters, string query = null, CommandType commandType = CommandType.Text)
        {
            if (id < 0) throw new Exception("id cannot be lower than 0.");
            if (parameters == null || parameters.GetType().GetProperties().Length == 0) throw new Exception("parameters cannot be null or empty.");
            if (commandType == CommandType.Text && query.Contains("WHERE") && !query.Contains("@")) throw new Exception("string query value cannot be allowed when there is 'where' keyword with no parameters. Vulneruble to Sql Injection Attacks.");

            var sqlParameters = ObjectToSqlParameters(parameters);

            bool result = false;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                if (string.IsNullOrEmpty(query) && id == 0) query = GenericQuery(commandType, QueryType.insert, sqlParameters);
                else if (string.IsNullOrEmpty(query) && id > 0)
                {
                    sqlParameters.Add(new SqlParameter($"@{KeyPropertyName()}", id));
                    query = GenericQuery(commandType, QueryType.update, sqlParameters);
                }

                using (var sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;

                    foreach (var parameter in sqlParameters)
                    {
                        sqlCommand.Parameters.Add(parameter);
                    }

                    result = sqlCommand.ExecuteNonQuery() > 0 ? true : false;
                }

                sqlConnection.Close();
            }

            return result;
        }

        public virtual bool InsertOrUpdate(int id, Dictionary<string, object> parameters, string query = null, CommandType commandType = CommandType.Text)
        {
            if (id < 0) throw new Exception("id cannot be lower than 0.");
            if (parameters == null || parameters.Count == 0) throw new Exception("parameters cannot be null or empty.");
            if (commandType == CommandType.Text && query.Contains("WHERE") && !query.Contains("@")) throw new Exception("string query value cannot be allowed when there is 'where' keyword with no parameters. Vulneruble to Sql Injection Attacks.");

            var sqlParameters = ObjectToSqlParameters(parameters);

            bool result = false;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                if (string.IsNullOrEmpty(query) && id == 0) query = GenericQuery(commandType, QueryType.insert, sqlParameters);
                else if (string.IsNullOrEmpty(query) && id > 0)
                {
                    sqlParameters.Add(new SqlParameter($"@{KeyPropertyName()}", id));
                    query = GenericQuery(commandType, QueryType.update, sqlParameters);
                }

                using (var sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;

                    foreach (var parameter in sqlParameters)
                    {
                        sqlCommand.Parameters.Add(parameter);
                    }

                    result = sqlCommand.ExecuteNonQuery() > 0 ? true : false;
                }

                sqlConnection.Close();
            }

            return result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~GenericDataAccess_ADONET()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
