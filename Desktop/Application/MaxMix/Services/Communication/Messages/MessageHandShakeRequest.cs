using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication.Messages
{
    internal class MessageHandShakeRequest : IMessage
    {
        #region Constructor
        public MessageHandShakeRequest() { }
        #endregion

        #region Public Methods
        public byte[] GetBytes()
        {
            return new byte[] { 252 };
        }

        public bool SetBytes(byte[] bytes)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}