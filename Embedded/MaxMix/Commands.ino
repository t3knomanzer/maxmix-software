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
void SendHandshakeCommand(uint8_t* rawBuffer, uint8_t* packageBuffer)
{
  rawBuffer[0] = MSG_COMMAND_HS_RESPONSE;
  uint8_t encodeSize =  EncodePackage(rawBuffer, 1, packageBuffer);
  Serial.write(packageBuffer, encodeSize);
}

//---------------------------------------------------------
//---------------------------------------------------------
void SendItemVolumeCommand(Item* item, uint8_t* rawBuffer, uint8_t* packageBuffer)
{
  rawBuffer[0] = MSG_COMMAND_UPDATE_VOLUME;
  
  rawBuffer[1] = (uint8_t)(item->id >> 24) & 0xFF;
  rawBuffer[2] = (uint8_t)(item->id >> 16) & 0xFF;
  rawBuffer[3] = (uint8_t)(item->id >> 8) & 0xFF;
  rawBuffer[4] = (uint8_t)item->id & 0xFF;

  rawBuffer[5] = item->volume;
  rawBuffer[6] = item->isMuted;
  
  uint8_t encodeSize =  EncodePackage(rawBuffer, 7, packageBuffer);
  Serial.write(packageBuffer, encodeSize);
}

//---------------------------------------------------------
//---------------------------------------------------------
void AddItemCommand(uint8_t* packageBuffer, Item* itemsBuffer, uint8_t* itemCount)
{
  UpdateItemCommand(packageBuffer, itemsBuffer, *itemCount);
  *itemCount = *itemCount + 1;
}

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateItemCommand(uint8_t* packageBuffer, Item* itemsBuffer, uint8_t itemIndex)
{
  itemsBuffer[itemIndex].id = GetIdFromPackage(packageBuffer);
  memcpy(itemsBuffer[itemIndex].name, &packageBuffer[5], ITEM_BUFFER_NAME_SIZE);
  itemsBuffer[itemIndex].volume = (uint8_t)packageBuffer[41];
  itemsBuffer[itemIndex].isMuted = (uint8_t)packageBuffer[42];
}

//---------------------------------------------------------
//---------------------------------------------------------
void RemoveItemCommand(uint8_t* packageBuffer, Item* itemsBuffer, uint8_t* itemCount, uint8_t itemIndex)
{
  // Re-order items array
  for(uint8_t i = itemIndex; i < *itemCount - 1; i++)
    items[i] = items[i + 1];

  *itemCount = *itemCount - 1;
}

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateItemVolumeCommand(uint8_t* packageBuffer, Item* itemsBuffer, uint8_t index) 
{
    itemsBuffer[index].volume = packageBuffer[5];
    itemsBuffer[index].isMuted = packageBuffer[6];
}

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateSettingsCommand(uint8_t* packageBuffer, Settings* settings) 
{
  settings->displayNewSession = packageBuffer[1];
  settings->sleepWhenInactive = packageBuffer[2];
  settings->sleepAfterSeconds = packageBuffer[3];
}

