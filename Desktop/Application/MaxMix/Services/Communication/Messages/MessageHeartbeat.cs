using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication.Messages
{
    internal class MessageHeartbeat : IMessage
    {
        #region Constructor
        public MessageHeartbeat() { }
        #endregion

        #region Properties
        public byte Revision { get; private set; }
        #endregion

        #region Public Methods

        /*
        * ---------------------------------------------------
        * CHUNK                     TYPE        SIZE (BYTES)
        * ---------------------------------------------------
        */

        public byte[] GetBytes()
        {
            return new byte[] { };
        }

        public bool SetBytes(byte[] bytes)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}