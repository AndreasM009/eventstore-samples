using System;

namespace Eventstore.Samples.UserCommand.Api.DomainObjects
{
    public class User 
    {
        public Guid Id {get; set;}
        public string Firstname {get; set;}
        public string Lastename {get; set;}
    }
}