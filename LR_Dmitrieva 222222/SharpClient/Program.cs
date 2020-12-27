using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace sharp
{
    class Program
    {
        static Encoding cp866 = Encoding.GetEncoding("CP866");
        static int clientId = 0;

        enum Messages
        {
            M_INIT = 0,
            M_EXIT = 1,
            M_GETDATA = 2,
            M_NODATA = 3,
            M_DATA = 4,
            M_CONFIRM = 5
        };

        enum Members
        {
            M_BROKER = 0,
            M_ALL = 10,
            M_USER = 100
        };

        public struct MsgHeader
        {
            public int m_To;
            public int m_From;
            public int m_Type;
            public int m_Size;
            public MsgHeader(int m_To, int m_From, int m_Type, int m_Size)
            {
                this.m_To = m_To;
                this.m_From = m_From;
                this.m_Type = m_Type;
                this.m_Size = m_Size;
            }

            public void Send(ref Socket s)
            {
                s.Send(BitConverter.GetBytes(this.m_To), sizeof(int), SocketFlags.None);
                s.Send(BitConverter.GetBytes(this.m_From), sizeof(int), SocketFlags.None);
                s.Send(BitConverter.GetBytes(this.m_Type), sizeof(int), SocketFlags.None);
                s.Send(BitConverter.GetBytes(this.m_Size), sizeof(int), SocketFlags.None);
            }
        };

        public class Message
        {
            public MsgHeader m_Header;
            public string m_Data;
            public Message(int To, int From, int Type = (int)Messages.M_DATA, string str = "")
            {
                m_Header = new MsgHeader(To, From, Type, str.Length);

                m_Data = str;
            }

            public void Send(ref Socket s)
            {
                m_Header.Send(ref s);
                if (m_Header.m_Size != 0)
                {
                    s.Send(cp866.GetBytes(m_Data), m_Data.Length, SocketFlags.None);
                }
            }

            public int GetInt(Socket s)
            {
                byte[] b = new byte[sizeof(int)];
                s.Receive(b, sizeof(int), SocketFlags.None);
                return BitConverter.ToInt32(b, 0);
            }

            public int Receive(ref Socket s)
            {
                m_Header.m_To = GetInt(s);
                m_Header.m_From = GetInt(s);
                m_Header.m_Type = GetInt(s);
                m_Header.m_Size = GetInt(s);
                if (m_Header.m_Size != 0)
                {
                    byte[] b = new byte[m_Header.m_Size];
                    s.Receive(b, m_Header.m_Size, SocketFlags.None);
                    m_Data = cp866.GetString(b, 0, m_Header.m_Size);
                }
                return m_Header.m_Type;
            }

            public static Message Send(int To, int Type, string Data = "")
            {
                int nPort = 12345;
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), nPort);
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s.Connect(endPoint);
                Message m = new Message(To, clientId, Type, Data);
                if (s.Connected)
                {
                    m.Send(ref s);
                    m.Receive(ref s);
                }
                if (m.m_Header.m_Type == (int)Messages.M_INIT && clientId == 0)
                {
                    clientId = m.m_Header.m_To;
                }
                return m;
            }
        };

        public static void ReceiveMsg()
        {
            for (; ; )
            {
                Message msg = Message.Send((int)Members.M_BROKER, (int)Messages.M_GETDATA);
                if (msg.m_Header.m_Type == (int)Messages.M_DATA)
                    Console.WriteLine(msg.m_Data);
            }
        }


        static void Main(string[] args)
        {

            Message m = Message.Send((int)Members.M_BROKER, (int)Messages.M_INIT);
            Thread t = new Thread(() => ReceiveMsg());
            t.Start();
            int check1;
            string str;
            for (; ; )
            {
                Console.WriteLine("Send message - 0; Exit - 1: ");
                str = Console.ReadLine();
                check1 = Convert.ToInt32(str);
                switch (check1)
                {
                    case 0:
                        {
                            Console.WriteLine("Send to: ");
                            str = Console.ReadLine();
                            m.m_Header.m_To = Convert.ToInt32(str);
                            Console.WriteLine("Message: ");
                            m.m_Data = Console.ReadLine();
                            Message.Send(m.m_Header.m_To, (int)Messages.M_DATA, m.m_Data);
                            break;
                        }
                    case 1:
                        {
                            Message.Send((int)Members.M_BROKER, (int)Messages.M_EXIT);
                            return;
                        }
                }
            }
        }
    }
}
