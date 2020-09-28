using MaxMix.Services.NewCommunication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;

namespace MaxMixTest
{
    class Program
    {
        // Device state data
        static DeviceSettings _settings = DeviceSettings.Default();
        static SessionInfo _sessionInfo = SessionInfo.Default();
        static SessionData[] _session = new[] { SessionData.Default(), SessionData.Default(), SessionData.Default() };

        static SerialPort _serialPort;
        static readonly byte[] m_ReadBuffer = new byte[128];

        static bool _allClear = false;
        static ulong _readBytes = 0;
        static ulong _readCount = 0;
        static ulong _writeCount = 0;
        static ulong _writeBytes = 0;
        static ulong _errorCount = 0;


        static Random _rand = new Random(Guid.NewGuid().GetHashCode());
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
            var stream = new MemoryStream();
            _settings.GetBytes(stream);

            //_serialPort = new SerialPort("COM5", 115200);
            //_serialPort.ReadTimeout = 20;
            //_serialPort.WriteTimeout = 20;
            //_serialPort.Open();

            //Stopwatch timer = Stopwatch.StartNew();
            //Write(Command.TEST);
            //while (true)
            //{
            //    Read();

            //    if (_allClear)
            //    {
            //        var now = DateTime.Now;
            //        if (now - _lastPrint > _printDelay)
            //        {
            //            _lastPrint = now;
            //            double readBps = _readBytes / timer.Elapsed.TotalSeconds;
            //            double writeBps = _writeBytes / timer.Elapsed.TotalSeconds;
            //            double readPs = _readCount / timer.Elapsed.TotalSeconds;
            //            double writePs = _writeCount / timer.Elapsed.TotalSeconds;
            //            Console.WriteLine($"Read {ToSize(_readBytes)} @ {readBps:n1}B/s ({_readCount} @ {readPs:n1}/s), Write {ToSize(_writeBytes)} @ {writeBps:n1}B/s ({_writeCount} @ {writePs:n1}/s), Errors {_errorCount}");
            //        }

            //        // Do Test Fuzzing
            //        Write((Command)_rand.Next((int)Command.OK, (int)Command.DEBUG + 1));
            //    }
            //}
        }

        static void RawReadDump()
        {
            if (_serialPort.BytesToRead > 0)
            {
                int command = _serialPort.ReadByte();
                Console.WriteLine($"{command}");
            }
        }

        static unsafe void ReadMessage<T>(T message) where T : unmanaged, IMessage
        {
            int length = 0;
            try
            {
                while (length < sizeof(T))
                    length += _serialPort.Read(m_ReadBuffer, length, sizeof(T) - length);

                if (length != sizeof(T))
                    throw new ArgumentException($"Message Length: {length}. Expected Length: {sizeof(T)}.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Exception]: {typeof(T).Name}");
                Console.WriteLine(e);
                _errorCount++;
                return;
            }

            message.SetBytes(m_ReadBuffer);
            _readBytes += (uint)length;

            // TODO: Raise command & message read event
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
                    case Command.CURRENT_SESSION:
                        ReadMessage(_session[1]);
                        break;
                    case Command.PREVIOUS_SESSION:
                        ReadMessage(_session[0]);
                        break;
                    case Command.NEXT_SESSION:
                        ReadMessage(_session[2]);
                        break;
                    case Command.VOLUME_CHANGE:
                        ReadMessage(_session[1].data);
                        break;
                    case Command.DEBUG:
                        _errorCount++; // Do Nothing, but this is an error as we should never get this msg
                        break;
                }
            }
        }

        //static void WriteMessage(Command command, IMessage message = null)
        //{
        //    byte[] bytes;
        //    if (message != null)
        //    {
        //        byte[] payload = message.GetBytes();
        //        bytes = new byte[payload.Length + 1];
        //        Array.Copy(payload, 0, bytes, 1, payload.Length);
        //    }
        //    else
        //    {
        //        bytes = new byte[1];
        //    }

        //    bytes[0] = (byte)command;
        //    _writeBytes += (uint)bytes.Length;
        //    _serialPort.Write(bytes, 0, bytes.Length);
        //}

        //static void WriteFuzzMessage<T>(Command command, ref T message) where T : IMessage
        //{
        //    byte[] payload = message.GetBytes();
        //    _rand.NextBytes(payload);
        //    if (message is SessionData)
        //        payload[29] = 0; // Expected to have null terminated strings
        //    message.SetBytes(payload);

        //    byte[] bytes = new byte[payload.Length + 1];
        //    Array.Copy(payload, 0, bytes, 1, payload.Length);

        //    bytes[0] = (byte)command;
        //    _writeBytes += (uint)bytes.Length;
        //    _serialPort.Write(bytes, 0, bytes.Length);
        //}

        //static void Write(Command command)
        //{
        //    _allClear = false;
        //    _writeCount++;
        //    switch (command)
        //    {
        //        case Command.TEST:
        //            WriteMessage(command);
        //            break;
        //        case Command.OK:
        //            WriteMessage(command);
        //            break;
        //        case Command.SETTINGS:
        //            WriteFuzzMessage(command, ref _settings);
        //            break;
        //        case Command.SESSION_INFO:
        //            WriteFuzzMessage(command, ref _sessionInfo);
        //            break;
        //        case Command.CURRENT:
        //            WriteFuzzMessage(command, ref _session[1]);
        //            break;
        //        case Command.PREVIOUS:
        //            WriteFuzzMessage(command, ref _session[0]);
        //            break;
        //        case Command.NEXT:
        //            WriteFuzzMessage(command, ref _session[2]);
        //            break;
        //        case Command.VOLUME:
        //            WriteFuzzMessage(command, ref _session[1].data);
        //            break;
        //        case Command.SCREEN:
        //            WriteFuzzMessage(command, ref _screen);
        //            break;
        //        case Command.DEBUG:
        //            WriteMessage(command);
        //            break;
        //    }
        //}
    }
}
