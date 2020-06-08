using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication
{
    internal class MessageHandShakeResponse : IMessage
    {
        #region Constructor
        public MessageHandShakeResponse() { }
        #endregion

        #region Consts
        #endregion

        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Public Methods
        public byte[] GetBytes()
        {
            throw new NotImplementedException("Should never be called");
        }

        public bool SetBytes(byte[] bytes)
        {
            return true;
        }
        #endregion
    }
}