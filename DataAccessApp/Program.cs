using GenericRepository_Pattern;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var dataAccess = new DataAccess(ConnectionString: "[Your Connection String]");

            /*

            var Repositories = dataAccess.RepositoryLibrary;

            var ExampleModelRepository = Repositories.ExampleModels;
            ExampleModelRepository.GetAll();
            ExampleModelRepository.GetById(5);
            ExampleModelRepository.RecordsByParameters(null);

            */

            /*
            
            dataAccess.GenericRepository_ADONET.List<T>();
            dataAccess.GenericRepository_ADONET.ListAsync<T>();
            dataAccess.GenericRepository_ADONET.Single<T>();
            dataAccess.GenericRepository_ADONET.SingleAsync<T>();
            dataAccess.GenericRepository_ADONET.SingleData<T>();
            dataAccess.GenericRepository_ADONET.SingleDataAsync<T>();
            dataAccess.GenericRepository_ADONET.Any<T>();
            dataAccess.GenericRepository_ADONET.AnyAsync<T>();
            dataAccess.GenericRepository_ADONET.Count<T>();
            dataAccess.GenericRepository_ADONET.CountAsync<T>();
            dataAccess.GenericRepository_ADONET.InsertOrUpdate<T>();
            dataAccess.GenericRepository_ADONET.InsertOrUpdateAsync<T>();
            
            */

            Console.ReadKey();
        }
    }
}
