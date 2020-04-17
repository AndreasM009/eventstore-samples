using Eventstore.Samples.UserQuery.Api.DomainObjects;

namespace Eventstore.Samples.UserQuery.Api.Dto
{
    public class EventStoreEntityDto 
    {
        public string ID {get; set;}
        public int Version {get; set;}
        public User Data {get; set;}
    }
}