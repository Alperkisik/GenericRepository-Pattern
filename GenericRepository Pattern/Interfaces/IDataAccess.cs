using GenericRepository_Pattern.DataAccess_Library;

namespace GenericRepository_Pattern.Interfaces
{
    internal interface IDataAccess
    {
        string BaseConnectionString { get; }
        GenericRepository_ADONET GenericRepository_ADONET { get; }
        GenericRepository_Dapper GenericRepository_Dapper { get; }
        IRepositoryLibrary RepositoryLibrary { get; }
    }
}
