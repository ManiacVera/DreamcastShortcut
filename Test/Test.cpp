// Test.cpp : Este archivo contiene la función "main". La ejecución del programa comienza y termina ahí.
//

#include <iostream>
#include "iso9660.h"

int main(int argc, char* argv[])
{
	Iso9660* iso9660 = new Iso9660(argv[3]);

	//iso9660->OpenGDI((char *)"C:\\JDownloader\\Dreamcast\\RetroDream V3\\GAMES\\UNREAL TOURNAMENT PAL 60HZ\\disc.gdi", 0);
	//int cddaSize = iso9660->GetCDDASize();
	//iso9660->CreateDummyFile("C:\\JDownloader\\Dreamcast\\RetroDream V3\\GAMES\\UNREAL TOURNAMENT PAL 60HZ\\dummy.iso", cddaSize);

	//const char* files[] = { 
	//	"C:\\JDownloader\\Dreamcast\\RetroDream V3\\GAMES\\UNREAL TOURNAMENT PAL 60HZ\\track03.iso",
	//	"C:\\JDownloader\\Dreamcast\\RetroDream V3\\GAMES\\UNREAL TOURNAMENT PAL 60HZ\\dummy.iso",
	//	"C:\\JDownloader\\Dreamcast\\RetroDream V3\\GAMES\\UNREAL TOURNAMENT PAL 60HZ\\track23.iso",
	//	NULL
	//};
	//
	//iso9660->MergeFiles("C:\\JDownloader\\Dreamcast\\RetroDream V3\\GAMES\\UNREAL TOURNAMENT PAL 60HZ\\full_iso.iso", (char **)files);

	//extract <in_file> <lba_offset> <out_dir> <out_boot_file> <out_sortfile>
	int i, j;

	printf("extract v1.0\nA simple ISO extract tool by bootsector\n(c) 05/13 bootsector@ig.com.br\n\n");

	if (argc != 6) {
		printf("Usage:\nextract <in_file> <lba_offset> <out_dir> <out_boot_file> <out_sortfile>\n\n");
		return 1;
	}

	if (iso9660->OpenISO(argv[1], atol(argv[2]))) {
		return 1;
	}

	//i = iso9660->NumDirs();

	//// Create full directory tree under <output_dir>
	//for (j = 1; j <= i; j++) {
	//	iso9660->CreateDirTree(j, argv[3]);
	//}

	//// Extract files
	//for (j = 1; j <= i; j++) {
	//	iso9660->ExtractFiles(j, argv[3]);
	//}

	char* pvr = NULL;
	char* stBin = NULL;

	iso9660->ReadFile("0GDTEX.PVR", pvr);
	iso9660->ReadFile("1ST_READ.BIN", stBin);

	iso9660->ExtractFiles();

	// Dump bootsector as "IP.BIN" in the local dir
	iso9660->DumpBootSector(argv[4]);

	// Write sortfile
	iso9660->WriteSortFile(argv[5]);

	iso9660->CloseISO();

	delete iso9660;
}