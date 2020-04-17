using System;

namespace Eventstore.Samples.UserQuery.Api.Dto
{
    public class UserDto 
    {
        public Guid Id {get; set;}
        public int Version {get; set;}
        public string Firstname {get; set;}
        public string Lastname {get; set;}
    }
}