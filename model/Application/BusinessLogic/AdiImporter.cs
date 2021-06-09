using System.IO;
using Application.BusinessLogic.Contracts;
using Application.FileManager.Serialization;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace Application.BusinessLogic
{
    public class AdiImporter : IAdiImporter
    {
        private readonly ILogger<AdiImporter> _logger;
        private readonly IXmlSerializationManager _serializationManager;

        public AdiImporter(ILogger<AdiImporter> logger, IXmlSerializationManager serializationManager)
        {
            _logger = logger;
            _serializationManager = serializationManager;
        }

        public Result<ADI> ReadFromXml(string xmlFile)
        {
            _logger.LogInformation("Validating ADI XML is well formed");
            return Result
                .Try(() => File.ReadAllText(xmlFile))
                .OnSuccessTry(s => _serializationManager.Read<ADI>(s), exception =>
                {
                    _logger.LogError(exception, exception.Message);
                    return exception.Message;
                });
        }
    }
}