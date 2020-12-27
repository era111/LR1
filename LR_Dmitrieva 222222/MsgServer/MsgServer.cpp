// MsgServer.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include "pch.h"
#include "framework.h"
#include "MsgServer.h"
#include "Msg.h"
#include "Session.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

int gMaxID = M_USER;
map<int, shared_ptr<Session>> gSessions;

void ProcessClient(SOCKET hSock)
{
    CSocket s;
    s.Attach(hSock);
    Message m;
    int nCode = m.Receive(s);
    switch (nCode)
    {
    case M_INIT:
    {
        auto pSession = make_shared<Session>(++gMaxID, m.m_Data);
        gSessions[pSession->m_ID] = pSession;
        Message::Send(s, pSession->m_ID, M_BROKER, M_INIT);
        cout << pSession->m_ID << endl;
        break;
    }
    case M_EXIT:
    {
        cout << m.m_Header.m_From << endl;
        gSessions.erase(m.m_Header.m_From);
        Message::Send(s, m.m_Header.m_From, M_BROKER, M_CONFIRM);
        return;
    }
    case M_GETDATA:
    {
        if (gSessions.find(m.m_Header.m_From) != gSessions.end())
        {
            gSessions[m.m_Header.m_From]->time = std::chrono::system_clock::now();
            gSessions[m.m_Header.m_From]->Send(s);
        }
        break;
    }
    default:
    {
        if (gSessions.find(m.m_Header.m_From) != gSessions.end())
        {
            gSessions[m.m_Header.m_From]->time = std::chrono::system_clock::now();

            if (gSessions.find(m.m_Header.m_To) != gSessions.end())
            {
                gSessions[m.m_Header.m_To]->Add(m);
            }
            else if (m.m_Header.m_To == M_ALL)
            {
                for (auto& [id, Session] : gSessions)
                {
                    if (id != m.m_Header.m_From)
                        Session->Add(m);
                }
            }
        }
        break;
    }
    }
}

void Timer()
{
    while (true)
    {
        auto time = std::chrono::system_clock::now();
        for (map<int, shared_ptr<Session>>::iterator it = gSessions.begin(); it != gSessions.end();) {
            if (std::chrono::duration_cast<std::chrono::milliseconds>(time - it->second->time).count() > 100000) {
                std::cout << "client " << it->second->m_ID << " out by time" << endl;
                it = gSessions.erase(it);
            }
            else {
                it++;
            }
        }
    }
}


void Server()
{
    AfxSocketInit();

    CSocket Server;
    Server.Create(12345);
    thread t1(Timer);
    t1.detach();

    while (true)
    {
        if (!Server.Listen())
            break;
        CSocket s;
        Server.Accept(s);
        thread t(ProcessClient, s.Detach());
        t.detach();
    }
}


// The one and only application object

CWinApp theApp;

using namespace std;

int main()
{
    int nRetCode = 0;

    HMODULE hModule = ::GetModuleHandle(nullptr);

    if (hModule != nullptr)
    {
        // initialize MFC and print and error on failure
        if (!AfxWinInit(hModule, nullptr, ::GetCommandLine(), 0))
        {
            // TODO: code your application's behavior here.
            wprintf(L"Fatal Error: MFC initialization failed\n");
            nRetCode = 1;
        }
        else
        {
            Server();
        }
    }
    else
    {
        // TODO: change error code to suit your needs
        wprintf(L"Fatal Error: GetModuleHandle failed\n");
        nRetCode = 1;
    }

    return nRetCode;
}
