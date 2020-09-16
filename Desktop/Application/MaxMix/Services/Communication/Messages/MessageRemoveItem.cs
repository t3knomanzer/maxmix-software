using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication.Messages
{
    internal class MessageRemoveItem : IMessage
    {
        #region Constructor
        public MessageRemoveItem(int id, bool isDevice, int deviceFlow = 0)
        {
            Id = id;
            IsDevice = isDevice;
            DeviceFlow = deviceFlow;
        }
        #endregion

        #region Properties
        public int Id { get; private set; }
        public bool IsDevice { get; private set; }
        public int DeviceFlow { get; private set; }
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
        * DEVICEFLOW   BYTE        1
        * ---------------------------------------
        *                          6
        */

        public byte[] GetBytes()
        {
            var result = new List<byte>();

            result.AddRange(BitConverter.GetBytes(Id));
            result.Add(Convert.ToByte(IsDevice));
            result.Add(Convert.ToByte(DeviceFlow));

            return result.ToArray();
        }

        public bool SetBytes(byte[] bytes)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}