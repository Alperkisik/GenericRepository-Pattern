using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GenericRepository_Pattern.Interfaces;
using Dapper;

namespace GenericRepository_Pattern.Repository
{
    public class GenericRepository_Dapper : IGenericRepository_Dapper, IDisposable
    {
        private bool disposedValue;

        readonly string _ConnectionString;

        private string GenericQuery<T>(CommandType commandType = CommandType.Text, QueryType queryType = QueryType.select, Dictionary<string, object> parameters = null)
        {
            var query = "";

            if (commandType == CommandType.Text && queryType == QueryType.select) query = $"SELECT * FROM {GetTableName<T>()}";
            if (commandType == CommandType.Text && queryType == QueryType.count) query = $"SELECT COUNT(*) FROM {GetTableName<T>()}";
            if (commandType == CommandType.Text && queryType == QueryType.any) query = $"SELECT 1 FROM {GetTableName<T>()}";
            if (commandType == CommandType.Text && queryType == QueryType.single) query = $"SELECT * FROM {GetTableName<T>()}";
            if (commandType == CommandType.Text && queryType == QueryType.insert) query = $"INSERT INTO {GetTableName<T>()} ({GetColumns<T>(excludeKey: true)}) VALUES ({GetPropertyNames<T>(excludeKey: true)})";

            if (commandType == CommandType.Text && queryType == QueryType.update)
            {
                query = $"UPDATE {GetTableName<T>()} SET ";

                var id_paramName = "";

                foreach (var parameter in parameters)
                {
                    var columnName = parameter.Key.Replace("@", "");
                    if (columnName.ToLower() == "id")
                    {
                        id_paramName = columnName;
                        continue;
                    }

                    query += $"{columnName}=@{columnName},";
                }

                query = query.Remove(query.Length - 1, 1);

                query += $" WHERE {GetKeyColumnName<T>()}=@{id_paramName}";
            }

            if (commandType == CommandType.Text && (parameters != null && parameters.Count > 0) && (queryType != QueryType.insert && queryType != QueryType.update))
            {
                query += " WHERE";
                int index = 0;
                foreach (var parameter in parameters)
                {
                    string keyName = parameter.Key;
                    if (keyName.Contains("@")) keyName = keyName.Replace("@", "");

                    if (index == 0) query += $" {keyName}=@{keyName}";
                    else query += $" AND {keyName}=@{keyName}";

                    index++;
                }
            }


            if (commandType == CommandType.StoredProcedure && (queryType == QueryType.select || queryType == QueryType.single)) query = $"[dbo].[sel_{GetTableName<T>()}]";
            if (commandType == CommandType.StoredProcedure && queryType == QueryType.count) query = $"[dbo].[sel_{GetTableName<T>()}_Count]";
            if (commandType == CommandType.StoredProcedure && queryType == QueryType.any) query = $"[dbo].[sel_{GetTableName<T>()}_Any]";
            if (commandType == CommandType.StoredProcedure && (queryType == QueryType.insert || queryType == QueryType.update)) query = $"[dbo].[up_{GetTableName<T>()}]";

            return query;
        }

