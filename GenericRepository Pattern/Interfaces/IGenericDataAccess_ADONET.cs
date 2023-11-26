using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericRepository_Pattern.Interfaces
{
    public interface IGenericDataAccess_ADONET<T>
    {
        T GetByParameters(List<SqlParameter> parameters,string query = null, CommandType commandType = CommandType.Text);
        T GetByParameters(object parameters, string query = null, CommandType commandType = CommandType.Text);
        T GetByParameters(Dictionary<string, object> parameters, string query = null, CommandType commandType = CommandType.Text);
        T GetById(int id, string query = null, CommandType commandType = CommandType.Text);
        IEnumerable<T> GetAll(string query = null, CommandType commandType = CommandType.Text);
        IEnumerable<T> GetAllByParameters(List<SqlParameter> parameters, string query = null, CommandType commandType = CommandType.Text);
        IEnumerable<T> GetAllByParameters(object parameters, string query = null, CommandType commandType = CommandType.Text);
        IEnumerable<T> GetAllByParameters(Dictionary<string, object> parameters, string query = null, CommandType commandType = CommandType.Text);
        bool InsertOrUpdate(int id, List<SqlParameter> parameters, string query = null, CommandType commandType = CommandType.Text);
        bool InsertOrUpdate(int id, object parameters, string query = null, CommandType commandType = CommandType.Text);
        bool InsertOrUpdate(int id, Dictionary<string, object> parameters, string query = null, CommandType commandType = CommandType.Text);
    }
}
