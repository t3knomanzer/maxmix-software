using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication.Messages
{
    internal class MessageAddSession : IMessage
    {
        #region Constructor
        public MessageAddSession(int id, string name, int volume, bool isMuted, bool isDevice)
        {
            Id = id;
            Name = name;
            Volume = volume;
            IsMuted = isMuted;
            IsDevice = isDevice;

            EncodeName();
        }
        #endregion

        #region Consts
        private readonly int _nameLength = 36;
        #endregion
        
        #region Properties
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string EncodedName { get; private set; }
        public int Volume { get; private set; }
        public bool IsMuted { get; private set; }
        public bool IsDevice { get; private set; }
        #endregion

        #region Private Methods
        private void EncodeName()
        {
            EncodedName = Name.ToUpper();

            if (EncodedName.Length > _nameLength)
            {
                EncodedName = EncodedName.Substring(0, _nameLength);
            }
            else if (EncodedName.Length < _nameLength)
            {
                while (EncodedName.Length < _nameLength)
                {
                    EncodedName += "\0";
                }
            }
        }
        #endregion

        #region Public Methods

        /*
        * ---------------------------------------
        * CHUNK        TYPE        SIZE (BYTES)
        * ---------------------------------------
        * ID           INT32       4
        * NAME         STRING      36
        * VOLUME       BYTE        1
        * ISMUTED      BYTE        1
        * ISDEVICE     BYTE        1
        * ---------------------------------------
        *                          43
        */

        public byte[] GetBytes()
        {
            var result = new List<byte>();

            result.AddRange(BitConverter.GetBytes(Id));
            result.AddRange(Encoding.ASCII.GetBytes(EncodedName));
            result.Add(Convert.ToByte(Volume));
            result.Add(Convert.ToByte(IsMuted));
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