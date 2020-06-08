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
        public MessageRemoveSession(int id)
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
        */

        public byte[] GetBytes()
        {
            return BitConverter.GetBytes(Id);
        }

        public bool SetBytes(byte[] bytes)
        {
            throw new NotImplementedException("Should never be called");
        }
        #endregion
    }
}