// dllmain.cpp : Define el punto de entrada de la aplicación DLL.
#include "pch.h"
//#include "IsoLoader.h"

//DLL_EXPORT void* CreateInstance();
//
//DLL_EXPORT void* CreateInstance()
//{
//    return NULL;
//}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

