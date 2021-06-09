namespace Application.FileManager.Serialization
{
    public interface IXmlSerializationManager
    {
        void Save<T>(string path, object obj);
        T Read<T>(string fileContent);
    }
}