// El siguiente bloque ifdef muestra la forma estándar de crear macros que hacen la exportación
// de un DLL más sencillo. Todos los archivos de este archivo DLL se compilan con DREAMSHELLUTILITIES_EXPORTS
// símbolo definido en la línea de comandos. Este símbolo no debe definirse en ningún proyecto
// que use este archivo DLL. De este modo, otros proyectos cuyos archivos de código fuente incluyan el archivo verán
// interpretan que las funciones DREAMSHELLUTILITIES_API se importan de un archivo DLL, mientras que este archivo DLL interpreta los símbolos
// definidos en esta macro como si fueran exportados.

#pragma once

//#include "types.h"
#include "IsoLoader.h"

class DLL_EXPORT DreamshellUtilities
{
private:
	

public:
	IsoLoader* isoLoaderPtr;

	DreamshellUtilities(void);
	~DreamshellUtilities();
};

extern "C"
{
	DLL_EXPORT DreamshellUtilities* CreateInstance();

	DLL_EXPORT void DestroyInstance(DreamshellUtilities* ptr);

	DLL_EXPORT char* MakePresetFileName(DreamshellUtilities* ptr, char trackFile[], bool sd);

	DLL_EXPORT void FreeCharPointer(char* ptr);

	//DLL_EXPORT bool ReadBootSector(DreamshellUtilities* ptr, char trackFile[], uint8*& bootSector);

	DLL_EXPORT ipbin_meta_t ReadIpData(DreamshellUtilities* ptr, char trackFile[]);
}

//extern DLL_EXPORT int VariableExportada;
