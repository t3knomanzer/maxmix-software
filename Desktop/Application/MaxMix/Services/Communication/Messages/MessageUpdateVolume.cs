using CSCore.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication.Messages
{
    internal class MessageUpdateVolume : IMessage
    {
        #region Constructor
        public MessageUpdateVolume() { }

        public MessageUpdateVolume(int id, int volume, bool isMuted, bool isDevice, int deviceFlow = 0)
        {
            Id = id;
            Volume = volume;
            IsMuted = isMuted;
            IsDevice = isDevice;
            DeviceFlow = deviceFlow;
        }
        #endregion

        #region Properties
        public int Id { get; private set; }
        public int Volume { get; private set; }
        public bool IsMuted { get; private set; }
        public bool IsDevice { get; private set; }
        public int DeviceFlow { get; private set; }
        #endregion


        #region Public Methods

        /*
        * ---------------------------------------
        * CHUNK        TYPE        SIZE (BYTES)
        * ---------------------------------------
        * ID           INT32       4
        * VOLUME       BYTE        1
        * ISMUTED      BYTE        1
        * ISDEVICE     BYTE        1
        * DEVICEFLOW   BYTE        1
        * ---------------------------------------
        *                          8
        */

        public byte[] GetBytes()
        {
            var result = new List<byte>();

            result.AddRange(BitConverter.GetBytes(Id));
            result.Add(Convert.ToByte(Volume));
            result.Add(Convert.ToByte(IsMuted));
            result.Add(Convert.ToByte(IsDevice));
            result.Add(Convert.ToByte(DeviceFlow));

            return result.ToArray();
        }

        public bool SetBytes(byte[] bytes)
        {
            Id = BitConverter.ToInt32(new byte[] { bytes[3], bytes[2], bytes[1], bytes[0] }, 0);

            Volume = Convert.ToInt16(bytes[4]);
            IsMuted = Convert.ToBoolean(bytes[5]);

            return true;
        }
        #endregion
    }
}