using CSCore.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication.Messages
{
    internal class MessageUpdateVolumeSession : IMessage
    {
        #region Constructor
        public MessageUpdateVolumeSession() { }

        public MessageUpdateVolumeSession(int id, int volume, bool isMuted, bool isDevice)
        {
            Id = id;
            Volume = volume;
            IsMuted = isMuted;
            IsDevice = isDevice;
        }
        #endregion

        #region Properties
        public int Id { get; private set; }
        public int Volume { get; private set; }
        public bool IsMuted { get; private set; }
        public bool IsDevice { get; private set; }
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
        * ---------------------------------------
        *                          7
        */

        public byte[] GetBytes()
        {
            var result = new List<byte>();

            result.AddRange(BitConverter.GetBytes(Id));
            result.Add(Convert.ToByte(Volume));
            result.Add(Convert.ToByte(IsMuted));
            result.Add(Convert.ToByte(IsDevice));

            return result.ToArray();
        }

        public bool SetBytes(byte[] bytes)
        {
            Id = BitConverter.ToInt32(new byte[] { bytes[3], bytes[2], bytes[1], bytes[0] }, 0);

            Volume = Convert.ToInt16(bytes[4]);
            IsMuted = Convert.ToBoolean(bytes[5]);
            IsDevice = false;

            return true;
        }
        #endregion
    }
}