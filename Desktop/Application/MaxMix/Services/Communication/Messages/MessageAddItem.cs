using ControlzEx.Standard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication.Messages
{
    internal class MessageAddItem : IMessage
    {
        #region Constructor
        public MessageAddItem(int id, string name, int volume, bool isMuted, bool isDevice, int deviceFlow = 0)
        {
            Id = id;
            Name = name;
            Volume = volume;
            IsMuted = isMuted;
            IsDevice = isDevice;
            DeviceFlow = deviceFlow;

            EncodeName();
        }
        #endregion

        #region Consts
        private readonly int _nameLength = 24;
        #endregion
        
        #region Properties
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string EncodedName { get; private set; }
        public int Volume { get; private set; }
        public bool IsMuted { get; private set; }
        public bool IsDevice { get; private set; }
        public int DeviceFlow { get; private set; }
        #endregion

        #region Private Methods
        private void EncodeName()
        {
            EncodedName = Name.ToUpper();
            if (EncodedName.Length >= _nameLength)
            {
                EncodedName = EncodedName.Substring(0, _nameLength - 1) + "\0";
            }
            else 
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
        * NAME         STRING      24
        * VOLUME       BYTE        1
        * ISMUTED      BYTE        1
        * ISDEVICE     BYTE        1
        * DEVICEFLOW   BYTE        1
        * ---------------------------------------
        *                          32
        */

        public byte[] GetBytes()
        {
            var result = new List<byte>();

            result.AddRange(BitConverter.GetBytes(Id));
            result.AddRange(Encoding.ASCII.GetBytes(EncodedName));
            result.Add(Convert.ToByte(Volume));
            result.Add(Convert.ToByte(IsMuted));
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