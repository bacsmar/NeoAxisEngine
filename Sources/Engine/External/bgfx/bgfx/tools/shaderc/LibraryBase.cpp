// Copyright (C) NeoAxis Group Ltd. 8 Copthall, Roseau Valley, 00152 Commonwealth of Dominica.
#include "LibraryBase.h"
#include <bx/debug.h>

#ifdef __ANDROID__
#include <codecvt>
#include <android/log.h>
#endif

///////////////////////////////////////////////////////////////////////////////////////////////////

std::string ConvertStringToUTF8(const std::wstring& str)
{
	std::string result;
	if(!str.empty())
	{

#ifdef _WIN32
		int size = WideCharToMultiByte(CP_UTF8, 0, str.c_str(), -1, NULL, 0, NULL, NULL);
		if(size != 0)
		{
			char* aString = (char*)_alloca(size * sizeof(char));
			//char* aString = (char*)malloc(size * sizeof(char));
			if(WideCharToMultiByte(CP_UTF8, 0, str.c_str(), -1, aString, size, NULL, NULL) != 0)
				result = aString;
		}
#elif defined(__ANDROID__)

		std::wstring_convert<std::codecvt_utf8<wchar_t>> myconv;
		result = myconv.to_bytes(str);

		//// can't use ConvertString/iconv because __LP64__ is defined somewhere..
		//// https://stackoverflow.com/questions/4804298/how-to-convert-wstring-into-string
		//using convert_type = std::codecvt_utf8<wchar_t>;
		//std::wstring_convert<convert_type, wchar_t> converter;
		////use converter (.to_bytes: wstr->str, .from_bytes: str->wstr)
		//result = converter.to_bytes(str);
#else
		static iconv_t cd = (iconv_t)-1;
		if (cd == (iconv_t)-1)
		{
			cd = iconv_open("UTF-8", "UTF-32LE");
			if (cd == (iconv_t)-1)
				Fatal("iconv_open failed.");
		}
		ConvertString(cd, str, &result, '?');
#endif

	}
	return result;
}

std::wstring ConvertStringToUTFWide(const std::string& str)
{
	std::wstring result;
	if(!str.empty())
	{

#ifdef _WIN32
		int size = MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, NULL, 0);
		if(size)
		{
			wchar_t* wString = (wchar_t*)_alloca(size * sizeof(wchar_t));
			if(MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, wString, size) != 0)
				result = wString;
		}
#elif defined(__ANDROID__)

		std::wstring_convert<std::codecvt_utf8<wchar_t>> myconv;
		result = myconv.from_bytes(str);

		//using convert_type = std::codecvt_utf8<wchar_t>;
		//std::wstring_convert<convert_type, wchar_t> converter;
		////use converter (.to_bytes: wstr->str, .from_bytes: str->wstr)
		//result = converter.from_bytes(str);
#else
		static iconv_t cd = (iconv_t)-1;
		if(cd == (iconv_t)-1)
		{
			cd = iconv_open("UTF-32LE", "UTF-8");
			if (cd == (iconv_t)(-1))
				Fatal("iconv_open failed.");
		}
		ConvertString(cd, str, &result, '?');
#endif

	}
	return result;
}

///////////////////////////////////////////////////////////////////////////////////////////////////

wchar16* CreateOutString(const std::wstring& str)
{
#ifdef _WIN32
	wchar16* result = new wchar_t[str.length() + 1];
	wcscpy(result, str.c_str());
	return result;
#else
	int len = str.length();
	wchar16* result = new wchar16[len + 1];
	for (int n = 0; n < len; n++)
		result[n] = (wchar16)str[n];
	result[len] = 0;
	return result;
#endif
}

EXPORT void FreeOutString(wchar16* pointer)
{
	delete[] pointer;
}

///////////////////////////////////////////////////////////////////////////////////////////////////

void Fatal(const char* text)
{
#if BX_PLATFORM_OSX
	CFStringRef textRef = CFStringCreateWithCString(NULL, text, kCFStringEncodingUTF8);
	CFUserNotificationDisplayAlert(0, kCFUserNotificationStopAlertLevel, NULL, NULL, NULL,
		CFSTR("Fatal"), textRef, CFSTR("OK"), NULL, NULL, NULL);
	CFRelease(textRef);
#elif BX_PLATFORM_WINRT || BX_PLATFORM_XBOXONE
	bx::debugOutput(text);
	bx::debugBreak();
#elif defined(__ANDROID__)
	//!!!!!!dr
	char tempBuffer[4096];
	sprintf(tempBuffer, "ShaderC: Fatal: %s\n", text);
	__android_log_write(ANDROID_LOG_ERROR, "NeoAxis Engine", tempBuffer);
#elif BX_PLATFORM_WINDOWS
	MessageBoxA(NULL, text, "Fatal", MB_OK | MB_ICONEXCLAMATION);
#endif
	exit(0);
}

///////////////////////////////////////////////////////////////////////////////////////////////////

//!!!!unicode
std::string string_format(const std::string fmt_str, ...)
{
	int final_n, n = ((int)fmt_str.size()) * 2; /* Reserve two times as much as the length of the fmt_str */
	std::unique_ptr<char[]> formatted;
	va_list ap;
	while (1)
	{
		formatted.reset(new char[n]); /* Wrap the plain char array into the unique_ptr */
		strcpy(&formatted[0], fmt_str.c_str());
		va_start(ap, fmt_str);
		final_n = vsnprintf(&formatted[0], n, fmt_str.c_str(), ap);
		va_end(ap);
		if (final_n < 0 || final_n >= n)
			n += abs(final_n - n + 1);
		else
			break;
	}
	return std::string(formatted.get());
}
