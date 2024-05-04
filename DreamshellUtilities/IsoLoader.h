#pragma once

#include "types.h"

/**
 * IP.BIN (boot sector) meta info
 */
typedef struct ipbin_meta
{
	char hardware_ID[16];
	char maker_ID[16];
	char device_info[16];
	char country_codes[8];
	char ctrl[4];
	char dev[1];
	char VGA[1];
	char WinCE[1];
	char unk[1];
	char product_ID[10];
	char product_version[6];
	char release_date[16];
	char boot_file[16];
	char software_maker_info[16];
	char title[32];
} ipbin_meta_t;

class IsoLoader
{	
const int boot_sector_size = 2048;
const int data_sector_size = 160;

public:
	IsoLoader();
	ipbin_meta_t ReadIpData(char trackFile[]);
	bool ReadBootSector(char trackFile[], uint8*& bootSector);
	void MakePresetFileName(char trackFile[], bool sd, char*& outputPresetName);
};

