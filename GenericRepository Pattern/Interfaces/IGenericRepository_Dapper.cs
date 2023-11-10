using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericRepository_Pattern.Interfaces
{
    internal interface IGenericRepository_Dapper
    {
        string BaseConnectionString { get; }

        T Single<T>(Dictionary<string, object> parameters = null, string query = null, CommandType commandType = CommandType.Text);
        Task<T> SingleAsync<T>(Dictionary<string, object> parameters, string query = null, CommandType commandType = CommandType.Text);



        T SingleById<T>(int id, string query = null, CommandType commandType = CommandType.Text);
        Task<T> SingleByIdAsync<T>(int id, string query = null, CommandType commandType = CommandType.Text);



        T SingleData<T>(string query, Dictionary<string, object> parameters = null, CommandType commandType = CommandType.Text);
        Task<T> SingleDataAsync<T>(string query, Dictionary<string, object> parameters = null, CommandType commandType = CommandType.Text);



        bool Any<T>(Dictionary<string, object> parameters, string query = null, CommandType commandType = CommandType.Text);
        Task<bool> AnyAsync<T>(Dictionary<string, object> parameters, string query = null, CommandType commandType = CommandType.Text);



        int Count<T>(Dictionary<string, object> parameters = null, string query = null, CommandType commandType = CommandType.Text);
        Task<int> CountAsync<T>(Dictionary<string, object> parameters = null, string query = null, CommandType commandType = CommandType.Text);



        IEnumerable<T> List<T>(Dictionary<string, object> parameters = null, string query = null, CommandType commandType = CommandType.Text);
        Task<IEnumerable<T>> ListAsync<T>(Dictionary<string, object> parameters = null, string query = null, CommandType commandType = CommandType.Text);



        bool Insert<T>(T model, CommandType commandType = CommandType.Text);
        Task<bool> InsertAsync<T>(T model, CommandType commandType = CommandType.Text);



        bool Update<T>(Dictionary<string, object> parameters, CommandType commandType = CommandType.Text);
        Task<bool> UpdateAsync<T>(Dictionary<string, object> parameters, CommandType commandType = CommandType.Text);
    }
}
