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
void SendAcknowledgment(uint8_t* rawBuffer, uint8_t* packageBuffer, uint8_t revision)
{
  rawBuffer[0] = MSG_COMMAND_ACKNOWLEDGMENT;
  rawBuffer[1] = revision;
  uint8_t encodeSize =  EncodePackage(rawBuffer, 2, packageBuffer);
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
void SendSetDefaultEndpointCommand(Item* item, uint8_t* rawBuffer, uint8_t* packageBuffer)
{
  rawBuffer[0] = MSG_COMMAND_SET_DEFAULT_ENDPOINT;
  
  rawBuffer[1] = (uint8_t)(item->id >> 24) & 0xFF;
  rawBuffer[2] = (uint8_t)(item->id >> 16) & 0xFF;
  rawBuffer[3] = (uint8_t)(item->id >> 8) & 0xFF;
  rawBuffer[4] = (uint8_t)item->id & 0xFF;
 
  uint8_t encodeSize =  EncodePackage(rawBuffer, 5, packageBuffer);
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
  memcpy(itemsBuffer[itemIndex].name, &packageBuffer[6], ITEM_BUFFER_NAME_SIZE);
  itemsBuffer[itemIndex].volume = (uint8_t)packageBuffer[30];
  itemsBuffer[itemIndex].isMuted = (uint8_t)packageBuffer[31];
}

//---------------------------------------------------------
//---------------------------------------------------------
void RemoveItemCommand(uint8_t* packageBuffer, Item* itemsBuffer, uint8_t* itemCount, uint8_t itemIndex)
{
  // Re-order items array
  for(uint8_t i = itemIndex; i < *itemCount - 1; i++)
    itemsBuffer[i] = itemsBuffer[i + 1];

  *itemCount = *itemCount - 1;
}

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateItemVolumeCommand(uint8_t* packageBuffer, Item* itemsBuffer, uint8_t index) 
{
  itemsBuffer[index].volume = packageBuffer[6];
  itemsBuffer[index].isMuted = packageBuffer[7];
}

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateSettingsCommand(uint8_t* packageBuffer, Settings* settings) 
{
  settings->displayNewItem = packageBuffer[2];
  settings->sleepWhenInactive = packageBuffer[3];
  settings->sleepAfterSeconds = packageBuffer[4];
  settings->continuousScroll = packageBuffer[5];
  settings->accelerationPercentage = packageBuffer[6];

  uint16_t dblTapTime = ((uint16_t)packageBuffer[7]) | ((uint16_t)packageBuffer[8] << 8);
  encoderButton.doubleTapTime(dblTapTime);

  settings->volumeMinColor.b = packageBuffer[9];
  settings->volumeMinColor.g = packageBuffer[10];
  settings->volumeMinColor.r = packageBuffer[11];

  settings->volumeMaxColor.b = packageBuffer[12];
  settings->volumeMaxColor.g = packageBuffer[13];
  settings->volumeMaxColor.r = packageBuffer[14];

  settings->mixChannelAColor.b = packageBuffer[15];
  settings->mixChannelAColor.g = packageBuffer[16];
  settings->mixChannelAColor.r = packageBuffer[17];

  settings->mixChannelBColor.b = packageBuffer[18];
  settings->mixChannelBColor.g = packageBuffer[19];
  settings->mixChannelBColor.r = packageBuffer[20];
}

//---------------------------------------------------------
//---------------------------------------------------------
uint8_t GetRevisionFromPackage(uint8_t* packageBuffer)
{
    return packageBuffer[0];
}
//---------------------------------------------------------
//---------------------------------------------------------
uint8_t GetCommandFromPackage(uint8_t* packageBuffer)
{
    return packageBuffer[1];
}

//---------------------------------------------------------
//---------------------------------------------------------
uint32_t GetIdFromPackage(uint8_t* packageBuffer)
{
    uint32_t id = ((uint32_t)packageBuffer[2]) |
                  ((uint32_t)packageBuffer[3] << 8)  |
                  ((uint32_t)packageBuffer[4] << 16) |
                  ((uint32_t)packageBuffer[5] << 24);
    return id;
}

bool GetIsDeviceFromAddPackage(uint8_t* packageBuffer)
{
    return packageBuffer[32] > 0;
}

uint8_t GetDeviceFlowFromAddPackage(uint8_t* packageBuffer)
{
    return packageBuffer[33];
}

bool GetIsDeviceFromRemovePackage(uint8_t* packageBuffer)
{
    return packageBuffer[6] > 0;
}

bool GetIsDeviceFromUpdatePackage(uint8_t* packageBuffer)
{
    return packageBuffer[8] > 0;
}

uint8_t GetDeviceFlowFromUpdatePackage(uint8_t* packageBuffer)
{
    return packageBuffer[9];
}

uint8_t GetDeviceFlowFromDefaultEndpointPackage(uint8_t* packageBuffer)
{
    return packageBuffer[6];
}
