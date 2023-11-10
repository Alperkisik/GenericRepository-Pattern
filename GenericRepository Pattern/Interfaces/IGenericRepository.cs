using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericRepository_Pattern.Interfaces
{
    internal interface IGenericRepository_ADONET
    {
        string BaseConnectionString { get; }

        T SingleData<T>(string sql, List<SqlParameter> sqlParameters = null, CommandType commandType = CommandType.StoredProcedure);
        Task<T> SingleDataAsync<T>(string sql, List<SqlParameter> sqlParameters = null, CommandType commandType = CommandType.StoredProcedure);



        T Single<T>(List<SqlParameter> sqlParameters = null, string sql = null, CommandType commandType = CommandType.StoredProcedure);
        Task<T> SingleAsync<T>(List<SqlParameter> sqlParameters = null, string sql = null, CommandType commandType = CommandType.StoredProcedure);



        IEnumerable<T> List<T>(List<SqlParameter> sqlParameters = null, string sql = null, CommandType commandType = CommandType.StoredProcedure);
        Task<IEnumerable<T>> ListAsync<T>(List<SqlParameter> sqlParameters = null, string sql = null, CommandType commandType = CommandType.StoredProcedure);



        int Count<T>(List<SqlParameter> sqlParameters = null, string sql = null, CommandType commandType = CommandType.StoredProcedure);
        Task<int> CountAsync<T>(List<SqlParameter> sqlParameters = null, string sql = null, CommandType commandType = CommandType.StoredProcedure);



        bool Any<T>(List<SqlParameter> sqlParameters = null, string sql = null, CommandType commandType = CommandType.StoredProcedure);
        Task<bool> AnyAsync<T>(List<SqlParameter> sqlParameters = null, string sql = null, CommandType commandType = CommandType.StoredProcedure);



        int InsertOrUpdate<T>(List<SqlParameter> sqlParameters, string sql = null, CommandType commandType = CommandType.StoredProcedure);
        Task<int> InsertOrUpdateAsync<T>(List<SqlParameter> sqlParameters, string sql = null, CommandType commandType = CommandType.StoredProcedure);
    }

}
