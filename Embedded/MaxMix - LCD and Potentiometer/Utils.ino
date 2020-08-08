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

//---------------------------------------------------------
//---------------------------------------------------------
uint8_t CanScrollLeft(uint8_t itemIndex, uint8_t itemCount, uint8_t continuousScroll)
{
  if((continuousScroll && itemCount > 1) || (itemIndex > 0))
    return true;
  
  return false;
}

//---------------------------------------------------------
//---------------------------------------------------------
uint8_t CanScrollRight(uint8_t itemIndex, uint8_t itemCount, uint8_t continuousScroll)
{
  if((continuousScroll && itemCount > 1) || ((itemCount - itemIndex - 1) > 0))
    return true;

  return false;
}

//---------------------------------------------------------
//---------------------------------------------------------
int8_t GetNextIndex(int8_t itemIndex, uint8_t itemCount, int8_t direction, uint8_t loop)
{
  itemIndex += direction;
  if(loop)
  {
    if(itemIndex >= itemCount)
      itemIndex = 0;
    else if(itemIndex < 0)
      itemIndex = itemCount - 1;
  }
  else
    itemIndex = constrain(itemIndex, 0, itemCount - 1);

  return itemIndex;
}