namespace Eventstore.Samples.UserQuery.Api.Repositories
{
    public class UserRepositoryOptions
    {
        public string ConnectionString { get; set; }
        public string DatabaseId { get; set; }
        public string ContainerId { get; set; }
        public int Throughput { get; set; }
    }
}