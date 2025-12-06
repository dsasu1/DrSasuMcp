using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.MongoDB
{
    public class MongoConnectionFactory : IMongoConnectionFactory
    {
        public async Task<IMongoDatabase> GetDatabaseAsync()
        {
            var connectionString = GetConnectionString();
            var databaseName = GetDatabaseName(connectionString);

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);

            // Verify connection by pinging the actual database that will be used
            await database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1));

            return database;
        }

        private static string GetConnectionString()
        {
            var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");

            return string.IsNullOrEmpty(connectionString)
                ? throw new InvalidOperationException("Connection string is not set in the environment variable 'MONGODB_CONNECTION_STRING'.\n\nHINT: For a local MongoDB instance, run `SET MONGODB_CONNECTION_STRING=mongodb://localhost:27017/test` (Windows) or `export MONGODB_CONNECTION_STRING=mongodb://localhost:27017/test` (Linux/Mac)")
                : connectionString;
        }

        private static string GetDatabaseName(string connectionString)
        {
            try
            {
                var url = new MongoUrl(connectionString);
                if (!string.IsNullOrEmpty(url.DatabaseName))
                {
                    return url.DatabaseName;
                }

                // If database name is not in connection string, try to extract from path
                var uri = new Uri(connectionString.Replace("mongodb://", "http://").Replace("mongodb+srv://", "https://"));
                var path = uri.AbsolutePath.TrimStart('/');
                
                // Remove query string if present
                if (path.Contains('?'))
                {
                    path = path.Substring(0, path.IndexOf('?'));
                }
                
                if (!string.IsNullOrEmpty(path))
                {
                    return path;
                }

                // Default database name
                return "test";
            }
            catch
            {
                // If parsing fails, use default
                return "test";
            }
        }
    }
}

