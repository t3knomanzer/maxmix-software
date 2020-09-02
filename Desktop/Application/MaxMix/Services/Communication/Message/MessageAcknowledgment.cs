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
        public MessageAcknowledgment() {}
        #endregion

        #region Fields
        private byte _revision;
        #endregion

        #region Properties
        #endregion

        #region Public Methods
        public byte[] GetBytes()
        {
            return new byte[] {_revision};
        }

        public bool SetBytes(byte[] bytes)
        {
            return true;
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