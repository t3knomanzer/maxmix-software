using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FirmwareInstaller.Models
{
    [Serializable]
    [XmlRoot(ElementName = "VersionIndex")]
    public class VersionIndexModel
    {
        #region Constructor
        public VersionIndexModel()
        {
            _versions = new List<VersionModel>();
        }
        #endregion


        #region Fields
        private List<VersionModel> _versions;
        #endregion

        #region Properties
        [XmlArray(ElementName = "Versions")]
        [XmlArrayItem(ElementName = "Version")]
        public List<VersionModel> Versions
        {
            get => _versions;
            set => _versions = value;
        }
        #endregion
    }
}