        private string GetTableName<T>()
        {
            string tableName;
            var type = typeof(T);

            tableName = type.Name;

            var tableAttr = type.GetCustomAttribute<TableAttribute>();
            if (tableAttr != null) tableName = tableAttr.Name;

            return tableName;
        }
        private string GetColumns<T>(bool excludeKey = false)
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
        protected string GetPropertyNames<T>(bool excludeKey = false)
        {
            var properties = typeof(T).GetProperties()
                .Where(p => !excludeKey || p.GetCustomAttribute<KeyAttribute>() == null);

            var values = string.Join(", ", properties.Select(p =>
            {
                return $"@{p.Name}";
            }));

            return values;
        }
        protected string GetKeyPropertyName<T>()
        {
            var properties = typeof(T).GetProperties()
                .Where(p => p.GetCustomAttribute<KeyAttribute>() != null);

            if (properties.Any())
            {
                return properties.FirstOrDefault().Name;
            }

            return null;
        }
        public static string GetKeyColumnName<T>()
        {
            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (PropertyInfo property in properties)
            {
                object[] keyAttributes = property.GetCustomAttributes(typeof(KeyAttribute), true);

                if (keyAttributes != null && keyAttributes.Length > 0)
                {
                    object[] columnAttributes = property.GetCustomAttributes(typeof(ColumnAttribute), true);

                    if (columnAttributes != null && columnAttributes.Length > 0)
                    {
                        ColumnAttribute columnAttribute = (ColumnAttribute)columnAttributes[0];
                        return columnAttribute.Name;
                    }
                    else
                    {
                        return property.Name;
                    }
                }
            }

            return null;
        }
        protected IEnumerable<PropertyInfo> GetProperties<T>(bool excludeKey = false)
        {
            var properties = typeof(T).GetProperties()
                .Where(p => !excludeKey || p.GetCustomAttribute<KeyAttribute>() == null);

            return properties;
        }



        public GenericRepository_Dapper(string ConnectionString)
        {
            _ConnectionString = ConnectionString;
        }

        public string BaseConnectionString => _ConnectionString;

        public bool Any<T>(Dictionary<string, object> parameters, string query = null, CommandType commandType = CommandType.Text)
        {
            if (parameters.Count == 0) throw new Exception("Dictionary<string, object> parameters must have some parameters.");

            var dynamicParameters = new DynamicParameters();
            foreach (var parameter in parameters)
            {
                dynamicParameters.Add(parameter.Key, parameter.Value);
            }

            if (string.IsNullOrEmpty(query)) query = GenericQuery<T>(commandType, QueryType.any, parameters);

            bool result;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                result = sqlConnection.ExecuteScalar<bool>(query, dynamicParameters, commandType: commandType);
            }

            return result;
        }
        public async Task<bool> AnyAsync<T>(Dictionary<string, object> parameters, string query = null, CommandType commandType = CommandType.Text)
        {
            if (parameters.Count == 0) throw new Exception("Dictionary<string, object> parameters must have some parameters.");

            var dynamicParameters = new DynamicParameters();
            foreach (var parameter in parameters)
            {
                dynamicParameters.Add(parameter.Key, parameter.Value);
            }

            if (string.IsNullOrEmpty(query)) query = GenericQuery<T>(commandType, QueryType.any, parameters);

            bool result;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                result = await sqlConnection.ExecuteScalarAsync<bool>(query, dynamicParameters, commandType: commandType);
            }

