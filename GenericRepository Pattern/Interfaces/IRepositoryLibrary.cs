using GenericRepository_Pattern.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericRepository_Pattern.Interfaces
{
    public interface IRepositoryLibrary
    {
        string BaseConnectionString { get; }
        ExampleModelRepository ExampleModels { get; }
    }
}
