using System.Text.RegularExpressions;
using Application.FileManager.Serialization;
using Application.Models;
using Application.Validation.Contracts;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Validation
{
    public class AdiValidator : IAdiValidator
    {
        private readonly ILogger<AdiValidator> _logger;

        public AdiValidator(ILogger<AdiValidator> logger)
        {
            _logger = logger;
        }

        public AdiValidationResult ValidatePaidValue(ADI adi)
        {
            var adiPaid = adi.Asset.Metadata.AMS.Asset_ID;
            var result = new AdiValidationResult();
            if (adiPaid.Length == 20)
            {
                result.IsQamAsset = false;
                result.OnapiProviderid = $"{adi.Asset.Metadata.AMS.Provider_ID}{adiPaid}";
                return result;
            }

            var tmpPaid = Regex.Replace(adiPaid, "[A-Za-z]", "").TrimStart('0');
            result.NewTitlPaid = $"TITL{new string('0', 16 - tmpPaid.Length)}{tmpPaid}";
            _logger.LogInformation($"Qam asset detected setting GN_Paid = {adiPaid}, " +
                                   $"ADI Titl Paid Value = {result.NewTitlPaid}");

            result.IsQamAsset = true;
            result.OnapiProviderid = $"{adi.Asset.Metadata.AMS.Provider_ID}{adiPaid}";
            _logger.LogInformation($"On api Provider id = {result.OnapiProviderid}");

            return result;
        }
    }
}