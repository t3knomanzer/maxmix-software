using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication
{
    internal class MessageSetDefaultEndpoint : IMessage
    {
        #region Constructor
        public MessageSetDefaultEndpoint() { }
        public MessageSetDefaultEndpoint(int id)
        {
            _id = id;
        }
        #endregion

        #region Consts
        #endregion

        #region Fields
        private int _id;
        #endregion

        #region Properties
        public int Id { get => _id; }
        #endregion

        #region Private Methods
        #endregion

        #region Public Methods

        /*
        * ---------------------------------------
        * CHUNK        TYPE        SIZE (BYTES)
        * ---------------------------------------
        * ID           INT32       4
        * ---------------------------------------
        *                          4
        */

        public byte[] GetBytes()
        {
            var result = new List<byte>();           
            result.AddRange(BitConverter.GetBytes(Id));
            return result.ToArray();
        }

        public bool SetBytes(byte[] bytes)
        {
            var idBytes = bytes.Take(4).Reverse().ToArray();
            _id = BitConverter.ToInt32(idBytes, 0);
            return true;
        }
        #endregion
    }
}