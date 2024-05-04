#include "pch.h"
#include "IsoLoader.h"
#include "md5.h"
#include <string.h>
#include <cstdio>
//#include "framework.h"

IsoLoader::IsoLoader()
{
	//char* output = (char*)malloc(2000);
	//MakePresetFileName((char*)"C:\\JDownloader\\Dreamcast\\Crazy Taxi\\track03.iso", false, output);
}

bool IsoLoader::ReadBootSector(char trackFile[], uint8*& bootSector)
{
	HANDLE hFile = CreateFileA(trackFile, GENERIC_READ, FILE_SHARE_READ
		, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);

	if (hFile != INVALID_HANDLE_VALUE)  // first thing to do
	{
		const int size = 3100;
		DWORD nRead = 0;
		DWORD nNumberOfBytesRead;
		char* boot_string = (char*)HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, (size) * sizeof(char));
		const int maxReadAttemps = 2;
		int readAttemps = 0;

		if (ReadFile(hFile, boot_string, boot_sector_size + (16 * maxReadAttemps), &nRead, NULL) == TRUE)
		{
		READ_BOOT_SECTOR:
			if (readAttemps < maxReadAttemps && boot_string[16 * readAttemps] == NULL)
			{
				readAttemps++;
				goto READ_BOOT_SECTOR;
			}

			if (boot_string[16 * readAttemps] != NULL)
			{
				bootSector = new uint8[boot_sector_size];
				memset(bootSector, 0, boot_sector_size);
				memcpy(bootSector, &boot_string[16 * readAttemps], boot_sector_size);
			}
			else
			{
				bootSector = NULL;
			}
		}
		else
		{
			bootSector = NULL;
		}

		HeapFree(GetProcessHeap(), 0, boot_string);
	}
	else
	{
		bootSector = NULL;
	}

	return bootSector != NULL;
}

ipbin_meta_t IsoLoader::ReadIpData(char trackFile[])
{
	ipbin_meta_t data = {};
	uint8* boot_sector = NULL;

	memset(&data, 0, sizeof(ipbin_meta_t));
	if (ReadBootSector(trackFile, boot_sector) != NULL)
	{
		memcpy(&data, boot_sector, data_sector_size);
	}

	return data;
}

void IsoLoader::MakePresetFileName(char trackFile[], bool sd, char*& outputPresetName)
{
	uint8* boot_sector = NULL;

	if (ReadBootSector(trackFile, boot_sector) != NULL)
	{
		md5* md5Ptr = new md5();
		uint8 varMD5[16];

		md5Ptr->kos_md5(boot_sector, boot_sector_size, varMD5);

		const int sizePresetName = 100;
		outputPresetName = new char[sizePresetName];
		memset(outputPresetName, 0, sizePresetName);
		snprintf(outputPresetName, sizePresetName,
			"%s_%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x.cfg",
			sd ? "sd" : "ide", varMD5[0],
			varMD5[1], varMD5[2], varMD5[3], varMD5[4], varMD5[5],
			varMD5[6], varMD5[7], varMD5[8], varMD5[9], varMD5[10],
			varMD5[11], varMD5[12], varMD5[13], varMD5[14], varMD5[15]);

		delete md5Ptr;
	}
}