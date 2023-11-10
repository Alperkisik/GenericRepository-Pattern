using GenericRepository_Pattern.Interfaces;
using GenericRepository_Pattern.Repository;
using System;

namespace GenericRepository_Pattern
{
    public class DataAccess : IDataAccess, IDisposable
    {
        readonly string _ConnectionString;
        private bool disposedValue;

        readonly GenericRepository_ADONET _GenericRepository_ADONET;
        readonly GenericRepository_Dapper _genericRepository_Dapper;

        public DataAccess(string ConnectionString)
        {
            _ConnectionString = ConnectionString;

            _GenericRepository_ADONET = new GenericRepository_ADONET(ConnectionString);
            _genericRepository_Dapper = new GenericRepository_Dapper(ConnectionString);
        }

        public string BaseConnectionString => _ConnectionString;

        public GenericRepository_ADONET GenericRepository_ADONET => _GenericRepository_ADONET;
        public GenericRepository_Dapper GenericRepository_Dapper => _genericRepository_Dapper;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _GenericRepository_ADONET.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DataAccess()
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
