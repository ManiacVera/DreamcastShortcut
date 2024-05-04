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


#include "Iso9660.h"
#include "pch.h"

uint32_t LBA_OFFSET = 45000;

Iso9660::Iso9660(char basedir[2048])
{
	memset(this->basedir, 0, sizeof(this->basedir));
	strcpy(this->basedir, basedir);

	iso_file = NULL;
	path_table = NULL;
	files_list = NULL;
	files_list_size = 0;
	gdi_files_list = NULL;
}

Iso9660::~Iso9660()
{
	CloseISO();
}

int Iso9660::OpenGDI(char* filename, uint32_t lba_offset) {
	int size = 1024, pos;
	int c;
	char* buffer = (char*)malloc(size);
	int number_line = 0;
	int list_size = 1;
	
	CloseISO();

	FILE* f = fopen(filename, "r");
	if (f) {
		const char delimiter[2] = " ";
		do { // read all lines in file
			FIRST_LINE:
			pos = 0;
			do { // read one line
				c = fgetc(f);
				if (c != EOF) buffer[pos++] = (char)c;
				if (pos >= size - 1) { // increase buffer length - leave room for 0
					size *= 2;
					buffer = (char*)realloc(buffer, size);
				}
			} while (c != EOF && c != '\n');

			if (number_line == 0)
			{
				number_line++;
				goto FIRST_LINE;
			}
			number_line++;
			buffer[pos] = 0;

			// line is now in buffer
			if (buffer != NULL && buffer[0] != ' ' && buffer[0] != '\0')
			{
				if (gdi_files_list == NULL) {
					gdi_files_list = (gdi_struct_info_t*)malloc(list_size * sizeof(gdi_struct_info_t));
				}
				else {
					gdi_files_list = (gdi_struct_info_t*)realloc(gdi_files_list, list_size * sizeof(gdi_struct_info_t));
				}

				char* ptr = NULL;
				char* output;
				
				ptr = strtok(buffer, delimiter);
				gdi_files_list[list_size - 1].index = strtol(ptr, &output, 10);

				ptr = strtok(NULL, delimiter);
				gdi_files_list[list_size - 1].offset = strtol(ptr, &output, 10);

				ptr = strtok(NULL, delimiter);
				gdi_files_list[list_size - 1].type = strtol(ptr, &output, 10);

				ptr = strtok(NULL, delimiter);
				gdi_files_list[list_size - 1].start_offset = strtol(ptr, &output, 10);

				ptr = strtok(NULL, delimiter);
				memset(gdi_files_list[list_size - 1].name, 0, 255);
				memcpy(gdi_files_list[list_size - 1].name, ptr, strlen(ptr));

				ptr = strtok(NULL, delimiter);
				gdi_files_list[list_size - 1].dummy = strtol(ptr, &output, 10) + 1;

				list_size++;
			}

		} while (c != EOF);
		fclose(f);
	}

	if (list_size > 0)
	{
		gdi_files_list = (gdi_struct_info_t*)realloc(gdi_files_list, list_size * sizeof(gdi_struct_info_t));
		memset(&gdi_files_list[list_size-1], NULL, sizeof(gdi_struct_info_t));
	}

	return 0;
}

const char* Iso9660::GetFileNameExtension(const char* filename)
{
	const char* dot = strrchr(filename, '.');
	if (!dot || dot == filename) return "";
	return dot;
}

bool Iso9660::CreateDummyFile(const char* filename, int size)
{
	FILE* fp = fopen(filename, "wb");
	fseek(fp, size, SEEK_SET);
	fputc('\0', fp);
	fclose(fp);

	return true;
}

void Iso9660::MergeFiles(const char output_file[2048], char* files[])
{
	char buf[1024];

	FILE* output = fopen(output_file, "wb");
	//FILE* output = stdout;

	// Iterate through the filenames given on the command line.
	for (int i = 0; files[i] != NULL; i++) {
		// Open the file for binary reading.
		char* filename = files[i];
		FILE* f = fopen(filename, "rb");
		if (f == NULL) {
			fprintf(stderr, "Could not open %s: %s\n", filename, strerror(errno));
		}

		// Read a BUFSIZ chunk, write it to the output.
		size_t bytes_read;
		while ((bytes_read = fread(buf, 1, sizeof(buf), f)) > 0) {
			fwrite(buf, 1, bytes_read, output);
		}

		// Close the input file.
		fclose(f);
	}

	if (output != stdout) {
		fclose(output);
	}
}

