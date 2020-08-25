using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication
{
    internal class MessageSettings : IMessage
    {
        #region Constructor
        public MessageSettings(bool displayNewSession, bool sleepWhenInactive, int sleepAfterSeconds, bool continuousScroll, uint accelerationPercentage, ushort doubleTapTime)
        {
            _displayNewSession = displayNewSession;
            _sleepWhenInactive = sleepWhenInactive;
            _sleepAfterSeconds = sleepAfterSeconds;
            _continuousScroll = continuousScroll;
            _accelerationPercentage = accelerationPercentage;
            _doubleTapTime = doubleTapTime;
        }
        #endregion

        #region Consts
        #endregion

        #region Fields
        public bool _displayNewSession;
        public bool _sleepWhenInactive;
        public int _sleepAfterSeconds;
        public bool _continuousScroll;
        public uint _accelerationPercentage;
        public ushort _doubleTapTime;
        #endregion

        #region Properties
        public bool DisplayNewSession { get => _displayNewSession; }
        public bool SleepWhenInactive { get => _sleepWhenInactive; }
        public int SleepAfterSeconds { get => _sleepAfterSeconds; }
        public bool ContinuousScroll { get => _continuousScroll; }
        public uint AccelerationPercentage { get => _accelerationPercentage; }
        public ushort DoubleTapTime { get => _doubleTapTime; }
        #endregion

        #region Private Methods
        #endregion

        #region Public Methods

        /*
        * ---------------------------------------------------
        * CHUNK                     TYPE        SIZE (BYTES)
        * ---------------------------------------------------
        * DISPLAYNEWSESSION         BYTE        1
        * SLEEPWHENINACTIVE         BYTE        1
        * SLEEPAFTERSECONDS         BYTE        1
        * CONTINUOUSSCROLL          BYTE        1
        * ACCELERATIONPERCENTAGE    BYTE        1
        * DOUBLETAPTIME             USHORT      2
        * ---------------------------------------------------
        */

        public byte[] GetBytes()
        {
            var result = new List<byte>();           

            result.Add(Convert.ToByte(DisplayNewSession));
            result.Add(Convert.ToByte(SleepWhenInactive));
            result.Add(Convert.ToByte(SleepAfterSeconds));
            result.Add(Convert.ToByte(ContinuousScroll));
            result.Add(Convert.ToByte(AccelerationPercentage));
            result.AddRange(BitConverter.GetBytes(DoubleTapTime));

            return result.ToArray();
        }

        public bool SetBytes(byte[] bytes)
        {
            throw new NotImplementedException("Should never be called");
        }
        #endregion
    }
}