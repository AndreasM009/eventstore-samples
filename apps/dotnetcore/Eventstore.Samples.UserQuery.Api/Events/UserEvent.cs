namespace Eventstore.Samples.UserQuery.Api.Events
{
    public class UserEvent
    {   
        public string EventType {get; set;}
        public string Id {get; set;}
        public int Version {get; set;}
    }
}