int Iso9660::GetCDDASize()
{
	long dummySize = 0;
	if (gdi_files_list != NULL)
	{
		gdi_struct_info_t* ptr = &gdi_files_list[0];
		int count = 0;
		char fileName[2048];

		while (ptr->name[0] != NULL)
		{
			if (ptr->index >= 3 && strcmp(GetFileNameExtension(ptr->name), ".raw") == 0)
			{
				sprintf(fileName, "%s%s", "C:\\JDownloader\\Dreamcast\\RetroDream V3\\GAMES\\UNREAL TOURNAMENT PAL 60HZ\\", ptr->name);

				FILE* file = fopen(fileName, "rb");
				if (file) {
					fseek(file, 0L, SEEK_END);
					dummySize += ftell(file);
					fclose(file);
					count++;
				}
			}
			ptr++;
		}

		if (dummySize > 0)
		{
			dummySize = (dummySize / CDDA_SECTOR_SIZE + 300) * SECTOR_SIZE;
		}
	}

	return dummySize;
}

int Iso9660::OpenISO(char* filename, uint32_t lba_offset) {
	uint8_t iso_id[] = { 0x01, 0x43, 0x44, 0x30, 0x30, 0x31 };
	uint32_t pvd_path_table_lba;

	CloseISO();

	LBA_OFFSET = lba_offset;

	if ((iso_file = fopen(filename, "rb")) == NULL) {
		printf("Error opening input file\n");
		return 1;
	}

	fseek(iso_file, 16 * SECTOR_SIZE, SEEK_SET);
	fread(lba_buffer, sizeof(lba_buffer), 1, iso_file);

	if (memcmp(iso_id, lba_buffer, 6) != 0) {
		printf("Error: Input file is not an ISO file\n");
		fclose(iso_file);
		return 1;
	}

	path_table_size = *((uint32_t*)(lba_buffer + 132));
	pvd_path_table_lba = *((uint32_t*)(lba_buffer + 140)) - LBA_OFFSET;

	path_table = (uint8_t *)malloc(path_table_size);

	fseek(iso_file, pvd_path_table_lba * SECTOR_SIZE, SEEK_SET);
	fread(path_table, path_table_size, 1, iso_file);

	// Check if first directory in path table is root.
	// If not, LBA_OFFSET is probably incorrect
	if ((path_table[0] != 0x01) || (path_table[8] != 0x00)) {
		printf("Error: First directory in the path table is not root.\n<lba_offset> is probably incorrect!\n");

		fclose(iso_file);
		free(path_table);

		return 1;
	}

	return 0;
}

int Iso9660::NumDirs(void) {
	int i, n, c;

	i = 0;
	c = 0;

	while (i < path_table_size) {
		n = path_table[i];

		c++;

		i += 8 + n + ((n % 2) ? 1 : 0);
	}

	return c;
}

int Iso9660::GetDirInfo(int id, iso9660_dir_info_t* dir_info) {
	int i, n, c;

	if ((id > NumDirs()) || (id < 1)) {
		return 1;
	}

	i = 0;
	c = 1;

	while (i < path_table_size) {
		n = path_table[i];

		if (c == id) {
			memset(dir_info, 0x00, sizeof(iso9660_dir_info_t));

			dir_info->id = c;
			dir_info->parent_id = *((uint16_t*)(path_table + 6 + i));
			dir_info->lba = *((uint32_t*)(path_table + 2 + i)) - LBA_OFFSET;

			if (path_table[i + 8] == 0x00) {
				strcpy(dir_info->name, DIR_SEP);
			}
			else {
				strncpy(dir_info->name, (const char*)&path_table[i + 8], n);
			}

			break;
		}

		i += 8 + n + ((n % 2) ? 1 : 0);
		c++;
	}

	return 0;
}

void Iso9660::WriteFile(char* output_file, uint32_t lba, uint32_t size) {
	FILE* outfile;
	uint8_t* databuffer;

	if ((databuffer = (uint8_t*)malloc(size)) == NULL) {
		printf("Error allocating %d bytes for reading file\n", size);
		return;
	}

	if ((outfile = fopen(output_file, "wb")) == NULL) {
		printf("Error creating output file: %s\n", output_file);

		free(databuffer);

		return;
	}

	printf("%s - %d\n", output_file, size);

	fseek(iso_file, lba * SECTOR_SIZE, SEEK_SET);
	fread(databuffer, size, 1, iso_file);

	fwrite(databuffer, size, 1, outfile);

	fclose(outfile);
	free(databuffer);
}

