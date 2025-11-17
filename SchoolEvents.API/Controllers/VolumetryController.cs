using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SchoolEvents.API.Services;

namespace SchoolEvents.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VolumetryController : ControllerBase
    {
        private readonly IGraphService _graphService;
        private readonly ILogger<VolumetryController> _logger;

        public VolumetryController(IGraphService graphService, ILogger<VolumetryController> logger)
        {
            _graphService = graphService;
            _logger = logger;
        }

        [HttpGet("analyze")]
        public async Task<ActionResult> AnalyzeVolumetry()
        {
            try
            {
                var result = await _graphService.AnalyzeVolumetryAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na análise de volumetria");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("users/sample")]
        public async Task<ActionResult> GetUsersSample([FromQuery] int maxUsers = 20)
        {
            try
            {
                var users = await _graphService.GetUsersSampleAsync(maxUsers);
                return Ok(new { count = users.Count(), users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter amostra de usuários");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}