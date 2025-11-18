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
        private readonly IGraphService _graphService;  // ← MUDAR PARA IGraphService
        
        public UsersController(IGraphService graphService)  // ← MUDAR PARA IGraphService
        {
            _graphService = graphService;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _graphService.GetUsersSampleAsync();  // ← MUDAR PARA GetUsersSampleAsync
            return Ok(users);
        }
        
        [HttpGet("{userId}/events")]
        public async Task<IActionResult> GetUserEvents(string userId)
        {
            var events = await _graphService.GetUserEventsAsync(userId);  // ← Já está correto
            return Ok(events);
        }
    }
}