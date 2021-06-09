using Application.FileManager.Serialization;
using Application.Models;

namespace Application.Validation.Contracts
{
    public interface IAdiValidator
    {
        AdiValidationResult ValidatePaidValue(ADI adi);
    }
}