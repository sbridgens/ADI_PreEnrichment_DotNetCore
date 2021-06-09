using Application.FileManager.Serialization;
using CSharpFunctionalExtensions;

namespace Application.BusinessLogic.Contracts
{
    public interface IAdiImporter
    {
        Result<ADI> ReadFromXml(string xmlFile);
    }
}