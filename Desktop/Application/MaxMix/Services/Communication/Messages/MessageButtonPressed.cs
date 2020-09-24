using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication.Messages
{
    internal class MessageButtonPressed : IMessage
    {
        #region Constructor
        public MessageButtonPressed() { }
        #endregion

        #region Properties
        public byte ButtonId { get; private set; }
        #endregion

        #region Public Methods

        /*
        * ---------------------------------------------------
        * CHUNK                     TYPE        SIZE (BYTES)
        * ---------------------------------------------------
        * BUTTONID                  BYTE        1
        * ---------------------------------------------------
        */

        public byte[] GetBytes()
        {
            throw new NotImplementedException();
        }

        public bool SetBytes(byte[] bytes)
        {
            ButtonId = bytes[0];
            return true;
        }
        #endregion
    }
}