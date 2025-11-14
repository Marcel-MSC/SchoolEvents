using Microsoft.AspNetCore.Mvc;

namespace SchoolEvents.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PublicController : ControllerBase
    {
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { 
                message = "API está funcionando!", 
                timestamp = DateTime.Now,
                status = "Online"
            });
        }
    }
}