int Iso9660::ListFiles()
{
	if (files_list_size > 0 && files_list != NULL)
	{
		free(files_list);
		files_list = NULL;
	}

	files_list_size = 0;
	int numDirectories = NumDirs();
	int countDirectory = 1;
	int directory_id = 1;

	for (directory_id = 1; directory_id <= numDirectories; directory_id++)
	{
		iso9660_dir_info_t dir;
		int i, n;
		char file_name[256];
		char output_file_name[4096];
		char bdir[256];
		char* p;

		uint8_t* dir_record;
		uint32_t dir_record_size;

		if (GetDirInfo(directory_id, &dir) != 0) {
			printf("Error getting files for directory: %s\n", dir.name);
			return 1;
		}

		// Sanitize base dir (remove trailing slash)
		strcpy(bdir, basedir);

		if (bdir[strlen(bdir) - 1] == DIR_SEP[0]) {
			bdir[strlen(bdir) - 1] = '\0';
		}

		// Find out and allocate necessary data for the whole directory record
		fseek(iso_file, dir.lba * SECTOR_SIZE, SEEK_SET);
		fread(lba_buffer, SECTOR_SIZE, 1, iso_file);

		dir_record_size = *((uint32_t*)(lba_buffer + 10));

		if ((dir_record = (uint8_t*)malloc(dir_record_size)) == NULL) {
			printf("Error allocating memory for the directory table\n");
			return 1;
		}

		// Read whole directory record
		fseek(iso_file, dir.lba * SECTOR_SIZE, SEEK_SET);
		fread(dir_record, dir_record_size, 1, iso_file);

		i = 0;

		while (i < dir_record_size) {
			n = dir_record[i];

			// Found padding bytes... Skipping remaining data until next LBA
			if (n == 0x00) {
				i++;
				continue;
			}

			// If not a directory entry
			if (!(dir_record[25 + i] & (1 << 1))) {

				// Get file name
				memset(file_name, 0x00, sizeof(file_name));

				strncpy(file_name, (const char*)&dir_record[33 + i], dir_record[32 + i]);

				if ((p = strrchr(file_name, ';')) != NULL) {
					*p = '\0';
				}

				sprintf(output_file_name, "%s%s%s", bdir, DirTree(directory_id), file_name);

				// Add file information to the files_list dynamic array
				files_list_size++;

				if (files_list == NULL) {
					files_list = (iso9660_file_info_t*)malloc(files_list_size * sizeof(iso9660_file_info_t));
				}
				else {
					files_list = (iso9660_file_info_t*)realloc(files_list, files_list_size * sizeof(iso9660_file_info_t));
				}

				strcpy(files_list[files_list_size - 1].full_name, output_file_name);
				strcpy(files_list[files_list_size - 1].file_name, file_name);

				files_list[files_list_size - 1].lba = *((uint32_t*)(dir_record + 2 + i)) - LBA_OFFSET;
				files_list[files_list_size - 1].parent_dir = directory_id;
				files_list[files_list_size - 1].size = *((uint32_t*)(dir_record + 10 + i));

				//// Write file contents do disk
				//WriteFile(output_file_name, *((uint32_t*)(dir_record + 2 + i)) - LBA_OFFSET, *((uint32_t*)(dir_record + 10 + i)));
			}

			i += n;
		}

		free(dir_record);
	}

	return files_list_size;
}

int Iso9660::ReadFile(const char fileName[2048], char *&databuffer)
{
	if (ListFiles() > 0)
	{
		int countFile = 0;	

		for (countFile = 0; countFile < files_list_size; countFile++)
		{
			if (strcmp(files_list[countFile].file_name, fileName) == 0)
			{
				//uint8_t* databuffer;

				if ((databuffer = (char*)malloc(files_list[countFile].size)) == NULL) {
					printf("Error allocating %d bytes for reading file\n", files_list[countFile].size);
					return -1;
				}

				//output = (char*)malloc(files_list[countFile].size);
				//memset(output, 0, files_list[countFile].size);
				////memcpy(output, files_list[countFile].)

				fseek(iso_file, files_list[countFile].lba * SECTOR_SIZE, SEEK_SET);
				fread(databuffer, files_list[countFile].size, 1, iso_file);

				break;
			}
		}
	}
	return 1;
}

int Iso9660::ExtractFiles()
{
	iso9660_dir_info_t dir;

	if (ListFiles() > 0)
	{
		int numDirectories =  NumDirs();
		int countDirectory = 0;
		for (countDirectory = 1; countDirectory <= numDirectories; countDirectory++)
		{
			CreateDirTree(countDirectory);
		}

		int countFile = 0;
		for (countFile = 0; countFile < files_list_size; countFile++)
		{
			// Write file contents do disk
			WriteFile(files_list[countFile].full_name, files_list[countFile].lba, files_list[countFile].size);
		}
	}

	return 0;
}

