using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SchoolEvents.API.Services;

namespace SchoolEvents.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IGraphService _graphService;
        
        public UsersController(IGraphService graphService) 
        {
            _graphService = graphService;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _graphService.GetUsersSampleAsync();
            return Ok(users);
        }
        
        [HttpGet("{userId}/events")]
        public async Task<IActionResult> GetUserEvents(string userId)
        {
            var events = await _graphService.GetUserEventsAsync(userId);
            return Ok(events);
        }
    }
}