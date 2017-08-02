namespace Telnet
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    [ComVisible(true), Guid("2E07EC16-B376-4664-9460-F134AB9D5BE6"), ProgId("Telnet.Connector"), ClassInterface(ClassInterfaceType.None)]
    public class Connector : IConnector
    {
        private byte[] brec = new byte[2048];  //0x800
        private byte[] bsent = new byte[2048];  //0x800
        private string errortext;
        private bool imakelog;
        private int itimeout = 2000;  // 0x7d0
        private string received;
        private Socket sock;
        private string tosend;

        public int communicate()
        {
            int num = 1;
            string s = this.Encode(this.tosend);
            this.received = "";
            int size = Encoding.ASCII.GetBytes(s, 0, s.Length, this.bsent, 0);
            try
            {
                this.sock.SendTimeout = this.itimeout;
                this.sock.ReceiveTimeout = this.itimeout;
                if (size != 0)
                {
                    this.WriteToLog("Before sent");
                    if (this.sock.Send(this.bsent, size, SocketFlags.None) != size)
                    {
                        throw new SocketException(10054); // 0x2746
                    }
                    this.WriteToLog("Successfully sent: " + this.tosend + " // " + s);
                }
                this.tosend = "";
                num = 2;

                string text = GetTextMessage();
                
                if (text.Length <= 0)
                {
                    throw new Exception("Invalid packet or empty packet received");
                }
                
                this.received = text;
                num = 0;
                this.errortext = "";
            }
            catch (Exception exception)
            {
                this.WriteToLog("DateTime:" + DateTime.Now.ToString("HH:mm:ss"));
                this.WriteToLog("StackTrace:" + exception.StackTrace);
                this.WriteToLog("Message:" + exception.Message);
                if (exception.InnerException != null)
                {
                    this.WriteToLog("InnerException:" + exception.InnerException.ToString());
                }
                this.WriteToLog("----------------------------------------------");
                this.errortext = exception.Message;
            }
            return num;
        }

        private string GetTextMessage()
        {
            string text = "";
            int count = 0;
            int size;
            string str4 = "Username:";
            string subtext = "Password:";
            string str6 = "--More--";


            bool repeatRequest = true;

            while (repeatRequest)
            {
                count = this.sock.Receive(this.brec, this.brec.Length, SocketFlags.None);
                repeatRequest = false;

                if (count != 0)
                {
                    this.WriteToLog("Received " + count.ToString() + " bytes");
                    text = text + Encoding.ASCII.GetString(this.brec, 0, count);
                    this.WriteToLog("Out" + text);
                    char ch = text[text.Length - 1];
                    if (((ch.ToString() != "#") && !text.Contains(str4)) && !this.StringsEqualFromRight(text, subtext))
                    {
                        if (this.StringsEqualFromRight(text, str6))
                        {
                            string str7 = this.Encode(" ");
                            size = Encoding.ASCII.GetBytes(str7, 0, str7.Length, this.bsent, 0);
                            this.sock.Send(this.bsent, size, SocketFlags.None);
                        }
                        repeatRequest = true;
                    }
                }
            }

            return text;
        }
            

        public int connect(string ipaddr, int portnr)
        {
            int num = 1;
            try
            {
                this.disconnect();
                IPAddress[] hostAddresses = Dns.GetHostAddresses(ipaddr);
                IPEndPoint remoteEP = null;
                foreach (IPAddress address in hostAddresses)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        remoteEP = new IPEndPoint(address, portnr);
                        break;
                    }
                }
                if (remoteEP == null)
                {
                    throw new Exception("This host has no IPV4 address");
                }
                this.sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.sock.Blocking = true;
                ManualResetEvent state = new ManualResetEvent(false);
                IAsyncResult asyncResult = this.sock.BeginConnect(remoteEP, delegate (IAsyncResult ar) {
                    ((ManualResetEvent) ar.AsyncState).Set();
                }, state);
                if (!state.WaitOne(this.itimeout))
                {
                    throw new SocketException(10060); // 0x274c
                }
                this.sock.EndConnect(asyncResult);
                this.errortext = "";
                num = 0;
            }
            catch (SocketException exception)
            {
                this.errortext = exception.Message;
            }
            catch (Exception exception2)
            {
                this.errortext = exception2.Message;
            }
            return num;
        }

        public int disconnect()
        {
            this.tosend = "";
            this.received = "";
            this.errortext = "";
            try
            {
                if (this.sock != null)
                {
                    if (this.sock.Connected)
                    {
                        this.sock.Shutdown(SocketShutdown.Both);
                    }
                    this.sock.Close();
                    this.sock = null;
                }
            }
            catch
            {
            }
            this.errortext = "";
            return 0;
        }

        private string Encode(string text)
        {
            string str = "";
            foreach (char ch in text)
            {
                int num = Convert.ToInt32(ch);
                if (num > 127) // 0x7f
                {
                    str = str + string.Format("&#{0};", num);
                }
                else
                {
                    str = str + ch;
                }
            }
            return str;
        }

        private void ProcWaitForLink(object o)
        {
        }

        private bool StringsEqualFromRight(string text, string subtext)
        {
            return ((text.Length >= subtext.Length) && (text.Substring(text.Length - subtext.Length) == subtext));
        }

        private void WriteToLog(string sdata)
        {
            if (this.imakelog)
            {
                string location = Assembly.GetExecutingAssembly().Location;
                using (StreamWriter writer = new StreamWriter(location.Substring(0, location.Length - 3) + "log", true))
                {
                    writer.WriteLine(sdata);
                    writer.Close();
                }
            }
        }

        public string buffer
        {
            get
            {
                return this.received;
            }
            set
            {
                this.tosend = value;
            }
        }

        public string lasterrortext
        {
            get
            {
                return this.errortext;
            }
        }

        public bool makelog
        {
            get
            {
                return this.imakelog;
            }
            set
            {
                this.imakelog = value;
            }
        }

        public int timeout
        {
            get
            {
                return this.itimeout;
            }
            set
            {
                this.itimeout = value;
            }
        }
    }
}

