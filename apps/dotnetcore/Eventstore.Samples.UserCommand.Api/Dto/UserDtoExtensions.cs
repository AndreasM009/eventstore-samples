using Eventstore.Samples.UserCommand.Api.DomainObjects;

namespace Eventstore.Samples.UserCommand.Api.Dto
{
    public static class UserDtoExtensions
    {
        public static User ToUser(this UserDto dto)
        {
            return new User 
            {
                Id = dto.Id.Value,
                Firstname = dto.Firstname,
                Lastename = dto.Lastname
            };
        }

        public static void FromUser(this UserDto dto, User user)
        {
            dto.Id = user.Id;
            dto.Firstname = user.Firstname;
            dto.Lastname = user.Lastename;
        }
    }
}