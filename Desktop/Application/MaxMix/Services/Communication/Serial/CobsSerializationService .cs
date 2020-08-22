using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication
{
    /// <summary>
    /// Implements the COBS serialization protocol for efficient communication
    /// over serial connection.
    /// 
    /// It uses the following packet structyure:
    /// 
    /// ------------ MESSAGE ----------------
    /// CHUNK        TYPE        SIZE (BYTES)
    /// ---------------------------------------
    /// START        BYTE        1
    /// COMMAND      BYTE        1
    /// PAYLOAD      BYTE        ///
    /// LENGTH       BYTE        1
    /// END          BYTE        1
    /// ---------------------------------------
    ///
    /// </summary>
    internal class CobsSerializationService : ISerializationService
    {
        #region Constructor
        public CobsSerializationService()
        {
            _registeredTypes = new Dictionary<int, Type>();
        }
        #endregion

        #region Consts
        private const byte _delimiter = 0;
        #endregion

        #region Fields
        private Dictionary<int, Type> _registeredTypes;
        #endregion

        #region Properties
        public byte Delimiter { get => _delimiter; }
        #endregion

        #region Private Methods
        private IList<byte> Encode(IEnumerable<byte> Input, byte delimiter)
        {
            var result = new List<byte>();
            int distanceIndex = 0;
            byte distance = 1;  // Distance to next zero

            foreach (var i in Input)
            {
                // If we encounter a zero (the frame delimiter)
                if (i == delimiter)
                {
                    // Write the value of the distance to the next zero back in output where we last saw a zero
                    result.Insert(distanceIndex, distance);

                    // Set the distance index to the latest index plus one
                    distanceIndex = (byte)result.Count;

                    // Reset the value which indicates the distance to the next zero (the frame delimiter)
                    distance = 1;
                }
                else
                {
                    // Otherwise simply add the next value to the result
                    result.Add(i);

                    // Increment the distance to the next zero
                    distance++;

                    // Check for maximum distance value
                    if (distance == 0xFF)
                    {
                        // Set the distance variable to its maximum value
                        result.Insert(distanceIndex, distance);

                        // Set the distance index to the latest index plus one
                        distanceIndex = (byte)result.Count;

                        // Reset the value which indicates the distance to the next zero (the frame delimiter)
                        distance = 1;
                    }
                }
            }

            // If the packet hasn't reached the maximum size
            if (result.Count != 255 && result.Count > 0)
            {
                // Add the last distance variable
                result.Insert(distanceIndex, distance);
            }


            // Return with the result
            return result;
        }

        private IList<byte> Decode(IEnumerable<byte> Input, byte delimiter)
        {
            var input = Input.ToArray();
            var result = new List<byte>();
            int distanceIndex = 0;
            byte distance = 1;  // Distance to next zero

            // Continue decoding which the next index is valid
            while (distanceIndex < input.Length)
            {
                // Get the next distance value
                distance = input[distanceIndex];

                // Ensure the input is formatted correctly (distanceIndex + distance)
                if (input.Length < distanceIndex + distance || distance < 1)
                    return null;

                // Add the range of byte up to the next zero
                if (distance > 1)
                {
                    for (byte i = 1; i < distance; i++)
                        result.Add(input[distanceIndex + i]);
                }

                // Determine the next distance index (doing this here assists the below if)
                distanceIndex += distance;

                // Add the original zero back
                if (distance < 0xFF && distanceIndex < input.Length)
                    result.Add(delimiter);
            }

            return result;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Encodes the message using the COBS algorithm using the packet structure
        /// defined by this class.
        /// </summary>
        /// <param name="message">The message to encode.</param>
        /// <returns>The message encoded as a byte array.</returns>
        public byte[] Serialize(IMessage message, byte revision)
        {
            if (!_registeredTypes.ContainsValue(message.GetType()))
                throw new ArgumentException("Message type not registered");

            var packet = new List<byte>();

            packet.Add(revision);

            var command = (byte)_registeredTypes.First(o => o.Value == message.GetType()).Key;
            packet.Add(command);

            var payload = message.GetBytes();
            packet.AddRange(payload);

            var length = (byte)(packet.Count() + 1);
            packet.Add(length);

            // Max length is 255. 1 byte reserved for the first 0 index.
            // This is so we can encode the packet length into a single byte.
            if (length >= 254)
                throw new ArgumentOutOfRangeException("Message too long.");

            var result = Encode(packet, Delimiter);
            result.Add(Delimiter);

            return result.ToArray();
        }

        /// <summary>
        /// Decodes the COBS encoded package into one of the registered
        /// IMessage messages.
        /// </summary>
        /// <param name="bytes">A byte array containing the COBS encoded message and
        /// following the packet structure established by this class.</param>
        /// <returns>The extracted IMessage.</returns>
        public IMessage Deserialize(byte[] bytes)
        {
            if (bytes.Count() > 255)
                throw new ArgumentException("Message too long.");

            // Drop last 0 (packet delimiter)
            var decoded = Decode(bytes.Take(bytes.Length - 1), Delimiter);

            if(decoded == null || decoded.Count == 0)
                throw new ArgumentException("Error decoding message.");

            // Verify message length (last byte)
            byte length = decoded.Last();
            if (decoded.Count != length)
                throw new ArgumentException("Message length missmatch.");

            // Extract message version
            byte revision = decoded[0];

            // Extract message index
            byte command = decoded[1];
            if (!_registeredTypes.ContainsKey(command))
                throw new ArgumentException("Message type not registered.");

            // Extract payload (everything except first and last bytes)
            byte[] payload = decoded.Skip(2).Take(length - 3).ToArray();

            // Deserialize payload with message type instance
            Type type = _registeredTypes[command];
            IMessage message;
            if (type == typeof(MessageAcknowledgment))
            {
                message = Activator.CreateInstance(type, revision) as MessageAcknowledgment;
            }
            else {
                message = Activator.CreateInstance(type) as IMessage;
            }

            if (!message.SetBytes(payload))
                throw new ArgumentException("Incorrect payload for message type.");

            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        public void RegisterType<T>(int id) where T : IMessage
        {
            if (_registeredTypes.ContainsKey(id))
                _registeredTypes[id] = typeof(T);
            else
                _registeredTypes.Add(id, typeof(T));
        }
        #endregion

        #region Event Dispatchers
        #endregion

    }
}