void Iso9660::DirTreeHelper(int directory_id, char* dtree) {
	iso9660_dir_info_t dir;

	GetDirInfo(directory_id, &dir);

	if (dir.id == 1) {
		//printf("%s\n", dir.name);
		strcat(dtree, dir.name);

		return;
	}

	DirTreeHelper(dir.parent_id, dtree);

	//printf("%s\n", dir.name);
	strcat(dtree, dir.name);
	strcat(dtree, DIR_SEP);
}

char* Iso9660::DirTree(int directory_id) {
	static char dir_tree[2048];

	memset(dir_tree, 0x00, sizeof(dir_tree));

	DirTreeHelper(directory_id, dir_tree);

	if (dir_tree[strlen(dir_tree) - 1] != DIR_SEP[0]) {
		strcat(dir_tree, DIR_SEP);
	}

	return dir_tree;
}

void Iso9660::CreateDirTreeHelper(int directory_id, char* dtree) {
	iso9660_dir_info_t dir;

	GetDirInfo(directory_id, &dir);

	if (dir.id == 1) {
		//printf("%s\n", dir.name);
		strcat(dtree, dir.name);
		//printf("mkdir %s\n", dtree);

#ifdef WINDOWS_PLATFORM
		_mkdir(dtree);
#else
		mkdir(dtree, S_IRWXU | S_IRWXG | S_IRWXO);
#endif

		return;
	}

	CreateDirTreeHelper(dir.parent_id, dtree);

	//printf("%s\n", dir.name);
	strcat(dtree, dir.name);
	strcat(dtree, DIR_SEP);

	//printf("mkdir %s\n", dtree);
#ifdef WINDOWS_PLATFORM
	_mkdir(dtree);
#else
	mkdir(dtree, S_IRWXU | S_IRWXG | S_IRWXO);
#endif
}

void Iso9660::CreateDirTree(int directory_id) {
	static char dir_tree[2048];

	strcpy(dir_tree, basedir);

	if (dir_tree[strlen(dir_tree) - 1] == DIR_SEP[0]) {
		dir_tree[strlen(dir_tree) - 1] = '\0';
	}

	CreateDirTreeHelper(directory_id, dir_tree);

	if (dir_tree[strlen(dir_tree) - 1] != DIR_SEP[0]) {
		strcat(dir_tree, DIR_SEP);
	}
}

int Iso9660::DumpBootSector(char* outfilename) {
	FILE* outfile;
	int i;

	if ((outfile = fopen(outfilename, "wb")) == NULL) {
		printf("Error creating bootsector file: %s\n", outfilename);

		return 1;
	}

	for (i = 0; i < 16; i++) {
		fseek(iso_file, i * SECTOR_SIZE, SEEK_SET);
		fread(lba_buffer, SECTOR_SIZE, 1, iso_file);
		fwrite(lba_buffer, SECTOR_SIZE, 1, outfile);
	}

	fclose(outfile);

	printf("First 16 sectors dumped as: %s\n", outfilename);

	return 0;
}

int WriteSortFileHelper(const void* a, const void* b) {
	return (((iso9660_file_info_t*)b)->lba - ((iso9660_file_info_t*)a)->lba);
}

int Iso9660::WriteSortFile(char* outfilename) {
	FILE* outfile;
	int i;

	if ((outfile = fopen(outfilename, "w")) == NULL) {
		printf("Error creating sortfile: %s\n", outfilename);

		return 1;
	}

	// Sort files_list array by LBA (DESC)
	qsort(files_list, files_list_size, sizeof(iso9660_file_info_t), WriteSortFileHelper);

	for (i = 0; i < files_list_size; i++) {
		fprintf(outfile, "%s %d\n", files_list[i].full_name, i + 1);
	}

	fclose(outfile);

	printf("Sortfile saved as: %s\n", outfilename);

	return 0;
}

void Iso9660::CloseISO(void) {
	if (iso_file != NULL)
	{
		fclose(iso_file);
		iso_file = NULL;
	}

	if (path_table != NULL)
	{
		free(path_table);
		path_table = NULL;
	}

	if (files_list != NULL)
	{
		free(files_list);
		files_list = NULL;
	}

	if (files_list_size > 0)
	{
		files_list_size = 0;
	}

	if (gdi_files_list != NULL)
	{
		free(gdi_files_list);
		gdi_files_list = NULL;
	}	
}
