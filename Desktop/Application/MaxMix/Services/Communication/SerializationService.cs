using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication
{
    // TODO: Delete, not in use.
    internal class SerializationService : ISerializationService
    {
        #region Constructor
        public SerializationService()
        {
            _typeMap = new Dictionary<int, Type>();
        }
        #endregion

        #region Consts
        private const byte _start = 250;
        private const byte _end = 251;
        private const int _maxLength = byte.MaxValue;
        #endregion

        #region Fields
        private Dictionary<int, Type> _typeMap;
        #endregion

        #region Properties
        public byte Delimiter
        {
            get => _end;
        }
        #endregion

        #region Public Methods
        /*
         * ------------ MESSAGE ----------------
         * CHUNK        TYPE        SIZE (BYTES)
         * START        BYTE        1
         * LENGTH       BYTE        1
         * COMMAND      BYTE        1
         * PAYLOAD      BYTE        *
         * END          BYTE        1
         * ---------------------------------------
         */

        public byte[] Serialize(IMessage message)
        {
            if (!_typeMap.ContainsValue(message.GetType()))
                throw new ArgumentException("Message type not registered");

            var result = new List<byte>();
            var payload = message.GetBytes();
            var command = _typeMap.First(o => o.Value == message.GetType()).Key;
            
            result.Add(_start);
            result.Add((byte)(payload.Length + 3));
            result.Add((byte)command);
            result.AddRange(payload);
            result.Add(_end);

            return result.ToArray();
        }

        public IMessage Deserialize(byte[] bytes)
        {
            // Verify message length
            byte length = bytes[1];
            if (bytes.Length != length)
            {
                return null;
            }

            // Extract message index
            byte command = bytes[2];
            if (!_typeMap.ContainsKey(command))
            {
                return null;
            }

            // Extract payload
            byte[] payload = bytes.Skip(3).Take(length - 4).ToArray();

            // Deserialize payload with message type instance
            Type type = _typeMap[command];
            IMessage message = Activator.CreateInstance(type) as IMessage;

            if (!message.SetBytes(payload))
            {
                return null;
            }

            return message;
        }

        public void RegisterType<T>(int id) where T : IMessage
        {
            if (_typeMap.ContainsKey(id))
                _typeMap[id] = typeof(T);
            else
                _typeMap.Add(id, typeof(T));
        }
        #endregion


        #region Event Dispatchers
        #endregion

    }
}
