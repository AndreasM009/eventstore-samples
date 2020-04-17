using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dapr;
using Dapr.Client;
using Eventstore.Samples.UserQuery.Api.Dto;
using Eventstore.Samples.UserQuery.Api.Events;
using Eventstore.Samples.UserQuery.Api.Repositories;
using Eventstore.Samples.UserQuery.Api.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Eventstore.Samples.UserQuery.Api.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class UserEventController : ControllerBase
    {
        private readonly EventStoreOptions _eventStoreOptions;
        private readonly LoopbackQueueOptions _loopbackQueueOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _evtStoreBaseUrl;
        private readonly UserRepository _repository;
        private readonly DaprClient _daprClient; 
        private ILogger<UserEventController> _logger;

        public UserEventController(
            IOptions<EventStoreOptions> eventStoreOptions, 
            IOptions<LoopbackQueueOptions> LoopbackQueueOptions,
            IHttpClientFactory httpClientfactory, 
            UserRepository repository,
            DaprClient daprClient,
            ILogger<UserEventController> logger)
        {
            _eventStoreOptions = eventStoreOptions.Value;
            _loopbackQueueOptions = LoopbackQueueOptions.Value;
            _httpClientFactory = httpClientfactory;
            _evtStoreBaseUrl = $"http://{_eventStoreOptions.Host}:{_eventStoreOptions.Port}/eventstores/{_eventStoreOptions.Name}/entities";
            _repository = repository;
            _daprClient = daprClient;
            _logger = logger;
        }
        
        [Topic("usertopic")]
        [HttpPost("usertopic")]
        public async Task<IActionResult> HandleUserEvent(UserEvent evtUser)
        {
            try
            {
                _logger.LogInformation($"Received: {evtUser.EventType} {evtUser.Id} with version {evtUser.Version}");

                // load eventstore version of user
                var ety = await GetByVersion(evtUser.Id, evtUser.Version);
                if (null == ety)
                {
                    return StatusCode((int)HttpStatusCode.NotFound);
                }

                if (evtUser.EventType.ToLower() == "useradded")
                {
                    var user = ety.Data;
                    user.Version = ety.Version;
                    await _repository.Add(user);
                }
                else if (evtUser.EventType.ToLower() == "userupdated")
                {
                    // first load current user to check the version
                    var savedUser = await _repository.GetUserById(ety.Data.Id);
                    if (null == savedUser)
                    {
                        // concurrency problem? Maybe the user is still not added
                        // Just return, the request which adds the user is responsible to requeue 
                        // newer version
                        return Ok();
                    }

                    // check version 
                    if (savedUser.Item1.Version >= ety.Version)
                    {
                        // nothing todo, a new version is already available
                        return Ok();
                    }

                    // try to update the user 
                    var user = ety.Data;
                    user.Version = ety.Version;

                    var result = await _repository.Save(user, savedUser.Item2);
                    if (null == result)
                    {
                        // a changed was already made to the current user (Optimistic Offline Control)
                        // The request that updated the user is responsible to reque newer versions if available
                        return Ok();
                    }
                }
                else
                {
                    return BadRequest("Unknown event type");
                }

                // now check if a new version is available and if so push it 
                // to the loopback queue
                var latestEty = await GetByLatestVersion(evtUser.Id);
                if (null == latestEty)
                {
                    return Ok();
                }

                if (latestEty.Version > evtUser.Version)
                {
                    // Todo, push it to the loopback queue
                    var evt = new UserEvent
                    {
                        EventType = "UserUpdated",
                        Version = latestEty.Version,
                        Id = latestEty.ID
                    };

                    await _daprClient.InvokeBindingAsync(_loopbackQueueOptions.UserOutputBindingName, evt);
                }
        
                return Ok();
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e);
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        [Route("userquery-loopback-queue")]
        [HttpPost]
        public async Task<IActionResult> HandleLoopbackQueueEvent([FromBody]UserEvent evtUser)
        {
            _logger.LogInformation($"Received loopback event: {evtUser.Id} / {evtUser.EventType}");
            return await HandleUserEvent(evtUser);
        }

        #region Internal
        private async Task<EventStoreEntityDto> GetByVersion(string id, int version)
        {
            var client = _httpClientFactory.CreateClient();
            var resp = await client.GetAsync($"{_evtStoreBaseUrl}/{id}?version={version}");

            if (resp.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            var json = await resp.Content.ReadAsStringAsync();
            var ety = JsonConvert.DeserializeObject<EventStoreEntityDto>(json);
            return ety;
        }

        private async Task<EventStoreEntityDto> GetByLatestVersion(string id)
        {
            var client = _httpClientFactory.CreateClient();
            var resp = await client.GetAsync($"{_evtStoreBaseUrl}/{id}");

            if (resp.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            var json = await resp.Content.ReadAsStringAsync();
            var ety = JsonConvert.DeserializeObject<EventStoreEntityDto>(json);
            return ety;
        }
        #endregion
    }
}