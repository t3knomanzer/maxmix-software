using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication
{
    internal class MessageAddSession : IMessage
    {
        #region Constructor
        public MessageAddSession(int id, string name, int volume, bool isMuted, bool isDevice)
        {
            _id = id;
            _name = name;
            _volume = volume;
            _isMuted = isMuted;
            _isDevice = isDevice;

            EncodeName();
        }
        #endregion

        #region Consts
        private readonly int _nameLength = 36;
        #endregion
        
        #region Fields
        private int _id;
        private string _name;
        private string _encodedName;
        private int _volume;
        private bool _isMuted;
        private bool _isDevice;
        #endregion

        #region Properties
        public int Id { get => _id; }
        public string Name { get => _name; }
        public string EncodedName { get => _encodedName; }
        public int Volume { get => _volume; }
        public bool IsMuted { get => _isMuted; }
        public bool IsDevice { get => _isDevice; }
        #endregion

        #region Private Methods
        private void EncodeName()
        {
            _encodedName = _name.ToUpper();

            if (_encodedName.Length > _nameLength)
                _encodedName = _encodedName.Substring(0, _nameLength);
            else if(_encodedName.Length < _nameLength)
                while(_encodedName.Length < _nameLength)
                {
                    _encodedName += "\0";
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
            return true;
        }
        #endregion
    }
}