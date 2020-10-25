using System;
using System.IO;
using System.Text;

namespace MaxMix.Services.Communication
{
    public enum Command
    {
        ERROR = -1,
        NONE = 0,
        TEST = 1,
        OK,
        SETTINGS,
        SESSION_INFO,
        CURRENT_SESSION,
        ALTERNATE_SESSION,
        PREVIOUS_SESSION,
        NEXT_SESSION,
        VOLUME_CURR_CHANGE,
        VOLUME_ALT_CHANGE,
        VOLUME_PREV_CHANGE,
        VOLUME_NEXT_CHANGE,
        MODE_STATES,
        DEBUG
    }

    public enum SessionIndex
    {
        INDEX_CURRENT,
        INDEX_ALTERNATE,
        INDEX_PREVIOUS,
        INDEX_NEXT,
        INDEX_MAX
    };

    public enum DisplayMode
    {
        MODE_SPLASH,
        MODE_OUTPUT,
        MODE_INPUT,
        MODE_APPLICATION,
        MODE_GAME,
        MODE_MAX
    };

    public enum SplashState
    {
        STATE_LOGO,
        STATE_INFO,
        STATE_MAX
    };

    public enum SessionState
    {
        STATE_NAVIGATE,
        STATE_EDIT,
        STATE_MAX
    };

    public enum GameState
    {
        STATE_SELECT_A,
        STATE_SELECT_B,
        STATE_GAME_EDIT,
        STATE_MAX
    };

    internal static class MessageUtils
    {
        public static unsafe bool UnsafeEquals<T>(this T data, T other, uint offset = 0, uint count = uint.MaxValue) where T : unmanaged, IMessage
        {
            count = Math.Min(count, (uint)(sizeof(T) - offset));
            bool equal = true;
            byte* ptr1 = (byte*)&data;
            byte* ptr2 = (byte*)&other;
            for (uint i = offset; i < sizeof(T); i++)
                equal = equal && ptr1[i] == ptr2[i];
            return equal;
        }

        public static unsafe void UnsafeCopyTo<T>(this T data, MemoryStream stream, uint offset = 0, uint count = uint.MaxValue) where T : unmanaged, IMessage
        {
            count = Math.Min(count, (uint)(sizeof(T) - offset));
            byte* ptr = (byte*)&data;
            for (uint i = offset; i < count; i++)
                stream.WriteByte(ptr[i]);
        }

        public static unsafe void UnsafeCopyFrom<T>(this ref T data, byte[] bytes, uint offset = 0, uint count = uint.MaxValue) where T : unmanaged, IMessage
        {
            count = Math.Min(count, (uint)bytes.Length);
            count = Math.Min(count, (uint)(sizeof(T) - offset));
            fixed (T* dataAddr = &data)
            {
                byte* ptr = (byte*)dataAddr;
                for (uint i = offset; i < count; i++)
                    ptr[i] = bytes[i];
            }
        }

        public static unsafe void UnsafeClear<T>(this ref T data, uint offset = 0, uint count = uint.MaxValue) where T : unmanaged, IMessage
        {
            count = Math.Min(count, (uint)(sizeof(T) - offset));
            fixed (T* dataAddr = &data)
            {
                byte* ptr = (byte*)dataAddr;
                for (uint i = offset; i < count; i++)
                    ptr[i] = 0;
            }
        }

        public static unsafe string ToByteString<T>(this ref T data, uint offset = 0, uint count = uint.MaxValue) where T : unmanaged, IMessage
        {
            count = Math.Min(count, (uint)(sizeof(T) - offset));
            string msg = "";
            fixed (T* dataAddr = &data)
            {
                byte* ptr = (byte*)dataAddr;
                for (uint i = offset; i < count; i++)
                    msg += ptr[i].ToString("x1");
            }
            return msg;
        }

        public static byte Lower(this byte data)
        {
            return (byte)(data & 0x7F);
        }

