using GenericRepository_Pattern.DataAccess_Library;
using GenericRepository_Pattern.Interfaces.Repository;
using GenericRepository_Pattern.Models.Database_Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericRepository_Pattern.Repository
{
    public class ExampleModelRepository : GenericDataAccess_ADONET<ExampleModel>, IExampleModelRepository
    {
        public ExampleModelRepository(string ConnectionString) : base(ConnectionString)
        {

        }

        /// <summary>
        /// <para>override Example</para>
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="query">It can be null.</param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public override IEnumerable<ExampleModel> GetAllByParameters(Dictionary<string, object> parameters, string query = null, CommandType commandType = CommandType.Text)
        {
            /*
             * Validate parameters according to your model
             * Your other Codes
            */

            return base.GetAllByParameters(parameters, query, commandType);
        }

        /// <summary>
        /// <para>Custom Function Example</para>
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public IEnumerable<ExampleModel> RecordsByParameters(Dictionary<string, object> parameters)
        {
            /*
             * Validate parameters according to your model
             * Your other Codes
            */
            string query = "{Your Query}";

            return base.GetAllByParameters(parameters, query, CommandType.StoredProcedure);
        }
    }
}
