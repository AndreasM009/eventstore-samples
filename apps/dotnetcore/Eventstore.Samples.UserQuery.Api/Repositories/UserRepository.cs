using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Eventstore.Samples.UserQuery.Api.Repositories
{
    public class UserRepository 
    {
        private static object _syncRoot = new object();
        private static bool _containerCreated = false;
        private readonly UserRepositoryOptions _options;

        public UserRepository(IOptions<UserRepositoryOptions> options)
        {
            _options = options.Value;
        }

        public async Task<Tuple<DomainObjects.User, string>> GetUserById(Guid id)
        {
            var client = GetClient();
            var container = client.GetContainer(_options.DatabaseId, _options.ContainerId);

            try
            {
                var user = await container.ReadItemAsync<DomainObjects.User>(id.ToString(), new PartitionKey(id.ToString()));
                return Tuple.Create(user.Resource, user.ETag);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<Tuple<DomainObjects.User, string>> Add(DomainObjects.User user)
        {
            var client = GetClient();
            var container = client.GetContainer(_options.DatabaseId, _options.ContainerId);

            var result = await container.CreateItemAsync(user, new PartitionKey(user.Id.ToString()));
            return Tuple.Create(result.Resource, result.ETag);
        }

        public async Task<Tuple<DomainObjects.User, string>> Save(DomainObjects.User user, string etag)
        {
            var client = GetClient();
            var container = client.GetContainer(_options.DatabaseId, _options.ContainerId);

            try
            {
                var opts = new ItemRequestOptions { IfMatchEtag = etag };

                var result = await container.ReplaceItemAsync(user, user.Id.ToString(), new PartitionKey(user.Id.ToString()), opts);

                return Tuple.Create(result.Resource, result.ETag);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed || ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        private CosmosClient GetClient()
        {
            var options = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                ConsistencyLevel = ConsistencyLevel.Eventual
            };

            options.SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            };

            var client = new CosmosClient(_options.ConnectionString, options);

            if (!_containerCreated)
            {
                lock (_syncRoot)
                {
                    if (!_containerCreated)
                    {
                        var db = client.CreateDatabaseIfNotExistsAsync(_options.DatabaseId).GetAwaiter().GetResult().Database;
                        db.DefineContainer(_options.ContainerId, "/id")
                            .WithIndexingPolicy()                                
                                .WithAutomaticIndexing(false)
                                .WithIndexingMode(IndexingMode.None)
                                .Attach()
                            .CreateIfNotExistsAsync().GetAwaiter().GetResult();

                        _containerCreated = true;
                    }
                }
            }            

            return client;
        }
    }
}