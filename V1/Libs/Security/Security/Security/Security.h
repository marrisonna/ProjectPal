// Security.h

#pragma once

using namespace System;

namespace Security {

	public ref class Functions
	{
	public:
		static String^ Encrypt(String^ s);
		static String^ Decrypt(String^ s);
		static void  EncryptBytes(array<System::Byte>^  s);
		static void  DecryptBytes(array<System::Byte>^  s);
	};
}
