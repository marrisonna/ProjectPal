#include "StdAfx.h"
#include "SecurityInternal.h"

#include "string.h"


SecurityInternal::SecurityInternal(void)
{
}


SecurityInternal::~SecurityInternal(void)
{
}


wchar_t* SecurityInternal::Encrypt(wchar_t* source)
{
	int length = wcslen(source);
	wchar_t* result = new wchar_t[length+1];

	int lastChar = 13;

	for(int i = 0; i < length; i++)
	{
		int c= source[i];
		if(c>=32 && c < 127)
		{
			int newLastChar = c;
			c += lastChar;
			int d = (c-32);
			d = d % 95;
			c = d +32 ;
			lastChar = newLastChar;
		}
		result[i] = c;
	}
	result[length]=0;
	return result;
}





wchar_t* SecurityInternal::Decrypt(wchar_t* source)
{
	int length = wcslen(source);
	wchar_t* result = new wchar_t[length+1];

	int lastChar = 13;

	for(int i = 0; i < length; i++)
	{
		wchar_t c= source[i];
		if(c>=32 && c < 127)
		{
			int d = c-32- lastChar;
			d = d % 95;
			if(d<0)
				d+=95;
			c = d +32;
			lastChar=c;
		}
		result[i] = c;
	}
	result[length]=0;
	return result;
}

void SecurityInternal::EncryptBytes(unsigned char*  s, int len)
{
	unsigned char lastChar = 13;

	for(int i=0; i < len; i++)
	{
		unsigned char c = s[i];
		unsigned char newLastChar = c;

		unsigned char a = (unsigned char)i;

		c = (c ^ lastChar) ^ a;

		s[i]=c;

		lastChar = newLastChar;

	}

	return;
}


void SecurityInternal::DecryptBytes(unsigned char*  s, int len)
{
	unsigned char lastChar = 13;

	for(int i=0; i < len; i++)
	{
		unsigned char c = s[i];

		unsigned char a = (unsigned char)i;

		c = (c ^ lastChar) ^ a;

		s[i]=c;

		lastChar = c;

	}

	return;
}