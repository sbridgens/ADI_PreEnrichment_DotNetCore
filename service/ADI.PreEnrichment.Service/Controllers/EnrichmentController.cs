using Application.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ADI.PreEnrichment.Service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EnrichmentController : ControllerBase
    {
        private readonly ILogger<EnrichmentController> _logger;
        private readonly EnrichmentSettings _options;

        public EnrichmentController(IOptions<EnrichmentSettings> options, ILogger<EnrichmentController> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        [HttpGet]
        public EnrichmentSettings Get()
        {
            return _options;
        }
    }
}