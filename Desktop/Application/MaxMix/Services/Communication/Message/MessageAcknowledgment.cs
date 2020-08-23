using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication
{
    internal class MessageAcknowledgment : IMessage
    {
        #region Constructor
        public MessageAcknowledgment(byte revision) {
            _revision = revision;
        }
        #endregion

        #region Fields
        private byte _revision;
        #endregion

        #region Properties
        public byte Revision { get => _revision; }
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