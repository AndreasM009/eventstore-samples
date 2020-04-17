namespace Eventstore.Samples.UserCommand.Api.Events
{
    public class UserAdded
    {
        public UserAdded()
        {
            EventType = "UserAdded";
        }
        
        public string EventType {get; set;}
        public string Id {get; set;}
        public int Version {get; set;}
    }
}