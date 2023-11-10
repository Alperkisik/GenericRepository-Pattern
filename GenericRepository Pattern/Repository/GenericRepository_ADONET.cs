using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GenericRepository_Pattern.Extensions;
using GenericRepository_Pattern.Interfaces;

namespace GenericRepository_Pattern.Repository
{
    public class GenericRepository_ADONET : IGenericRepository_ADONET, IDisposable
    {
        public GenericRepository_ADONET(string connectionString)
        {
            _connectionString = connectionString;
        }

        bool disposedValue = false;
        readonly string _connectionString;

        private string GenericQuery<T>(CommandType commandType = CommandType.StoredProcedure, QueryType queryType = QueryType.select, List<SqlParameter> parameters = null)
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
                    var columnName = parameter.ParameterName.Replace("@", "");
                    if (columnName.ToLower() == "id")
                    {
                        id_paramName = columnName;
                        continue;
                    }

                    query += $"{columnName}=@{columnName},";
                }

                query = query.Remove(query.Length - 1, 1);

                query += $" WHERE {GetKeyColumnName<T>()}=@{id_paramName.Replace("@", "")}";
            }

            if (commandType == CommandType.Text && (parameters != null && parameters.Count > 0) && (queryType != QueryType.insert && queryType != QueryType.update))
            {
                query += " WHERE";
                int index = 0;
                foreach (var parameter in parameters)
                {
                    string Name = parameter.ParameterName;
                    if (Name.Contains("@")) Name = Name.Replace("@", "");

                    if (index == 0) query += $" {Name}=@{Name}";
                    else query += $" AND {Name}=@{Name}";

                    index++;
                }
            }


            if (commandType == CommandType.StoredProcedure && (queryType == QueryType.select || queryType == QueryType.single)) query = $"[dbo].[Sel_{GetTableName<T>()}]";
            if (commandType == CommandType.StoredProcedure && queryType == QueryType.count) query = $"[dbo].[Sel_{GetTableName<T>()}_Count]";
            if (commandType == CommandType.StoredProcedure && queryType == QueryType.any) query = $"[dbo].[Sel_{GetTableName<T>()}_Any]";
            if (commandType == CommandType.StoredProcedure && (queryType == QueryType.insert || queryType == QueryType.update)) query = $"[dbo].[Up_{GetTableName<T>()}]";

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
        private string GetPropertyNames<T>(bool excludeKey = false)
        {
            var properties = typeof(T).GetProperties()
                .Where(p => !excludeKey || p.GetCustomAttribute<KeyAttribute>() == null);

            var values = string.Join(", ", properties.Select(p =>
            {
                return $"@{p.Name}";
            }));

            return values;
        }
        private string GetKeyPropertyName<T>()
        {
            var properties = typeof(T).GetProperties()
                .Where(p => p.GetCustomAttribute<KeyAttribute>() != null);

            if (properties.Any())
            {
                return properties.FirstOrDefault().Name;
            }

            return null;
        }
        private string GetKeyColumnName<T>()
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
        private int GetIdValue<T>(List<SqlParameter> sqlParameters)
        {
            int idValue = -1;

            var keyName = GetKeyPropertyName<T>();
            foreach (var sqlParameter in sqlParameters)
            {
                var paramName = sqlParameter.ParameterName.Replace("@", "");
                if (paramName == keyName || paramName.ToLower() == "id" || paramName.ToLower() == "ıd")
                {
                    idValue = Convert.ToInt32(sqlParameter.Value);
                    break;
                }
            }

            return idValue;
        }



        public string BaseConnectionString => _connectionString;

        public T SingleData<T>(string sql, List<SqlParameter> sqlParameters = null, CommandType commandType = CommandType.StoredProcedure)
        {
            if ((sqlParameters == null || sqlParameters.Count == 0) && commandType == CommandType.Text) throw new Exception("List<SqlParameter> sqlParameters cannot be null or empty while commandType == CommandType.Text.");

            if (string.IsNullOrEmpty(sql)) throw new Exception("string sql cannot be equals to null or string.empty");

            object data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                using (var sqlCommand = new SqlCommand(sql, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;

                    if (sqlParameters != null)
                    {
                        foreach (var parameter in sqlParameters)
                        {
                            sqlCommand.Parameters.Add(parameter);
                        }
                    }

                    data = sqlCommand.ExecuteScalar();
                }

                sqlConnection.Close();
            }

            return (T)Convert.ChangeType(data, typeof(T));
        }
        public async Task<T> SingleDataAsync<T>(string sql, List<SqlParameter> sqlParameters = null, CommandType commandType = CommandType.StoredProcedure)
        {
            if ((sqlParameters == null || sqlParameters.Count == 0) && commandType == CommandType.Text) throw new Exception("List<SqlParameter> sqlParameters cannot be null or empty while commandType == CommandType.Text.");

            if (string.IsNullOrEmpty(sql)) throw new Exception("string sql cannot be equals to null or string.empty");

            object data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                using (var sqlCommand = new SqlCommand(sql, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;

                    if (sqlParameters != null)
                    {
                        foreach (var parameter in sqlParameters)
                        {
                            sqlCommand.Parameters.Add(parameter);
                        }
                    }

                    data = await sqlCommand.ExecuteScalarAsync();
                }

                sqlConnection.Close();
            }

            return (T)Convert.ChangeType(data, typeof(T));
        }



        public T Single<T>(List<SqlParameter> sqlParameters = null, string sql = null, CommandType commandType = CommandType.StoredProcedure)
        {
            if ((sqlParameters == null || sqlParameters.Count == 0) && commandType == CommandType.Text) throw new Exception("List<SqlParameter> sqlParameters cannot be null or empty while commandType == CommandType.Text.");

            if (string.IsNullOrEmpty(sql)) sql = GenericQuery<T>(commandType, QueryType.single, sqlParameters);

            T dataObject;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                using (var sqlCommand = new SqlCommand(sql, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;

                    if (sqlParameters != null)
                    {
                        foreach (var parameter in sqlParameters)
                        {
                            sqlCommand.Parameters.Add(parameter);
                        }
                    }

                    dataObject = sqlCommand.ExecuteReader(CommandBehavior.SingleRow).ReadSingle<T>();
                }

                sqlConnection.Close();
            }

            return dataObject;
        }
        public async Task<T> SingleAsync<T>(List<SqlParameter> sqlParameters = null, string sql = null, CommandType commandType = CommandType.StoredProcedure)
        {
            if ((sqlParameters == null || sqlParameters.Count == 0) && commandType == CommandType.Text) throw new Exception("List<SqlParameter> sqlParameters cannot be null or empty while commandType == CommandType.Text.");

            if (string.IsNullOrEmpty(sql)) sql = GenericQuery<T>(commandType, QueryType.single, sqlParameters);

            T dataObject;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                using (var sqlCommand = new SqlCommand(sql, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;

                    if (sqlParameters != null)
                    {
                        foreach (var parameter in sqlParameters)
                        {
                            sqlCommand.Parameters.Add(parameter);
                        }
                    }

                    dataObject = (await sqlCommand.ExecuteReaderAsync(CommandBehavior.SingleRow)).ReadSingle<T>();
                }

                sqlConnection.Close();
            }

            return dataObject;
        }



        public IEnumerable<T> List<T>(List<SqlParameter> sqlParameters = null, string sql = null, CommandType commandType = CommandType.StoredProcedure)
        {
            if (string.IsNullOrEmpty(sql)) sql = GenericQuery<T>(commandType, QueryType.select, sqlParameters);

            if (commandType == CommandType.Text && sql.Contains("WHERE") && !sql.Contains("@") && (sqlParameters != null || sqlParameters.Count == 0)) throw new Exception("string sql query cannot be allowed when it has 'where' keyword with no parameters");

            IEnumerable<T> data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                using (var sqlCommand = new SqlCommand(sql, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;

                    if (sqlParameters != null)
                    {
                        foreach (var parameter in sqlParameters)
                        {
                            sqlCommand.Parameters.Add(parameter);
                        }
                    }

                    data = sqlCommand.ExecuteReader().ReadList<T>();
                }

                sqlConnection.Close();
            }

            return data;
        }
        public async Task<IEnumerable<T>> ListAsync<T>(List<SqlParameter> sqlParameters = null, string sql = null, CommandType commandType = CommandType.StoredProcedure)
        {
            if (string.IsNullOrEmpty(sql)) sql = GenericQuery<T>(commandType, QueryType.select, sqlParameters);

            if (commandType == CommandType.Text && sql.Contains("WHERE") && !sql.Contains("@") && (sqlParameters != null || sqlParameters.Count == 0)) throw new Exception("string sql query cannot be allowed when it has 'where' keyword with no parameters");

            IEnumerable<T> data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                using (var sqlCommand = new SqlCommand(sql, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;

                    if (sqlParameters != null)
                    {
                        foreach (var parameter in sqlParameters)
                        {
                            sqlCommand.Parameters.Add(parameter);
                        }
                    }

                    data = (await sqlCommand.ExecuteReaderAsync()).ReadList<T>();
                }

                sqlConnection.Close();
            }

            return data;
        }


        public int Count<T>(List<SqlParameter> sqlParameters = null, string sql = null, CommandType commandType = CommandType.StoredProcedure)
        {
            if (string.IsNullOrEmpty(sql)) sql = GenericQuery<T>(commandType, QueryType.count, sqlParameters);

            if (commandType == CommandType.Text && sql.Contains("WHERE") && !sql.Contains("@") && (sqlParameters != null || sqlParameters.Count == 0)) throw new Exception("string sql query cannot be allowed when it has 'where' keyword with no parameters");

            int data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                using (var sqlCommand = new SqlCommand(sql, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;

                    if (sqlParameters != null)
                    {
                        foreach (var parameter in sqlParameters)
                        {
                            sqlCommand.Parameters.Add(parameter);
                        }
                    }

                    try
                    {
                        var result = sqlCommand.ExecuteScalar();
                        data = Convert.ToInt32(result);
                    }
                    catch
                    {
                        data = -2;
                    }
                }

                sqlConnection.Close();
            }

            return data;
        }
        public async Task<int> CountAsync<T>(List<SqlParameter> sqlParameters = null, string sql = null, CommandType commandType = CommandType.StoredProcedure)
        {
            if (string.IsNullOrEmpty(sql)) sql = GenericQuery<T>(commandType, QueryType.count, sqlParameters);

            if (commandType == CommandType.Text && sql.Contains("WHERE") && !sql.Contains("@") && (sqlParameters != null || sqlParameters.Count == 0)) throw new Exception("string sql query cannot be allowed when it has 'where' keyword with no parameters");

            int data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                using (var sqlCommand = new SqlCommand(sql, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;

                    if (sqlParameters != null)
                    {
                        foreach (var parameter in sqlParameters)
                        {
                            sqlCommand.Parameters.Add(parameter);
                        }
                    }

                    try
                    {
                        if (commandType == CommandType.StoredProcedure)
                        {
                            var result = await sqlCommand.ExecuteScalarAsync();
                            data = Convert.ToInt32(result);
                        }
                        else data = sqlCommand.ExecuteNonQuery();
                    }
                    catch
                    {
                        data = -2;
                    }
                }

                sqlConnection.Close();
            }

            return data;
        }



        public bool Any<T>(List<SqlParameter> sqlParameters = null, string sql = null, CommandType commandType = CommandType.StoredProcedure)
        {
            return Single<T>(sqlParameters, sql, commandType) != null ? true : false;
        }
        public async Task<bool> AnyAsync<T>(List<SqlParameter> sqlParameters = null, string sql = null, CommandType commandType = CommandType.StoredProcedure)
        {
            return await SingleAsync<T>(sqlParameters, sql, commandType) != null ? true : false;
        }



        public int InsertOrUpdate<T>(List<SqlParameter> sqlParameters, string sql = null, CommandType commandType = CommandType.StoredProcedure)
        {
            if (sqlParameters == null || sqlParameters.Count == 0) throw new Exception("List<SqlParameter> sqlParameters cannot be null or empty.");

            int Id = GetIdValue<T>(sqlParameters);
            if (Id == -1) throw new Exception($"List<SqlParameter> sqlParameters must have key property of {typeof(T).Name}.");

            if (string.IsNullOrEmpty(sql) && Id == 0) sql = GenericQuery<T>(commandType, QueryType.insert, sqlParameters);
            else if (string.IsNullOrEmpty(sql) && Id > 0) sql = GenericQuery<T>(commandType, QueryType.update, sqlParameters);

            int data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                using (var sqlCommand = new SqlCommand(sql, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;

                    foreach (var parameter in sqlParameters)
                    {
                        sqlCommand.Parameters.Add(parameter);
                    }

                    try
                    {
                        var result = sqlCommand.ExecuteScalar();
                        data = Convert.ToInt32(result);
                    }
                    catch
                    {
                        data = -2;
                    }
                }

                sqlConnection.Close();
            }

            return data;
        }
        public async Task<int> InsertOrUpdateAsync<T>(List<SqlParameter> sqlParameters, string sql = null, CommandType commandType = CommandType.StoredProcedure)
        {
            if (sqlParameters == null || sqlParameters.Count == 0) throw new Exception("List<SqlParameter> sqlParameters cannot be null or empty.");

            int Id = GetIdValue<T>(sqlParameters);
            if (Id == -1) throw new Exception($"List<SqlParameter> sqlParameters must have key property of {typeof(T).Name}.");

            if (string.IsNullOrEmpty(sql) && Id == 0) sql = GenericQuery<T>(commandType, QueryType.insert, sqlParameters);
            else if (string.IsNullOrEmpty(sql) && Id > 0) sql = GenericQuery<T>(commandType, QueryType.update, sqlParameters);

            int data;

            using (var sqlConnection = new SqlConnection(BaseConnectionString))
            {
                sqlConnection.Open();

                using (var sqlCommand = new SqlCommand(sql, sqlConnection))
                {
                    sqlCommand.CommandType = commandType;

                    foreach (var parameter in sqlParameters)
                    {
                        sqlCommand.Parameters.Add(parameter);
                    }

                    try
                    {
                        var result = await sqlCommand.ExecuteScalarAsync();
                        data = Convert.ToInt32(result);
                    }
                    catch
                    {
                        data = -2;
                    }
                }

                sqlConnection.Close();
            }

            return data;
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

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~GenericRepository_ADONET()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
