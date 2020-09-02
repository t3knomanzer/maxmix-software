using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication
{
    internal class MessageRemoveSession : IMessage
    {
        #region Constructor
        public MessageRemoveSession(int id, bool isDevice)
        {
            _id = id;
            _isDevice = isDevice;
        }
        #endregion

        #region Consts
        #endregion

        #region Fields
        private int _id;
        private bool _isDevice;
        #endregion

        #region Properties
        public int Id { get => _id; }
        public bool IsDevice { get => _isDevice; }
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
            throw new NotImplementedException("Should never be called");
        }
        #endregion
    }
}