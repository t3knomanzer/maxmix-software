using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Update
{
    internal class UpdateService
    {
        #region Constructor
        public UpdateService(){}
        #endregion

        #region Events
        #endregion

        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Private Methods
        #endregion

        #region Public Methods
        public async void Start()
        {
            // Get version.xml file from master branch in bitbucket repo
            HttpClient test = new HttpClient();
        }
        #endregion

        #region Event Methods
        #endregion
    }
}
