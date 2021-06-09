using Application.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ADI.UpdateTracking.Service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UpdateTrackingController : ControllerBase
    {
        private readonly ILogger<UpdateTrackingController> _logger;
        private readonly GN_UpdateTracker_Config _options;

        public UpdateTrackingController(IOptions<GN_UpdateTracker_Config> options, ILogger<UpdateTrackingController> logger)
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