        public static byte Lower(this byte data, byte value, byte maxVal = 127)
        {
            return (byte)(data & 0x80 | Math.Min(value, maxVal) & 0x7F);
        }

        public static bool Upper(this byte data)
        {
            return (data & 0x80) == 0x80;
        }

        public static byte Upper(this byte data, bool value)
        {
            return (byte)((value ? 0x80 : 0x00) | data & 0x7F);
        }
    }


    public unsafe interface IMessage
    {
        void GetBytes(MemoryStream stream);
        void SetBytes(byte[] bytes);
    }

    public unsafe struct SessionInfo : IMessage, IEquatable<SessionInfo>
    {
        fixed byte m_Data[5];

        public DisplayMode mode
        {
            get => (DisplayMode)m_Data[0];
            set => m_Data[0] = (byte)value;
        }

        public byte current
        {
            get => m_Data[1];
            set => m_Data[1] = value;
        }

        public byte output
        {
            get => m_Data[2];
            set => m_Data[2] = value;
        }

        public byte input
        {
            get => m_Data[3];
            set => m_Data[3] = value;
        }

        public byte application
        {
            get => m_Data[4];
            set => m_Data[4] = value;
        }

        public static SessionInfo Default()
        {
            return new SessionInfo();
        }

        public bool Equals(SessionInfo other)
        {
            return this.UnsafeEquals(other);
        }

        public unsafe void GetBytes(MemoryStream stream)
        {
            this.UnsafeCopyTo(stream);
        }

        public void SetBytes(byte[] bytes)
        {
            this.UnsafeCopyFrom(bytes);
        }

        public override string ToString()
        {
            return $"{mode}, {current}, {output}, {input}, {application} > {this.ToByteString()}";
        }
    }

    public unsafe struct VolumeData : IMessage, IEquatable<VolumeData>
    {
        fixed byte m_Data[2];

        public byte id
        {
            get => m_Data[0].Lower();
            set => m_Data[0] = m_Data[0].Lower(value);
        }

        public bool isDefault
        {
            get => m_Data[0].Upper();
            set => m_Data[0] = m_Data[0].Upper(value);
        }

        public byte volume
        {
            get => m_Data[1].Lower();
            set => m_Data[1] = m_Data[1].Lower(value);
        }

        public bool isMuted
        {
            get => m_Data[1].Upper();
            set => m_Data[1] = m_Data[1].Upper(value);
        }

        public static VolumeData Default()
        {
            return new VolumeData();
        }

        public bool Equals(VolumeData other)
        {
            return this.UnsafeEquals(other);
        }

        public unsafe void GetBytes(MemoryStream stream)
        {
            this.UnsafeCopyTo(stream);
        }

        public void SetBytes(byte[] bytes)
        {
            this.UnsafeCopyFrom(bytes);
        }

        public override string ToString()
        {
            return $"{id}, {isDefault}, {volume}, {isMuted} > {this.ToByteString()}";
        }
    }

    public unsafe struct SessionData : IMessage, IEquatable<SessionData>
    {
        fixed byte m_Data[30];

        public string name
        {
            get
            {
                fixed (byte* ptr = m_Data)
                    return new string((sbyte*)ptr, 0, 30);
            }
            set
            {
                this.UnsafeClear(0, 30);
                if (string.IsNullOrEmpty(value))
                    return;

                var bytes = Encoding.UTF8.GetBytes(value);
                this.UnsafeCopyFrom(bytes, 0, 30);
            }
        }

        public VolumeData data;

        public static SessionData Default()
        {
            return new SessionData();
        }

        public bool Equals(SessionData other)
        {
            return this.UnsafeEquals(other);
        }

        public unsafe void GetBytes(MemoryStream stream)
        {
            this.UnsafeCopyTo(stream);
        }

        public void SetBytes(byte[] bytes)
        {
            this.UnsafeCopyFrom(bytes);
        }

        public override string ToString()
        {
            return $"{name}, {data} > {this.ToByteString()}";
        }
    }

