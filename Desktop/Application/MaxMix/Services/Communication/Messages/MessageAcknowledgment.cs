using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication.Messages
{
    internal class MessageAcknowledgment : IMessage
    {
        #region Constructor
        public MessageAcknowledgment() { }
        #endregion

        #region Properties
        public byte Revision { get; private set; }
        #endregion

        #region Public Methods
        /*
        * ---------------------------------------------------
        * CHUNK                     TYPE        SIZE (BYTES)
        * ---------------------------------------------------
        * REVISION                   BYTE        1
        * ---------------------------------------------------
        */

        public bool SetBytes(byte[] bytes)
        {
            Revision = bytes[0];
            return true;
        }

        public byte[] GetBytes()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}