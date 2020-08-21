using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication
{
    internal class MessageUpdateVolumeSession : IMessage
    {
        #region Constructor
        public MessageUpdateVolumeSession() { }

        public MessageUpdateVolumeSession(int id, int volume, bool isMuted)
        {
            _id = id;
            _volume = volume;
            _isMuted = isMuted;
        }
        #endregion

        #region Consts
        #endregion

        #region Fields
        private int _id;
        private int _volume;
        private bool _isMuted;
        #endregion

        #region Properties
        public int Id { get => _id; }
        public int Volume { get => _volume; }
        public bool IsMuted { get => _isMuted; }
        #endregion

        #region Private Methods
        #endregion

        #region Public Methods

        /*
        * ---------------------------------------
        * CHUNK        TYPE        SIZE (BYTES)
        * ---------------------------------------
        * ID           INT32       4
        * VOLUME       BYTE        1
        * ISMUTED      BYTE        1
        * ---------------------------------------
        */

        public byte[] GetBytes()
        {
            var result = new List<byte>();           

            result.AddRange(BitConverter.GetBytes(Id));
            result.Add(Convert.ToByte(Volume));
            result.Add(Convert.ToByte(IsMuted));

            return result.ToArray();
        }

        public bool SetBytes(byte[] bytes)
        {
            var idBytes = bytes.Take(4).Reverse().ToArray();
            _id = BitConverter.ToInt32(idBytes, 0);
            _volume = Convert.ToInt16(bytes[4]);
            _isMuted = Convert.ToBoolean(bytes[5]);

            return true;
        }
        #endregion
    }
}