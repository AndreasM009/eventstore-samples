namespace Eventstore.Samples.UserCommand.Api.Events
{
    public class UserUpdated
    {
        public UserUpdated()
        {
            EventType = "UserUpdated";
        }
        
        public string EventType {get; set;}
        public string Id {get; set;}
        public int Version {get; set;}
    }
}