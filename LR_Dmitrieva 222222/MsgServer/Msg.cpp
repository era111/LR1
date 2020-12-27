#include "pch.h"
#include "Msg.h"

int Message::m_ClientID = 0;

void Message::Send(CSocket& s, unsigned int To, unsigned int From, unsigned int Type, const string& Data)
{
	Message m(To, From, Type, Data);
	m.Send(s);
}

Message Message::Send(unsigned int To, unsigned int Type, const string& Data)
{
	CSocket s;
	s.Create();
	if (!s.Connect("127.0.0.1", 12345))
	{
		DWORD dwError = GetLastError();
		throw runtime_error("Connection error");
	}
	Message m(To, m_ClientID, Type, Data);
	m.Send(s);
	m.Receive(s);
	if (m.m_Header.m_Type == M_INIT)
	{
		m_ClientID = m.m_Header.m_To;
	}
	return m;
}