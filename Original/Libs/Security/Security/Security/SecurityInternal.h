#pragma once
class SecurityInternal
{
public:
	SecurityInternal(void);
	~SecurityInternal(void);
	
	static wchar_t* Encrypt(wchar_t* source);
	static wchar_t* Decrypt(wchar_t* source);
	
	static void EncryptBytes(unsigned char*  s, int len);
	static void DecryptBytes(unsigned char*  s, int len);

};

