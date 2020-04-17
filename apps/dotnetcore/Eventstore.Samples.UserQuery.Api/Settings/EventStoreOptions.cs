namespace Eventstore.Samples.UserQuery.Api.Settings
{
    public class EventStoreOptions 
    {
        public int Port {get; set;}
        public string Name {get; set;}

        public string Host {get; set;}
        public string TopicName {get; set;}
    }
}