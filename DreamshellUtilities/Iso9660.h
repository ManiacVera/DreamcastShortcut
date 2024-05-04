#pragma once

/*
* DCTools - DreamCast CD-R authoring tools.
* Copyright (c) 2013 Bruno Freitas - bootsector@ig.com.br
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

#ifndef ISO9660_H_
#define ISO9660_H_

#include <stdio.h>
#include <stdint.h>
#include <string.h>
#include <stdlib.h>

#ifdef _WIN32
#define WINDOWS_PLATFORM
#endif
#ifdef _WIN64
#define WINDOWS_PLATFORM
#endif

#define DIR_SEP "/"

#ifdef WINDOWS_PLATFORM
#include <direct.h>
#else
#include <sys/stat.h>
#endif

#define SECTOR_SIZE	2048
#define CDDA_SECTOR_SIZE	2352

struct iso9660_dir_info_s {
	char name[255];
	uint16_t id;
	uint16_t parent_id;
	uint32_t lba;
};

typedef struct iso9660_dir_info_s iso9660_dir_info_t;

struct iso9660_file_info_s {
	char full_name[4096];
	char file_name[256];
	uint16_t parent_dir;
	uint32_t lba;
	uint32_t size;
};

typedef struct iso9660_file_info_s iso9660_file_info_t;

struct gdi_struct_info_s {
	uint32_t index;
	uint32_t offset;
	uint16_t type;
	uint32_t start_offset;
	char name[255];
	uint16_t dummy;
};

typedef struct gdi_struct_info_s gdi_struct_info_t;

class Iso9660
{
private:
	const char* GetFileNameExtension(const char* filename);
	FILE* iso_file;
	uint8_t lba_buffer[SECTOR_SIZE];
	uint8_t* path_table;
	uint32_t path_table_size;
	iso9660_file_info_t* files_list;
	uint32_t files_list_size = 0;
	gdi_struct_info_t* gdi_files_list = NULL;
	char basedir[2048];

	//int WriteSortFileHelper(const void* a, const void* b);

public:
	Iso9660(char basedir[2048]);
	~Iso9660();

	int OpenGDI(char* filename, uint32_t lba_offset);
	int NumDirs(void);
	int OpenISO(char* filename, uint32_t lba_offset);
	void CloseISO(void);
	int GetDirInfo(int id, iso9660_dir_info_t* dir_info);
	int ExtractFiles();
	char* DirTree(int directory_id);
	void DirTreeHelper(int directory_id, char* dtree);
	void CreateDirTree(int directory_id);
	int DumpBootSector(char* outfilename);
	int WriteSortFile(char* outfilename);
	int GetCDDASize();	
	bool CreateDummyFile(const char* filename, int size);
	void MergeFiles(const char output_file[2048], char* files[]);
	void CreateDirTreeHelper(int directory_id, char* dtree);
	void WriteFile(char* output_file, uint32_t lba, uint32_t size);
	int ListFiles();
	int ReadFile(const char fileName[2048], char*& databuffer);
};

#endif /* ISO9660_H_ */