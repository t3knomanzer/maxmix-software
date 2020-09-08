using Newtonsoft.Json.Bson;
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
        public MessageSettings(
            bool displayNewSession,
            bool sleepWhenInactive,
            int sleepAfterSeconds,
            bool continuousScroll,
            uint accelerationPercentage,
            ushort doubleTapTime,
            uint volumeMinColor,
            uint volumeMaxColor,
            uint mixChannelAColor,
            uint mixChannelBColor)
        {
            _displayNewSession = displayNewSession;
            _sleepWhenInactive = sleepWhenInactive;
            _sleepAfterSeconds = sleepAfterSeconds;
            _continuousScroll = continuousScroll;
            _accelerationPercentage = accelerationPercentage;
            _doubleTapTime = doubleTapTime;
            _volumeMinColor = volumeMinColor;
            _volumeMaxColor = volumeMaxColor;
            _mixChannelAColor = mixChannelAColor;
            _mixChannelBColor = mixChannelBColor;
        }
        #endregion

        #region Consts
        #endregion

        #region Fields
        private bool _displayNewSession;
        private bool _sleepWhenInactive;
        private int _sleepAfterSeconds;
        private bool _continuousScroll;
        private uint _accelerationPercentage;
        private ushort _doubleTapTime;
        private uint _volumeMinColor;
        private uint _volumeMaxColor;
        private uint _mixChannelAColor;
        private uint _mixChannelBColor;
        #endregion

        #region Properties
        public bool DisplayNewSession { get => _displayNewSession; }
        public bool SleepWhenInactive { get => _sleepWhenInactive; }
        public int SleepAfterSeconds { get => _sleepAfterSeconds; }
        public bool ContinuousScroll { get => _continuousScroll; }
        public uint AccelerationPercentage { get => _accelerationPercentage; }
        public ushort DoubleTapTime { get => _doubleTapTime; }
        public uint VolumeMinColor { get => _volumeMinColor; }
        public uint VolumeMaxColor { get => _volumeMaxColor; }
        public uint MixChannelAColor { get => _mixChannelAColor; }
        public uint MixChannelBColor { get => _mixChannelBColor; }
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
        * VOLUMEMINCOLOR            BYTE[]      3
        * VOLUMEMAXCOLOR            BYTE[]      3
        * MIXCHANNELACOLOR          BYTE[]      3
        * MIXCHANNELBCOLOR          BYTE[]      3
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
            result.AddRange(GetColorTriplet(VolumeMinColor));
            result.AddRange(GetColorTriplet(VolumeMaxColor));
            result.AddRange(GetColorTriplet(MixChannelAColor));
            result.AddRange(GetColorTriplet(MixChannelBColor));

            return result.ToArray();
        }

        public bool SetBytes(byte[] bytes)
        {
            throw new NotImplementedException("Should never be called");
        }
        #endregion

        private byte[] GetColorTriplet(UInt32 color)
        {
            // For colors we don't send the alpha component.
            byte[] rgba = BitConverter.GetBytes(color);
            Array.Resize(ref rgba, 3); // Drop the alpha channel
            return rgba;
        }

    }
}