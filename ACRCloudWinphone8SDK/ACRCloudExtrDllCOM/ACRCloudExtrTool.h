#pragma once
#include "pch.h"
#include <windows.h>
using namespace Platform;

namespace ACRCloudExtrDllCOM
{
	public ref class ACRCloudExtrTool sealed
	{
	public:
		ACRCloudExtrTool();

		void CreateFingerprint(const Array<unsigned char>^ pcmBuffer, Array<unsigned char>^* fp);
	};
}
