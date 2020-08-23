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
