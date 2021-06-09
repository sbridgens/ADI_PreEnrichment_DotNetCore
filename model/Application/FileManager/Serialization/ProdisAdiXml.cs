using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Application.FileManager.Serialization
{
    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    public class ADI
    {
        private ADIAsset assetField;

        private ADIMetadata metadataField;

        /// <remarks />
        public ADIMetadata Metadata
        {
            get => metadataField;
            set => metadataField = value;
        }

        /// <remarks />
        public ADIAsset Asset
        {
            get => assetField;
            set => assetField = value;
        }
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    public class ADIMetadata
    {
        private ADIMetadataAMS aMSField;

        private List<ADIMetadataApp_Data> app_DataField;

        /// <remarks />
        public ADIMetadataAMS AMS
        {
            get => aMSField;
            set => aMSField = value;
        }

        /// <remarks />
        [XmlElement("App_Data")]
        public List<ADIMetadataApp_Data> App_Data
        {
            get => app_DataField;
            set => app_DataField = value;
        }
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    public class ADIMetadataAMS
    {
        private string asset_ClassField;

        private string asset_IDField;

        private string asset_NameField;

        private DateTime creation_DateField;

        private string descriptionField;

        private string productField;

        private string provider_IDField;

        private string providerField;

        private string verbField;

        private int version_MajorField;

        private int version_MinorField;

        /// <remarks />
        [XmlAttribute]
        public string Asset_Class
        {
            get => asset_ClassField;
            set => asset_ClassField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Asset_ID
        {
            get => asset_IDField;
            set => asset_IDField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Asset_Name
        {
            get => asset_NameField;
            set => asset_NameField = value;
        }

        /// <remarks />
        [XmlAttribute(DataType = "date")]
        public DateTime Creation_Date
        {
            get => creation_DateField;
            set => creation_DateField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Description
        {
            get => descriptionField;
            set => descriptionField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Product
        {
            get => productField;
            set => productField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Provider
        {
            get => providerField;
            set => providerField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Provider_ID
        {
            get => provider_IDField;
            set => provider_IDField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Verb
        {
            get => verbField;
            set => verbField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public int Version_Major
        {
            get => version_MajorField;
            set => version_MajorField = Convert.ToInt32(value);
        }

        /// <remarks />
        [XmlAttribute]
        public int Version_Minor
        {
            get => version_MinorField;
            set => version_MinorField = Convert.ToInt32(value);
        }
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    public class ADIMetadataApp_Data
    {
        private string appField;

        private string nameField;

        private string valueField;

        /// <remarks />
        [XmlAttribute]
        public string App
        {
            get => appField;
            set => appField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Name
        {
            get => nameField;
            set => nameField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Value
        {
            get => valueField;
            set => valueField = value;
        }
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    public class ADIAsset
    {
        private List<ADIAssetAsset> assetField;

        private ADIAssetMetadata metadataField;

        /// <remarks />
        public ADIAssetMetadata Metadata
        {
            get => metadataField;
            set => metadataField = value;
        }

        /// <remarks />
        [XmlElement("Asset")]
        public List<ADIAssetAsset> Asset
        {
            get => assetField;
            set => assetField = value;
        }
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    public class ADIAssetMetadata
    {
        private ADIAssetMetadataAMS aMSField;

        private List<ADIAssetMetadataApp_Data> app_DataField;

        /// <remarks />
        public ADIAssetMetadataAMS AMS
        {
            get => aMSField;
            set => aMSField = value;
        }

        /// <remarks />
        [XmlElement("App_Data")]
        public List<ADIAssetMetadataApp_Data> App_Data
        {
            get => app_DataField;
            set => app_DataField = value;
        }
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    public class ADIAssetMetadataAMS
    {
        private string asset_ClassField;

        private string asset_IDField;

        private string asset_NameField;

        private DateTime creation_DateField;

        private string descriptionField;

        private string productField;

        private string provider_IDField;

        private string providerField;

        private string verbField;

        private int version_MajorField;

        private int version_MinorField;

        /// <remarks />
        [XmlAttribute]
        public string Asset_Class
        {
            get => asset_ClassField;
            set => asset_ClassField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Asset_ID
        {
            get => asset_IDField;
            set => asset_IDField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Asset_Name
        {
            get => asset_NameField;
            set => asset_NameField = value;
        }

        /// <remarks />
        [XmlAttribute(DataType = "date")]
        public DateTime Creation_Date
        {
            get => creation_DateField;
            set => creation_DateField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Description
        {
            get => descriptionField;
            set => descriptionField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Product
        {
            get => productField;
            set => productField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Provider
        {
            get => providerField;
            set => providerField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Provider_ID
        {
            get => provider_IDField;
            set => provider_IDField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Verb
        {
            get => verbField;
            set => verbField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public int Version_Major
        {
            get => version_MajorField;
            set => version_MajorField = Convert.ToInt32(value);
        }

        /// <remarks />
        [XmlAttribute]
        public int Version_Minor
        {
            get => version_MinorField;
            set => version_MinorField = Convert.ToInt32(value);
        }
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    public class ADIAssetMetadataApp_Data
    {
        private string appField;

        private string nameField;

        private string valueField;

        /// <remarks />
        [XmlAttribute]
        public string App
        {
            get => appField;
            set => appField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Name
        {
            get => nameField;
            set => nameField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Value
        {
            get => valueField;
            set => valueField = value;
        }
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    public class ADIAssetAsset
    {
        private ADIAssetAssetContent contentField;

        private ADIAssetAssetMetadata metadataField;

        /// <remarks />
        public ADIAssetAssetMetadata Metadata
        {
            get => metadataField;
            set => metadataField = value;
        }

        /// <remarks />
        public ADIAssetAssetContent Content
        {
            get => contentField;
            set => contentField = value;
        }
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    public class ADIAssetAssetMetadata
    {
        private ADIAssetAssetMetadataAMS aMSField;

        private List<ADIAssetAssetMetadataApp_Data> app_DataField;

        /// <remarks />
        public ADIAssetAssetMetadataAMS AMS
        {
            get => aMSField;
            set => aMSField = value;
        }

        /// <remarks />
        [XmlElement("App_Data")]
        public List<ADIAssetAssetMetadataApp_Data> App_Data
        {
            get => app_DataField;
            set => app_DataField = value;
        }
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    public class ADIAssetAssetMetadataAMS
    {
        private string asset_ClassField;

        private string asset_IDField;

        private string asset_NameField;

        private DateTime creation_DateField;

        private string descriptionField;

        private string productField;

        private string provider_IDField;

        private string providerField;

        private string verbField;

        private int version_MajorField;

        private int version_MinorField;

        /// <remarks />
        [XmlAttribute]
        public string Asset_Class
        {
            get => asset_ClassField;
            set => asset_ClassField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Asset_ID
        {
            get => asset_IDField;
            set => asset_IDField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Asset_Name
        {
            get => asset_NameField;
            set => asset_NameField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public int Version_Minor
        {
            get => version_MinorField;
            set => version_MinorField = Convert.ToInt32(value);
        }

        /// <remarks />
        [XmlAttribute]
        public int Version_Major
        {
            get => version_MajorField;
            set => version_MajorField = Convert.ToInt32(value);
        }

        /// <remarks />
        [XmlAttribute(DataType = "date")]
        public DateTime Creation_Date
        {
            get => creation_DateField;
            set => creation_DateField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Description
        {
            get => descriptionField;
            set => descriptionField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Provider_ID
        {
            get => provider_IDField;
            set => provider_IDField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Provider
        {
            get => providerField;
            set => providerField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Product
        {
            get => productField;
            set => productField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Verb
        {
            get => verbField;
            set => verbField = value;
        }
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    public class ADIAssetAssetMetadataApp_Data
    {
        private string appField;

        private string nameField;

        private string valueField;

        /// <remarks />
        [XmlAttribute]
        public string App
        {
            get => appField;
            set => appField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Name
        {
            get => nameField;
            set => nameField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string Value
        {
            get => valueField;
            set => valueField = value;
        }
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    public class ADIAssetAssetContent
    {
        private string valueField;

        /// <remarks />
        [XmlAttribute]
        public string Value
        {
            get => valueField;
            set => valueField = value;
        }
    }
}