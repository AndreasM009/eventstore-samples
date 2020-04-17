using System;
using System.ComponentModel.DataAnnotations;

namespace Eventstore.Samples.UserCommand.Api.Dto
{
    public class UserDto 
    {
        public Guid? Id {get; set;}
        public int? Version {get; set;}
        [Required]
        public string Firstname {get; set;}
        [Required]
        public string Lastname {get; set;}
    }
}