    public unsafe struct Color : IMessage, IEquatable<Color>
    {
        fixed byte m_Data[3];

        public byte r
        {
            get => m_Data[0];
            set => m_Data[0] = value;
        }

        public byte g
        {
            get => m_Data[1];
            set => m_Data[1] = value;
        }

        public byte b
        {
            get => m_Data[2];
            set => m_Data[2] = value;
        }

        public Color(byte _r, byte _g, byte _b)
        {
            r = _r;
            g = _g;
            b = _g;
        }

        public static Color Default()
        {
            return new Color();
        }

        public bool Equals(Color other)
        {
            return this.UnsafeEquals(other);
        }

        public unsafe void GetBytes(MemoryStream stream)
        {
            this.UnsafeCopyTo(stream);
        }

        public void SetBytes(byte[] bytes)
        {
            this.UnsafeCopyFrom(bytes);
        }

        public override string ToString()
        {
            return $"{r}, {g}, {b} > {this.ToByteString()}";
        }
    }

    public unsafe struct DeviceSettings : IMessage, IEquatable<DeviceSettings>
    {
        fixed byte m_Data[2];

        public byte sleepAfterSeconds
        {
            get => m_Data[0];
            set => m_Data[0] = value;
        }

        public byte accelerationPercentage
        {
            get => m_Data[1].Lower();
            set => m_Data[1] = m_Data[1].Lower(value, 100);
        }

        public bool continuousScroll
        {
            get => m_Data[1].Upper();
            set => m_Data[1] = m_Data[1].Upper(value);
        }

        public Color volumeMinColor;
        public Color volumeMaxColor;
        public Color mixChannelAColor;
        public Color mixChannelBColor;

        public static DeviceSettings Default()
        {
            return new DeviceSettings
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

        public bool Equals(DeviceSettings other)
        {
            return this.UnsafeEquals(other);
        }

        public unsafe void GetBytes(MemoryStream stream)
        {
            this.UnsafeCopyTo(stream);
        }

        public void SetBytes(byte[] bytes)
        {
            this.UnsafeCopyFrom(bytes);
        }

        public override string ToString()
        {
            return $"{sleepAfterSeconds}, {accelerationPercentage}, {continuousScroll}, {volumeMinColor}, {volumeMaxColor}, {mixChannelAColor}, {mixChannelBColor} > {this.ToByteString()}";
        }
    }

    public unsafe struct ModeStates : IMessage, IEquatable<ModeStates>
    {
        fixed byte m_Data[5];

        public SplashState splash
        {
            get => (SplashState)m_Data[0];
            set => m_Data[0] = (byte)value;
        }

        public SessionState output
        {
            get => (SessionState)m_Data[1];
            set => m_Data[1] = (byte)value;
        }

        public SessionState input
        {
            get => (SessionState)m_Data[2];
            set => m_Data[2] = (byte)value;
        }

        public SessionState application
        {
            get => (SessionState)m_Data[3];
            set => m_Data[3] = (byte)value;
        }

        public GameState game
        {
            get => (GameState)m_Data[4];
            set => m_Data[4] = (byte)value;
        }

        public static ModeStates Default()
        {
            return new ModeStates
            {
                splash = SplashState.STATE_LOGO,
                output = SessionState.STATE_EDIT,
                input = SessionState.STATE_EDIT,
                application = SessionState.STATE_NAVIGATE,
                game = GameState.STATE_SELECT_A
            };
        }

        public bool Equals(ModeStates other)
        {
            return this.UnsafeEquals(other);
        }

        public unsafe void GetBytes(MemoryStream stream)
        {
            this.UnsafeCopyTo(stream);
        }

        public void SetBytes(byte[] bytes)
        {
            this.UnsafeCopyFrom(bytes);
        }

        public override string ToString()
        {
            return $"{splash}, {output}, {input}, {application}, {game} > {this.ToByteString()}";
        }
    }
}
