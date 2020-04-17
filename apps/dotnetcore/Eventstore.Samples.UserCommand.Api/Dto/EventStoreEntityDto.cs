using Eventstore.Samples.UserCommand.Api.DomainObjects;

namespace Eventstore.Samples.UserCommand.Api.Dto
{
    public class EventStoreEntityDto 
    {
        public string ID {get; set;}
        public int Version {get; set;}
        public User Data {get; set;}
    }
}