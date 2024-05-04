// DreamshellUtilities.cpp : Define las funciones exportadas del archivo DLL.
//

#include "pch.h"
#include "framework.h"
#include "DreamshellUtilities.h"

// Constructor de clase exportada.
DreamshellUtilities::DreamshellUtilities()
{
    isoLoaderPtr = new IsoLoader();
    return;
}

DreamshellUtilities::~DreamshellUtilities()
{
    delete isoLoaderPtr;
}

DLL_EXPORT DreamshellUtilities* CreateInstance()
{
    return new DreamshellUtilities();
}

DLL_EXPORT void DestroyInstance(DreamshellUtilities *ptr)
{
    if (ptr != NULL)
    {
        delete ptr;
        ptr = NULL;
    }
}

DLL_EXPORT char* MakePresetFileName(DreamshellUtilities* ptr, char trackFile[], bool sd)
{
    char* output = NULL;
    if (ptr != NULL)
    {
        ptr->isoLoaderPtr->MakePresetFileName(trackFile, sd, output);
    }
    return output;
}

DLL_EXPORT void FreeCharPointer(char* ptr)
{
    if (ptr != NULL)
    {
        delete[] ptr;
        ptr = NULL;
    }
}

DLL_EXPORT ipbin_meta_t ReadIpData(DreamshellUtilities* ptr, char trackFile[])
{
    ipbin_meta_t ipbinStruct = {};
    if (ptr != NULL)
    {
        ipbinStruct = ptr->isoLoaderPtr->ReadIpData(trackFile);
    }
    return ipbinStruct;
}