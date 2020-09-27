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
bool CanScrollLeft(void)
{
    if ((g_Settings.continuousScroll && g_SessionInfo.count > 1) || (g_SessionInfo.count > 0))
        return true;

    return false;
}

//---------------------------------------------------------
//---------------------------------------------------------
bool CanScrollRight(void)
{
    if ((g_Settings.continuousScroll && g_SessionInfo.count > 1) || ((g_SessionInfo.count - g_SessionInfo.current - 1) > 0))
        return true;

    return false;
}