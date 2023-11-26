using GenericRepository_Pattern.Models.Database_Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericRepository_Pattern.Interfaces.Repository
{
    public interface IExampleModelRepository
    {
        IEnumerable<ExampleModel> RecordsByParameters(Dictionary<string, object> parameters);
    }
}
