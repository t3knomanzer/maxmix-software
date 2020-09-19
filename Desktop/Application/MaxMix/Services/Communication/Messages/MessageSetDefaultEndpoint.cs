using MaxMix.Services.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication.Messages
{
    internal class MessageSetDefaultEndpoint : IMessage
    {
        #region Constructor
        public MessageSetDefaultEndpoint() { }
        public MessageSetDefaultEndpoint(int id, int deviceFlow)
        {
            Id = id;
            DeviceFlow = deviceFlow;
        }
        #endregion

        #region Properties
        public int Id { get; private set; }
        public int DeviceFlow{ get; private set; }
        #endregion

        #region Private Methods
        #endregion

        #region Public Methods

        /*
        * ---------------------------------------
        * CHUNK        TYPE        SIZE (BYTES)
        * ---------------------------------------
        * ID           INT32       4
        * DEVICEFLOW   BYTE        1
        * ---------------------------------------
        *                          5
        */
        public byte[] GetBytes()
        {
            var result = new List<byte>();

            result.AddRange(BitConverter.GetBytes(Id));
            result.Add(Convert.ToByte(DeviceFlow));
            return result.ToArray();
        }

        public bool SetBytes(byte[] bytes)
        {
            Id = BitConverter.ToInt32(new byte[] { bytes[3], bytes[2], bytes[1], bytes[0] }, 0);
            return true;
        }
        #endregion
    }
}