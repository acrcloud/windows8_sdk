#include "pch.h"
#include "ACRCloudExtrTool.h"
#include "ACRCloudExtrDll.h"

#include <iostream>
using namespace std;

using namespace ACRCloudExtrDllCOM;
using namespace Platform;

ACRCloudExtrTool::ACRCloudExtrTool()
{
}

void ACRCloudExtrTool::CreateFingerprint(const Array<unsigned char>^ pcmBuffer, Array<unsigned char>^* fp)
{
	unsigned char* buffer = pcmBuffer->Data;
	int bufferLen = pcmBuffer->Length;
	char* fpBuffer = NULL;
	int fpBufferLen = 0;

	if (buffer == NULL || bufferLen <= 0) return;

	fpBufferLen = native_create_fingerprint((char*)buffer, bufferLen, &fpBuffer);
	if (fpBufferLen > 0) {
		*fp = ref new Array<unsigned char>((unsigned char*)fpBuffer, fpBufferLen);
		native_free(fpBuffer);
	}
}
