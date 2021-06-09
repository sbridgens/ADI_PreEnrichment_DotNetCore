using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Application.FileManager.Serialization;

namespace Infrastructure.FileManager
{
    public class XmlSerializationManager : IXmlSerializationManager
    {
        public void Save<T>(string path, object obj)
        {
            using TextWriter textWriter = new StreamWriter(path);
            var serializer = new XmlSerializer(typeof(T));
            serializer.Serialize(textWriter, obj);
            textWriter.Close();
        }

        public T Read<T>(string fileContent)
        {
            using TextReader textReader = new StringReader(fileContent);
            using var reader = new XmlTextReader(textReader) {Namespaces = false};
            var serializer = new XmlSerializer(typeof(T));
            return (T) serializer.Deserialize(reader);
        }
    }
}