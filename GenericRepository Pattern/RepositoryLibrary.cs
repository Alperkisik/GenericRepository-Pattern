using GenericRepository_Pattern.DataAccess_Library;
using GenericRepository_Pattern.Interfaces;
using GenericRepository_Pattern.Models.Database_Models;
using GenericRepository_Pattern.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericRepository_Pattern
{
    public class RepositoryLibrary : IRepositoryLibrary, IDisposable
    {
        readonly string _BaseConnectionString;
        readonly ExampleModelRepository _ExampleModels;
        private bool disposedValue;

        public RepositoryLibrary(string ConnectionString)
        {
            _BaseConnectionString = ConnectionString;

            _ExampleModels = new ExampleModelRepository(ConnectionString);
        }

        public string BaseConnectionString => _BaseConnectionString;
        public ExampleModelRepository ExampleModels => _ExampleModels;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _ExampleModels.Dispose();
                }
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RepositoryLibrary()
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
