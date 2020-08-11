using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication
{
    internal class MessageDisplayData : IMessage
    {
        #region Constructor
        public MessageDisplayData(byte[] pixels)
        {
            _pixels = pixels;
        }
        #endregion

        #region Consts
        #endregion

        #region Fields
        private byte[] _pixels;
        #endregion

        #region Properties
        #endregion

        #region Public Methods
        public byte[] GetBytes()
        {
            return _pixels;
        }

        public bool SetBytes(byte[] bytes)
        {
            throw new NotImplementedException("Should never be called");
        }
        #endregion
    }
}