using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using System.Xml.Serialization;

namespace FirmwareInstaller.Models
{
    [Serializable]
    public class VersionModel : IEquatable<VersionModel>
    {
        #region Constructor
        public VersionModel() { }
        public VersionModel(Version version, string url)
        {
            Version = version;
            Url = url;
        }

        public VersionModel(string version, string url)
        {
            Version = new Version(version);
            Url = url;
        }
        #endregion

        #region Properties
        [XmlIgnore]
        public Version Version { get; set; }

        /// <summary>
        /// This is a string representation of the Version property only 
        /// used for serialization since the Version type is not supported.
        /// </summary>
        [XmlAttribute(AttributeName = "version")]
        public string SerializableVersion 
        {  
            get => Version.ToString();
            set => Version = new Version(value);
        }

        /// <summary>
        /// URL of the file to download for this version.
        /// </summary>
        [XmlAttribute(AttributeName = "url")]
        public string Url { get; set; }
        #endregion

        #region IEquatable
        public bool Equals(VersionModel other)
        {
            return other.Version == Version;
        }
        #endregion
    }
}
