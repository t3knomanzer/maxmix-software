using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication.Messages
{
    internal class MessageRemoveSession : IMessage
    {
        #region Constructor
        public MessageRemoveSession(int id, bool isDevice)
        {
            Id = id;
            IsDevice = isDevice;
        }
        #endregion

        #region Properties
        public int Id { get; private set; }
        public bool IsDevice { get; private set; }
        #endregion

        #region Private Methods
        #endregion

        #region Public Methods
        /*
        * ---------------------------------------
        * CHUNK        TYPE        SIZE (BYTES)
        * ---------------------------------------
        * ID           INT32       4
        * ISDEVICE     BYTE        1
        * ---------------------------------------
        *                          5
        */

        public byte[] GetBytes()
        {
            var result = new List<byte>();

            result.AddRange(BitConverter.GetBytes(Id));
            result.Add(Convert.ToByte(IsDevice));

            return result.ToArray();
        }

        public bool SetBytes(byte[] bytes)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}