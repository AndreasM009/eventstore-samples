using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dapr.Client;
using Eventstore.Samples.UserCommand.Api.Dto;
using Eventstore.Samples.UserCommand.Api.Events;
using Eventstore.Samples.UserCommand.Api.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Eventstore.Samples.UserCommand.Api.Controllers
{
    [ApiController]
    [Route("users")]
    [Produces("application/json")]
    public class UserController : ControllerBase
    {
        private readonly EventStoreOptions _eventStoreOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _evtStoreBaseUrl;
        private readonly DaprClient _daprClient;

        public UserController(IOptions<EventStoreOptions> eventStoreOptions, IHttpClientFactory httpClientfactory, DaprClient daprClient)
        {
            _eventStoreOptions = eventStoreOptions.Value;
            _httpClientFactory = httpClientfactory;
            _evtStoreBaseUrl = $"http://{_eventStoreOptions.Host}:{_eventStoreOptions.Port}/eventstores/{_eventStoreOptions.Name}/entities";
            _daprClient = daprClient;
        }

        [HttpPost]
        [ProducesResponseType(typeof(UserDto), (int)HttpStatusCode.Created)]        
        public async Task<IActionResult> Add([FromBody]UserDto user)
        {
            user.Id = Guid.NewGuid();
            user.Version = 0;

            var ety = new EventStoreEntityDto{
                ID = user.Id.ToString(),
                Version = 1,
                Data = user.ToUser()
            };

            var json = JsonConvert.SerializeObject(ety, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver() });

            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await client.PostAsync($"{_evtStoreBaseUrl}/{user.Id}", content);

            if (resp.StatusCode != HttpStatusCode.Created)
            {
                var err = await resp.Content.ReadAsStringAsync();
                return StatusCode((int)resp.StatusCode, err);
            }
            
            var tmp = await resp.Content.ReadAsStringAsync();
            var respUser = JsonConvert.DeserializeObject<EventStoreEntityDto>(tmp);
            user.Version = respUser.Version;

            // publish events
            await _daprClient.PublishEventAsync(_eventStoreOptions.TopicName, new UserAdded{
                Id = user.Id.ToString(),
                Version = user.Version.Value
            });

            return Created("QueryUserApi/todo", user);
        }

        [HttpPut("{id}")]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(UserDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Save(Guid id, [FromBody]UserDto user)
        {
            var ety = new EventStoreEntityDto{
                ID = user.Id.ToString(),
                Version = user.Version.Value,
                Data = user.ToUser()
            };

            var json = JsonConvert.SerializeObject(ety, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver() });

            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await client.PutAsync($"{_evtStoreBaseUrl}/{user.Id}", content);

            if (resp.StatusCode != HttpStatusCode.OK)
            {
                var err = await resp.Content.ReadAsStringAsync();
                return StatusCode((int)resp.StatusCode, err);
            }

            var tmp = await resp.Content.ReadAsStringAsync();
            var respUser = JsonConvert.DeserializeObject<EventStoreEntityDto>(tmp);

            var result = new UserDto();
            result.FromUser(respUser.Data);
            result.Version = respUser.Version;

            await _daprClient.PublishEventAsync(_eventStoreOptions.TopicName, new UserUpdated
            {
                Id = respUser.ID,
                Version = respUser.Version
            });

            return Ok(result);
        }
    }
}