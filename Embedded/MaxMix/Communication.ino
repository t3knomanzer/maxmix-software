//********************************************************
// PROJECT: MAXMIX
// AUTHOR: Ruben Henares
// EMAIL: rhenares0@gmail.com
//
// DECRIPTION:
//********************************************************

//********************************************************
// *** FUNCTIONS
//********************************************************
//---------------------------------------------------------
// Reads the data from the serial receive buffer.
//---------------------------------------------------------
bool ReceiveData(uint8_t* buffer, uint8_t* index, uint8_t delimiter, uint8_t bufferSize)
{
  while(Serial.available() > 0)
  {
    uint8_t received = (uint8_t)Serial.read();

    if(*index == bufferSize)
        return false;

    if(received == delimiter)
      return true;

    // Otherwise continue filling the buffer    
    buffer[*index] = received;
    *index = *index + 1;
  }

  return false;
}

//---------------------------------------------------------
//=
//---------------------------------------------------------
void SendData(uint8_t* rawBuffer, uint8_t* packageBuffer, bool* waitingAck, uint32_t* ackTimer, uint32_t now)
{
  uint8_t encodeSize =  EncodePackage(rawBuffer, 8, packageBuffer);
  Serial.write(packageBuffer, encodeSize);

  *waitingAck = true;
  *ackTimer = now;
}

//---------------------------------------------------------
// 
//---------------------------------------------------------
uint8_t EncodePackage(uint8_t* inBuffer, uint8_t size, uint8_t* outBuffer)
{
    // Add checksum
    inBuffer[size] = size + 1;
    size++;

    // Encode message
    size_t outSize = Encode(inBuffer, size, outBuffer);

    // Add packet delimiter
    outBuffer[outSize] = 0;
    outSize++;

    return outSize;
}

bool DecodePackage(const uint8_t* inBuffer, uint8_t size, uint8_t* outBuffer)
{
  // Decode received message
  uint8_t outSize = Decode(inBuffer, size, outBuffer);

  // Check message size is greater than 0
  if(outSize == 0)
    return false;

  // Verify message checksum
  uint8_t checksum = outBuffer[outSize - 1];
  if(checksum != outSize)
    return false;

  return true;
}

//-----------------------------------------------------------------------------
// \brief Encode a byte buffer with the COBS encoder.
// \param inBuffer A pointer to the unencoded buffer to encode.
// \param size  The number of bytes in the \p inBuffer.
// \param outBuffer The buffer for the encoded bytes.
// \returns The number of bytes written to the \p outBuffer.
// \warning The encodedBuffer must have at least getEncodedBufferSize() 
//          allocated.
//-----------------------------------------------------------------------------
uint8_t Encode(const uint8_t* inBuffer, uint8_t size, uint8_t* outBuffer)
{
    uint8_t inIndex  = 0;
    uint8_t outIndex = 1;
    uint8_t codeIndex  = 0;
    uint8_t code       = 1;

    while (inIndex < size)
    {
        if (inBuffer[inIndex] == 0)
        {
            outBuffer[codeIndex] = code;
            code = 1;
            codeIndex = outIndex++;
            inIndex++;
        }
        else
        {
            outBuffer[outIndex++] = inBuffer[inIndex++];
            code++;

            if (code == 0xFF)
            {
                outBuffer[codeIndex] = code;
                code = 1;
                codeIndex = outIndex++;
            }
        }
    }

    outBuffer[codeIndex] = code;
    return outIndex;
}

//-----------------------------------------------------------------------------
// \brief Decode a COBS-encoded buffer.
// \param inBuffer A pointer to the \p inBuffer to decode.
// \param size The number of bytes in the \p inBuffer.
// \param outBuffer The target buffer for the decoded bytes.
// \returns The number of bytes written to the \p outBuffer.
// \warning decodedBuffer must have a minimum capacity of size.
//-----------------------------------------------------------------------------
uint8_t Decode(const uint8_t* inBuffer, uint8_t size, uint8_t* outBuffer)
{
    if (size == 0)
        return 0;

    uint8_t inIndex = 0;
    uint8_t outIndex = 0;
    uint8_t code = 0;
    uint8_t i = 0;

    while (inIndex < size)
    {
        code = inBuffer[inIndex];
        
        if (inIndex + code > size && code != 1)
            return 0;

        inIndex++;

        for (i = 1; i < code; i++)
            outBuffer[outIndex++] = inBuffer[inIndex++];

        if (code != 0xFF && inIndex != size)
            outBuffer[outIndex++] = '\0';
    }

    return outIndex;
}
