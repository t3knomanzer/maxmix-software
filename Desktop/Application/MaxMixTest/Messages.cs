using System;
using System.Diagnostics;
using System.Linq;

namespace MaxMixTest
{
    public enum Command
    {
        TEST = 1,
        OK,
        SETTINGS,
        SESSION_INFO,
        CURRENT,
        PREVIOUS,
        NEXT,
        VOLUME,
        SCREEN,
        DEBUG
    }

    static class MessageUtils
    {
        public static bool AreEqual(this IMessage message, IMessage other)
        {
            var bytes = message.GetBytes();
            var otherBytes = other.GetBytes();
            return Enumerable.SequenceEqual(bytes, otherBytes);
        }

        public static void Extract(this byte data, ref byte value1, ref bool value2)
        {
            value1 = (byte)(data & 0x7F);
            value2 = (data & 0x80) == 0x80;
        }

        public static void Pack(ref this byte data, byte value1, bool value2)
        {
            data = (byte)(value1 | (value2 ? 0x80 : 0x00));
        }
    }


    public interface IMessage
    {
        byte[] GetBytes();
        void SetBytes(byte[] bytes);
    }

    public struct SessionInfo : IMessage, IEquatable<SessionInfo>
    {
        public byte current;
        public byte count;

        public static SessionInfo Default()
        {
            return new SessionInfo
            {
                count = 0,
                current = 0
            };
        }

        public bool Equals(SessionInfo other)
        {
            return this.AreEqual(other);
        }

        public byte[] GetBytes()
        {
            byte[] buffer = new byte[2];
            buffer[0] = current;
            buffer[1] = count;
            return buffer;
        }

        public void SetBytes(byte[] bytes)
        {
            Debug.Assert(bytes.Length == 2);
            current = bytes[0];
            count = bytes[1];
        }
    }

    public struct Volume : IMessage, IEquatable<Volume>
    {
        internal byte _id;
        public byte id
        {
            get => _id;
            set => _id = Math.Min(value, (byte)127);
        }

        public bool isDefault;

        internal byte _volume;
        public byte volume
        {
            get => _volume;
            set => _volume = Math.Min(value, (byte)100);
        }

        public bool isMuted;

        public static Volume Default()
        {
            return new Volume
            {
                _id = 0,
                isDefault = false,
                _volume = 0,
                isMuted = false
            };
        }

        public byte[] GetBytes()
        {
            byte[] buffer = new byte[2];
            buffer[0].Pack(_id, isDefault);
            buffer[1].Pack(_volume, isMuted);
            return buffer;
        }

        public void SetBytes(byte[] bytes)
        {
            Debug.Assert(bytes.Length == 2);
            _id = (byte)(bytes[0] & 0x7F);
            isDefault = (bytes[0] & 0x80) == 0x80;
            _volume = (byte)(bytes[1] & 0x7F);
            isMuted = (bytes[1] & 0x80) == 0x80;
        }

        public bool Equals(Volume other)
        {
            return this.AreEqual(other);
        }
    }

    public struct Session : IMessage, IEquatable<Session>
    {
        internal char[] _name;
        public string name
        {
            get => new string(_name);
            set
            {
                _name = value.ToCharArray(0, 30);
                _name[29] = (char)0;
            }
        }

        public Volume values;

        public static Session Default()
        {
            return new Session
            {
                _name = new char[30],
                values = Volume.Default()
            };
        }

        public byte[] GetBytes()
        {
            if (_name == null)
                _name = new char[30];

            byte[] buffer = new byte[32];
            for (int i = 0; i < _name.Length; i++)
                buffer[i] = (byte)_name[i];
            buffer[30].Pack(values._id, values.isDefault);
            buffer[31].Pack(values._volume, values.isMuted);
            return buffer;
        }

        public void SetBytes(byte[] bytes)
        {
            Debug.Assert(bytes.Length == 32);
            if (_name == null)
                _name = new char[30];

            for (int i = 0; i < 30; i++)
                _name[i] = (char)bytes[i];

            bytes[30].Extract(ref values._id, ref values.isDefault);
            bytes[31].Extract(ref values._volume, ref values.isMuted);
        }

        public bool Equals(Session other)
        {
            return this.AreEqual(other);
        }
    }

    public struct Screen : IMessage, IEquatable<Screen>
    {
        public byte id;

        public static Screen Default()
        {
            return new Screen
            {
                id = 0
            };
        }

        public byte[] GetBytes()
        {
            return new[] { id };
        }

        public void SetBytes(byte[] bytes)
        {
            Debug.Assert(bytes.Length == 1);
            id = bytes[0];
        }

        public bool Equals(Screen other)
        {
            return this.AreEqual(other);
        }
    }

    public struct Color : IEquatable<Color>
    {
        public byte r;
        public byte g;
        public byte b;

        public static Color Default()
        {
            return new Color { r = 0, g = 0, b = 0, };
        }

        public Color(byte _r, byte _g, byte _b)
        {
            r = _r;
            g = _g;
            b = _g;
        }

        public bool Equals(Color other)
        {
            return r == other.r && g == other.g && b == other.b;
        }
    }

    public struct Settings : IMessage, IEquatable<Settings>
    {
        public byte sleepAfterSeconds;

        internal byte _accelerationPercentage;
        public byte accelerationPercentage
        {
            get => _accelerationPercentage;
            set => _accelerationPercentage = Math.Min(value, (byte)100);
        }

        public bool continuousScroll;
        public Color volumeMinColor;
        public Color volumeMaxColor;
        public Color mixChannelAColor;
        public Color mixChannelBColor;

        public static Settings Default()
        {
            return new Settings
            {
                sleepAfterSeconds = 5,
                accelerationPercentage = 60,
                continuousScroll = true,
                volumeMinColor = new Color(0, 0, 255),
                volumeMaxColor = new Color(255, 0, 0),
                mixChannelAColor = new Color(0, 0, 255),
                mixChannelBColor = new Color(255, 0, 255)
            };
        }

        public byte[] GetBytes()
        {
            byte[] buffer = new byte[14];
            buffer[0] = sleepAfterSeconds;
            buffer[1].Pack(_accelerationPercentage, continuousScroll);
            buffer[2] = volumeMinColor.r;
            buffer[3] = volumeMinColor.g;
            buffer[4] = volumeMinColor.b;
            buffer[5] = volumeMaxColor.r;
            buffer[6] = volumeMaxColor.g;
            buffer[7] = volumeMaxColor.b;
            buffer[8] = mixChannelAColor.r;
            buffer[9] = mixChannelAColor.g;
            buffer[10] = mixChannelAColor.b;
            buffer[11] = mixChannelBColor.r;
            buffer[12] = mixChannelBColor.g;
            buffer[13] = mixChannelBColor.b;
            return buffer;
        }

        public void SetBytes(byte[] bytes)
        {
            Debug.Assert(bytes.Length == 14);
            sleepAfterSeconds = bytes[0];

            bytes[1].Extract(ref _accelerationPercentage, ref continuousScroll);

            volumeMinColor = new Color(bytes[2], bytes[3], bytes[4]);
            volumeMaxColor = new Color(bytes[5], bytes[6], bytes[7]);
            mixChannelAColor = new Color(bytes[8], bytes[9], bytes[10]);
            mixChannelBColor = new Color(bytes[11], bytes[12], bytes[13]);
        }

        public bool Equals(Settings other)
        {
            return this.AreEqual(other);
        }
    }
}
