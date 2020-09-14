using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;

namespace MaxMixTest
{
    class Program
    {
        // Device state data
        static Settings _settings = Settings.Default();
        static SessionInfo _sessionInfo = SessionInfo.Default();
        static Session[] _session = new[] { Session.Default(), Session.Default(), Session.Default() };
        static Screen _screen = Screen.Default();


        static SerialPort _serialPort;

        static bool _allClear = false;
        static ulong _readBytes = 0;
        static ulong _readCount = 0;
        static ulong _writeCount = 0;
        static ulong _writeBytes = 0;
        static ulong _errorCount = 0;


        static Random _rand = new Random(Guid.NewGuid().GetHashCode());

        static DateTime _lastDebug = DateTime.Now;
        static TimeSpan _debugDelay = new TimeSpan(0, 0, 0, 1);
        static DateTime _lastPrint = DateTime.Now;
        static TimeSpan _printDelay = new TimeSpan(0, 0, 30);

        static readonly string[] _sizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
        public static string ToSize(ulong bytes)
        {
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return $"{number:n1}{_sizeSuffixes[counter]}";
        }


        static void Main(string[] args)
        {
            _serialPort = new SerialPort("COM5", 115200);
            _serialPort.ReadTimeout = 10;
            _serialPort.WriteTimeout = 10;
            _serialPort.Open();

            Stopwatch timer = Stopwatch.StartNew();
            Write(Command.TEST);
            while (true)
            {
                Read();

                if (_allClear)
                {
                    var now = DateTime.Now;
                    if (now - _lastDebug > _debugDelay)
                    {
                        _lastDebug = now;
                        Write(Command.DEBUG);
                    }
                    else if (now - _lastPrint > _printDelay)
                    {
                        _lastPrint = now;
                        double readBps = _readBytes / timer.Elapsed.TotalSeconds;
                        double writeBps = _writeBytes / timer.Elapsed.TotalSeconds;
                        Console.WriteLine($"Read {ToSize(_readBytes)} @ {readBps:n1}B/s ({_readCount}), Write {ToSize(_writeBytes)} @ {writeBps:n1}B/s ({_writeCount}), Errors {_errorCount}");
                    }
                    else
                    {
                        // Do Test Fuzzing
                        Write((Command)_rand.Next((int)Command.SETTINGS, (int)Command.DEBUG));
                    }
                }
            }
        }

        static void RawReadDump()
        {
            if (_serialPort.BytesToRead > 0)
            {
                int command = _serialPort.ReadByte();
                Console.WriteLine($"{command}");
            }
        }

        static void ReadMessage<T>(T oldMessage) where T : IMessage, IEquatable<T>, new()
        {
            var message = new T();
            var buffer = message.GetBytes();

            int length = 0;
            while (length < buffer.Length)
                length += _serialPort.Read(buffer, length, buffer.Length - length);

            _readBytes += (uint)length;

            if (length != buffer.Length)
                _errorCount++;

            message.SetBytes(buffer);
            if (!oldMessage.Equals(message))
                _errorCount++;
        }

        static void Read()
        {
            if (_serialPort.BytesToRead > 0)
            {
                int command = _serialPort.ReadByte();
                _readCount++;
                _readBytes++;

                switch ((Command)command)
                {
                    case Command.TEST:
                        {
                            var firmware = _serialPort.ReadLine();
                            Console.WriteLine($"Firmware: {firmware}");
                        }
                        break;
                    case Command.OK:
                        _allClear = true;
                        break;
                    case Command.SETTINGS:
                        ReadMessage(_settings);
                        break;
                    case Command.SESSION_INFO:
                        ReadMessage(_sessionInfo);
                        break;
                    case Command.CURRENT:
                        ReadMessage(_session[1]);
                        break;
                    case Command.PREVIOUS:
                        ReadMessage(_session[0]);
                        break;
                    case Command.NEXT:
                        ReadMessage(_session[2]);
                        break;
                    case Command.VOLUME:
                        ReadMessage(_session[1].values);
                        break;
                    case Command.SCREEN:
                        ReadMessage(_screen);
                        break;
                    case Command.DEBUG:
                        _errorCount++; // Do Nothing, but this is an error as we should never get this msg
                        break;
                }
            }
        }

        static void WriteMessage(Command command, IMessage message = null)
        {
            byte[] bytes;
            if (message != null)
            {
                byte[] payload = message.GetBytes();
                bytes = new byte[payload.Length + 1];
                Array.Copy(payload, 0, bytes, 1, payload.Length);
            }
            else
            {
                bytes = new byte[1];
            }

            bytes[0] = (byte)command;
            _writeBytes += (uint)bytes.Length;
            _serialPort.Write(bytes, 0, bytes.Length);
        }

        static void WriteFuzzMessage<T>(Command command, ref T message) where T : IMessage
        {
            byte[] payload = message.GetBytes();
            _rand.NextBytes(payload);
            if (message is Session)
                payload[29] = 0; // Expected to have null terminated strings
            message.SetBytes(payload);

            byte[] bytes = new byte[payload.Length + 1];
            Array.Copy(payload, 0, bytes, 1, payload.Length);

            bytes[0] = (byte)command;
            _writeBytes += (uint)bytes.Length;
            _serialPort.Write(bytes, 0, bytes.Length);
        }

        static void Write(Command command)
        {
            _allClear = false;
            _writeCount++;
            switch (command)
            {
                case Command.TEST:
                    WriteMessage(command);
                    break;
                case Command.OK:
                    WriteMessage(command);
                    break;
                case Command.SETTINGS:
                    WriteFuzzMessage(command, ref _settings);
                    break;
                case Command.SESSION_INFO:
                    WriteFuzzMessage(command, ref _sessionInfo);
                    break;
                case Command.CURRENT:
                    WriteFuzzMessage(command, ref _session[1]);
                    break;
                case Command.PREVIOUS:
                    WriteFuzzMessage(command, ref _session[0]);
                    break;
                case Command.NEXT:
                    WriteFuzzMessage(command, ref _session[2]);
                    break;
                case Command.VOLUME:
                    WriteFuzzMessage(command, ref _session[1].values);
                    break;
                case Command.SCREEN:
                    WriteFuzzMessage(command, ref _screen);
                    break;
                case Command.DEBUG:
                    WriteMessage(command);
                    break;
            }
        }
    }
}
