using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.MongoDB
{
    public interface IMongoConnectionFactory
    {
        Task<IMongoDatabase> GetDatabaseAsync();
    }
}

