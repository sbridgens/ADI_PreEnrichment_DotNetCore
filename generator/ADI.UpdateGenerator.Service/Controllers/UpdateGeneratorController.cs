using Application.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ADI.UpdateGenerator.Service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UpdateGeneratorController : ControllerBase
    {
        private readonly ILogger<UpdateGeneratorController> _logger;
        private readonly GN_UpdateTracker_Config _options;

        public UpdateGeneratorController(IOptions<GN_UpdateTracker_Config> options, ILogger<UpdateGeneratorController> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        [HttpGet]
        public GN_UpdateTracker_Config Get()
        {
            return _options;
        }
    }
}