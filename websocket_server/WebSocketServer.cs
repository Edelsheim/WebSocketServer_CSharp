using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace WebSocketServer_CSharp.websocket_server
{
    internal class WebSocketServer
    {
        private object lockObj = new object();

        private bool acceptRun = false;
        private TcpListener tcpListener;

        public WebSocketServer(in int port)
        {
            try
            {
                this.tcpListener = new TcpListener(System.Net.IPAddress.Any, port);
            }
            catch
            {
                throw;
            }
        }

        public void Run()
        {
            try
            {
                lock (lockObj)
                {
                    if (this.acceptRun)
                        return;

                    this.acceptRun = true;
                }

                this.tcpListener.Start();
                this.Accept();

            }
            catch
            {
                throw;
            }
        }

        public void Close()
        {
            try
            {
                lock (lockObj)
                {
                    this.acceptRun = false;

                    this.tcpListener.Stop();
                }
            }
            catch
            {
                throw;
            }
        }

        private void Accept()
        {
            while (true)
            {
                try
                {
                    lock (lockObj)
                    {
                        if (!this.acceptRun)
                            break;
                    }

                    TcpClient client = this.tcpListener.AcceptTcpClient();

                    NetworkStream stream;

                    using (stream = client.GetStream())
                    {
                        while (true)
                        {
                            while (!stream.DataAvailable) ;
                            while (client.Available < 3) ;

                            // read message
                            byte[] bytes = new byte[client.Available];
                            stream.Read(bytes, 0, client.Available);

                            string s = Encoding.UTF8.GetString(bytes);
                            if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
                            {
                                byte[] response = WebSocketIO.Handshake(s);
                                if (response != null)
                                    stream.Write(response, 0, response.Length);
                            }
                            else
                            {
                                WebSocketIO.OPCODE opcode = WebSocketIO.OPCODE.Unknown;
                                byte[] data = WebSocketIO.ParseReceive(bytes, out opcode);

                                // close message
                                if (opcode == WebSocketIO.OPCODE.Close)
                                {
                                    byte[] close_byte = WebSocketIO.CloseRequest(1000, "Close");
                                    byte[] send = WebSocketIO.ParseSend(close_byte, opcode);
                                    stream.Write(send, 0, send.Length);
                                    break;
                                }

                                // receive message
                                if (data != null && data.Length >= 1)
                                {
                                    string receive = Encoding.UTF8.GetString(data);

                                    /*****************************************************
                                                        Do Something
                                    *****************************************************/

                                    byte[] send = WebSocketIO.ParseSend(receive, opcode);
                                    if (send != null && send.Length >= 1)
                                        stream.Write(send, 0, send.Length);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    throw;
                }
            }
        }
    }
}