            return result;
        }



        public int Count<T>(Dictionary<string, object> parameters = null, string query = null, CommandType commandType = CommandType.Text)
        {
            var dynamicParameters = new DynamicParameters();
            if (parameters != null && parameters.Count > 0)
            {
                foreach (var parameter in parameters)
                {
                    dynamicParameters.Add(parameter.Key, parameter.Value);
                }
            }
            else dynamicParameters = null;

            if (string.IsNullOrEmpty(query)) query = GenericQuery<T>(commandType, QueryType.count, parameters);

            int data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                data = sqlConnection.ExecuteScalar<int>(query, dynamicParameters, commandType: commandType);
            }

            return data;
        }
        public async Task<int> CountAsync<T>(Dictionary<string, object> parameters = null, string query = null, CommandType commandType = CommandType.Text)
        {
            var dynamicParameters = new DynamicParameters();
            if (parameters != null && parameters.Count > 0)
            {
                foreach (var parameter in parameters)
                {
                    dynamicParameters.Add(parameter.Key, parameter.Value);
                }
            }
            else dynamicParameters = null;

            if (string.IsNullOrEmpty(query)) query = GenericQuery<T>(commandType, QueryType.count, parameters);

            int data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                data = await sqlConnection.ExecuteScalarAsync<int>(query, dynamicParameters, commandType: commandType);
            }

            return data;
        }



        public IEnumerable<T> List<T>(Dictionary<string, object> parameters = null, string query = null, CommandType commandType = CommandType.Text)
        {
            var dynamicParameters = new DynamicParameters();
            if (parameters != null && parameters.Count > 0)
            {
                foreach (var parameter in parameters)
                {
                    dynamicParameters.Add(parameter.Key, parameter.Value);
                }
            }
            else dynamicParameters = null;

            if (string.IsNullOrEmpty(query)) query = GenericQuery<T>(commandType, QueryType.select, parameters);

            IEnumerable<T> data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                data = sqlConnection.Query<T>(query, dynamicParameters, commandType: commandType);
            }

            return data;
        }
        public async Task<IEnumerable<T>> ListAsync<T>(Dictionary<string, object> parameters = null, string query = null, CommandType commandType = CommandType.Text)
        {
            var dynamicParameters = new DynamicParameters();
            if (parameters != null && parameters.Count > 0)
            {
                foreach (var parameter in parameters)
                {
                    dynamicParameters.Add(parameter.Key, parameter.Value);
                }
            }
            else dynamicParameters = null;

            if (string.IsNullOrEmpty(query)) query = GenericQuery<T>(commandType, QueryType.select, parameters);

            IEnumerable<T> data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                data = await sqlConnection.QueryAsync<T>(query, dynamicParameters, commandType: commandType);
            }

            return data;
        }



        public T Single<T>(Dictionary<string, object> parameters, string query = null, CommandType commandType = CommandType.Text)
        {
            if (parameters.Count == 0) throw new Exception("Dictionary<string, object> parameters must have some parameters.");

            var dynamicParameters = new DynamicParameters();
            foreach (var parameter in parameters)
            {
                dynamicParameters.Add(parameter.Key, parameter.Value);
            }

            if (string.IsNullOrEmpty(query)) query = GenericQuery<T>(commandType, QueryType.single, parameters);

            T data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                data = sqlConnection.QuerySingle<T>(query, dynamicParameters, commandType: commandType);
            }

            return data;
        }
        public async Task<T> SingleAsync<T>(Dictionary<string, object> parameters, string query = null, CommandType commandType = CommandType.Text)
        {
            if (parameters.Count == 0) throw new Exception("Dictionary<string, object> parameters must have some parameters.");

            var dynamicParameters = new DynamicParameters();
            foreach (var parameter in parameters)
            {
                dynamicParameters.Add(parameter.Key, parameter.Value);
            }

            if (string.IsNullOrEmpty(query)) query = GenericQuery<T>(commandType, QueryType.single, parameters);

            T data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                data = await sqlConnection.QuerySingleAsync<T>(query, parameters, commandType: commandType);
            }

            return data;
        }



        public T SingleById<T>(int id, string query = null, CommandType commandType = CommandType.Text)
        {
            if (id <= 0) throw new Exception("id must be greater then 0.");

            var id_property = typeof(T).GetProperty("id") ?? typeof(T).GetProperty("Id");

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add(id_property.Name, id);

            if (string.IsNullOrEmpty(query))
            {
                query = GenericQuery<T>(commandType);
                if (commandType == CommandType.Text) query += $" WHERE {id_property.Name} = @{id_property.Name}";
            }

            T data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                data = sqlConnection.QuerySingle<T>(query, parameters, commandType: commandType);
            }

            return data;
        }
        public async Task<T> SingleByIdAsync<T>(int id, string query = null, CommandType commandType = CommandType.Text)
        {
            if (id <= 0) throw new Exception("id must be greater then 0.");

            var id_property = typeof(T).GetProperty("id") ?? typeof(T).GetProperty("Id");

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add(id_property.Name, id);

            if (string.IsNullOrEmpty(query))
            {
                query = GenericQuery<T>(commandType);
                if (commandType == CommandType.Text) query += $" WHERE {id_property.Name} = @{id_property.Name}";
            }

            T data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                data = await sqlConnection.QuerySingleAsync<T>(query, parameters, commandType: commandType);
            }

            return data;
        }



        public T SingleData<T>(string query, Dictionary<string, object> parameters = null, CommandType commandType = CommandType.Text)
        {
            var dynamicParameters = new DynamicParameters();
            if (parameters != null && parameters.Count > 0)
            {
                foreach (var parameter in parameters)
                {
                    dynamicParameters.Add(parameter.Key, parameter.Value);
                }
            }
            else dynamicParameters = null;

            T data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                data = sqlConnection.ExecuteScalar<T>(query, dynamicParameters, commandType: commandType);
            }

            return data;
        }
        public async Task<T> SingleDataAsync<T>(string query, Dictionary<string, object> parameters = null, CommandType commandType = CommandType.Text)
        {
            var dynamicParameters = new DynamicParameters();
            if (parameters != null && parameters.Count > 0)
            {
                foreach (var parameter in parameters)
                {
                    dynamicParameters.Add(parameter.Key, parameter.Value);
                }
            }
            else dynamicParameters = null;

            T data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                data = await sqlConnection.ExecuteScalarAsync<T>(query, dynamicParameters, commandType: commandType);
            }

            return data;
        }



        public bool Insert<T>(T model, CommandType commandType = CommandType.Text)
        {
            int rowsEffected = 0;

            string query = GenericQuery<T>(commandType, QueryType.insert);

            try
            {
                using (var sqlConnection = new SqlConnection(BaseConnectionString))
                {
                    rowsEffected = sqlConnection.Execute(query, model);
                }
            }
            catch { }

            return rowsEffected > 0 ? true : false;
        }
        public async Task<bool> InsertAsync<T>(T model, CommandType commandType = CommandType.Text)
        {
            int rowsEffected = 0;

            string query = GenericQuery<T>(commandType, QueryType.insert);

            try
            {
                using (var sqlConnection = new SqlConnection(BaseConnectionString))
                {
                    rowsEffected = await sqlConnection.ExecuteAsync(query, model);
                }
            }
            catch { }

            return rowsEffected > 0 ? true : false;
        }



        public bool Update<T>(Dictionary<string, object> parameters, CommandType commandType = CommandType.Text)
        {
            if (parameters.Count == 0) throw new Exception("Dictionary<string, object> parameters must have some parameters.");

            var dynamicParameters = new DynamicParameters();
            foreach (var parameter in parameters)
            {
                dynamicParameters.Add(parameter.Key, parameter.Value);
            }

            int rowsEffected = 0;

            string query = GenericQuery<T>(commandType, QueryType.update, parameters);

            try
            {
                using (var sqlConnection = new SqlConnection(BaseConnectionString))
                {
                    rowsEffected = sqlConnection.Execute(query, dynamicParameters);
                }
            }
            catch { }

            return rowsEffected > 0 ? true : false;
        }
        public async Task<bool> UpdateAsync<T>(Dictionary<string, object> parameters, CommandType commandType = CommandType.Text)
        {
            if (parameters.Count == 0) throw new Exception("Dictionary<string, object> parameters must have some parameters.");

            var dynamicParameters = new DynamicParameters();
            foreach (var parameter in parameters)
            {
                dynamicParameters.Add(parameter.Key, parameter.Value);
            }

            int rowsEffected = 0;

            string query = GenericQuery<T>(commandType, QueryType.update, parameters);

            try
            {
                using (var sqlConnection = new SqlConnection(BaseConnectionString))
                {
                    rowsEffected = await sqlConnection.ExecuteAsync(query, dynamicParameters);
                }
            }
            catch { }

            return rowsEffected > 0 ? true : false;
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
        // ~DataAccess_Dapper()
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
