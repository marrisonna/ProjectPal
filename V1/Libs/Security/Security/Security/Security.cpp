// This is the main DLL file.

#include "stdafx.h"

#include "Security.h"

#include <string.h>
#include <stdlib.h>
#include <stdio.h>
#include <errno.h>
#include <Vcclr.h>


using namespace System;

#include "SecurityInternal.h"

namespace Security
{

		String^ Functions::Encrypt(System::String^ s)
		{

			pin_ptr<const wchar_t> wch = PtrToStringChars(s);


			int len = s->Length;
			wchar_t* wcstring = new wchar_t[len+1];

			wcscpy_s(wcstring, len+1,wch);

			wchar_t* newString = SecurityInternal::Encrypt(wcstring);

			System::String ^result = gcnew String(newString);
    
			return result;
		}

		String^ Functions::Decrypt(System::String^ s)
		{

			pin_ptr<const wchar_t> wch = PtrToStringChars(s);


			int len = s->Length;
			wchar_t* wcstring = new wchar_t[len+1];

			wcscpy_s(wcstring, len+1,wch);

			wchar_t* newString = SecurityInternal::Decrypt(wcstring);

			System::String ^result = gcnew String(newString);
    
			return result;
		}




		void  Functions::EncryptBytes(array<System::Byte>^  s)
		{
			pin_ptr<unsigned char> pUnmanagedArr = &s[0];

			int l = s->Length;

			unsigned char* q = pUnmanagedArr;

			SecurityInternal::EncryptBytes(q,l);

		}

		void  Functions::DecryptBytes(array<System::Byte>^  s)
		{
			pin_ptr<unsigned char> pUnmanagedArr = &s[0];

			int l = s->Length;

			unsigned char* q = pUnmanagedArr;

			SecurityInternal::DecryptBytes(q,l);
		}
}