using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication
{
    internal class MessageHandShakeRequest : IMessage
    {
        #region Constructor
        public MessageHandShakeRequest() {}
        #endregion

        #region Consts
        #endregion

        #region Fields
        private byte _revision;
        #endregion

        #region Properties
        #endregion

        #region Public Methods
        public byte[] GetBytes()
        {
            return new byte[] { 252 };
        }

        public bool SetBytes(byte[] bytes)
        {
            throw new NotImplementedException("Should never be called");
        }
        public byte GetRevision()
        {
            return _revision;
        }
        public bool SetRevision(byte revision)
        {
            _revision = revision;
            return true;
        }
        #endregion
    }
}