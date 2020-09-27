#pragma once

#include <Config.h>
#include <Communications/Messages.h>

namespace Communications
{
    void Initialize(void);
    Command Read(void);
    void Write(Command command);
}
