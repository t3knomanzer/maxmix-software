using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication.Messages
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
            DisplayNewSession = displayNewSession;
            SleepWhenInactive = sleepWhenInactive;
            SleepAfterSeconds = sleepAfterSeconds;
            ContinuousScroll = continuousScroll;
            AccelerationPercentage = accelerationPercentage;
            DoubleTapTime = doubleTapTime;
            VolumeMinColor = volumeMinColor;
            VolumeMaxColor = volumeMaxColor;
            MixChannelAColor = mixChannelAColor;
            MixChannelBColor = mixChannelBColor;
        }
        #endregion

        #region Properties
        public bool DisplayNewSession { get; private set; }
        public bool SleepWhenInactive { get; private set; }
        public bool ContinuousScroll { get; private set; }
        public int SleepAfterSeconds { get; private set; }
        public uint AccelerationPercentage { get; private set; }
        public ushort DoubleTapTime { get; private set; }
        public uint VolumeMinColor { get; private set; }
        public uint VolumeMaxColor { get; private set; }
        public uint MixChannelAColor { get; private set; }
        public uint MixChannelBColor { get; private set; }
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
            throw new NotImplementedException();
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