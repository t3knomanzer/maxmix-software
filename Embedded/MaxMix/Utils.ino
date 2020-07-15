//********************************************************
// PROJECT: MAXMIX
// AUTHOR: Ruben Henares
// EMAIL: rhenares0@gmail.com
//
// DECRIPTION:
// 
//
//
//********************************************************


//********************************************************
// *** FUNCTIONS
//********************************************************
//---------------------------------------------------------
//---------------------------------------------------------
void ClearBuffer(uint8_t* buffer, uint8_t size)
{
  for(size_t i = 0; i < size; i++)
    buffer[i] = 0;
}

//---------------------------------------------------------
//---------------------------------------------------------
uint8_t GetCommandFromPackage(uint8_t* packageBuffer)
{
    return packageBuffer[0];
}

//---------------------------------------------------------
//---------------------------------------------------------
uint32_t GetIdFromPackage(uint8_t* packageBuffer)
{
    uint32_t id = ((uint32_t)packageBuffer[1]) |
                  ((uint32_t)packageBuffer[2] << 8)  |
                  ((uint32_t)packageBuffer[3] << 16) |
                  ((uint32_t)packageBuffer[4] << 24);
    return id;
}