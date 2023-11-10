using GenericRepository_Pattern.Repository;

namespace GenericRepository_Pattern.Interfaces
{
    internal interface IDataAccess
    {
        string BaseConnectionString { get; }
        GenericRepository_ADONET GenericRepository_ADONET { get; }
        GenericRepository_Dapper GenericRepository_Dapper { get; }
    }
}
