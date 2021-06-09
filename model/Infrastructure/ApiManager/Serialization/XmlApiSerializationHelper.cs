using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Infrastructure.ApiManager.Serialization
{
    public class XmlApiSerializationHelper<T>
    {
        private readonly Type _apiType;

        public XmlApiSerializationHelper()
        {
            _apiType = typeof(T);
        }

        public T Read(string fileContent)
        {
            T result;
            using (TextReader textReader = new StringReader(fileContent))
            {
                var deserializer = new XmlSerializer(_apiType);
                result = (T) deserializer.Deserialize(textReader);
            }

            return result;
        }
        
        public static string SerializeObjectToString<T1>(T1 serializedObject, bool isMapping)
        {
            var xmlSerializer = new XmlSerializer(serializedObject.GetType());
            using var writer = new StringWriter();
            
            //remove incorrectly placed namespace
            var xsn = new XmlSerializerNamespaces();
            xsn.Add("", "");
            xmlSerializer.Serialize(writer, serializedObject, xsn);
            
            var doc = new XmlDocument();
            doc.LoadXml(writer.ToString());
            
            var newDoc = PrepareDbDocument(doc, isMapping);
            return newDoc.InnerXml;
        }

        private static XmlDocument PrepareDbDocument(XmlDocument apiDocument, bool isMapping)
        {
            var returnDoc = new XmlDocument();
            XmlNode rootElement = returnDoc.CreateElement("on");
            var childElement = isMapping ? returnDoc.CreateElement("programMappings") : returnDoc.CreateElement("programs");

            returnDoc.AppendChild(rootElement);
            returnDoc.DocumentElement?.AppendChild(childElement);
            

            if (apiDocument.DocumentElement == null) return returnDoc;
            var importedNode = returnDoc.ImportNode(apiDocument.DocumentElement, true);
            childElement.AppendChild(importedNode);
            return returnDoc;
        }
    }
}