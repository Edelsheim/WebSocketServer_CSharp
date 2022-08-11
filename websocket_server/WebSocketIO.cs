using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace WebSocketServer_CSharp.websocket_server
{
    public class WebSocketIO
    {
        public enum OPCODE : int
        {
            Unknown = -1,
            Connect = 0,
            Text = 1,
            Binary = 2,
            Close = 8,
            Ping = 9,
            Pong = 10
        };

        private const string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        public static byte[] Handshake(string s)
        {
            try
            {
                string swk = Regex.Match(s, "Sec-WebSocket-Key:(.*)").Groups[1].Value.Trim();
                string swka = swk + guid;
                byte[] swka_sha1 = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                string swka_sha1_base64 = Convert.ToBase64String(swka_sha1);

                string header = "HTTP/1.1 101 Switching Protocols\r\n";
                header += "Connection: Upgrade\r\n";
                header += "Upgrade: websocket\r\n";
                header += "Sec-WebSocket-Accept: " + swka_sha1_base64 + "\r\n\r\n";

                byte[] response = Encoding.UTF8.GetBytes(header);
                return response;
            }
            catch
            {
                throw;
            }
        }

        public static byte[] ParseReceive(byte[] bytes, out OPCODE opcode)
        {
            try
            {
                bool fin = (bytes[0] & 0b10000000) != 0;
                bool mask = (bytes[1] & 0b10000000) != 0;

                opcode = (OPCODE)(bytes[0] & 0b00001111);
                int offset = 2;
                int msg_len = bytes[1] & 0b01111111;

                if (opcode == OPCODE.Close)
                    return null;

                if (msg_len == 126)
                {
                    msg_len = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                    offset = 4;
                }
                else if (msg_len == 127)
                {
                    msg_len = BitConverter.ToUInt16(new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] }, 0);
                    offset = 10;
                }

                if (msg_len > 0 && mask)
                {
                    byte[] decoded = new byte[msg_len];
                    byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                    offset += 4;

                    for (int i = 0; i < msg_len; ++i)
                    {
                        decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);
                    }

                    return decoded;
                }

                return null;
            }
            catch
            {
                throw;
            }
        }

        public static byte[] ParseSend(string data, OPCODE opcode)
        {
            try
            {
                return ParseSend(Encoding.UTF8.GetBytes(data), opcode);
            }
            catch
            {
                throw;
            }
        }

        public static byte[] ParseSend(byte[] data, OPCODE opcode)
        {
            try
            {
                BitArray first_byte = new BitArray(new bool[]
                {
                    opcode == OPCODE.Text || opcode == OPCODE.Ping,
                    opcode == OPCODE.Binary || opcode == OPCODE.Pong,
                    false,
                    opcode == OPCODE.Close || opcode == OPCODE.Ping || opcode == OPCODE.Pong,
                    false, // RSV3
                    false, // RSV2
                    false, // RSV1
                    true, // Fin
                });

                byte[] send_bytes = null;
                if (data.Length < 126)
                {
                    send_bytes = new byte[data.Length + 2];
                    first_byte.CopyTo(send_bytes, 0);
                    send_bytes[1] = (byte)data.Length;
                    data.CopyTo(send_bytes, 2);
                }
                else
                {
                    if (data.Length >= ushort.MaxValue)
                    {
                        send_bytes = new byte[data.Length + 10];
                        first_byte.CopyTo(send_bytes, 0);
                        send_bytes[1] = 127;
                        byte[] length_bytes = BitConverter.GetBytes((long)data.Length);
                        for (int i = 7, j = 2; i >= 0; i--, j++)
                        {
                            send_bytes[j] = length_bytes[i];
                        }
                    }
                    else
                    {
                        send_bytes = new byte[data.Length + 4];
                        first_byte.CopyTo(send_bytes, 0);
                        send_bytes[1] = 126;
                        byte[] length_bytes = BitConverter.GetBytes(data.Length);
                        send_bytes[2] = length_bytes[1];
                        send_bytes[3] = length_bytes[0];
                        Array.Copy(data, 0, send_bytes, 4, data.Length);
                    }
                }
                return send_bytes;
            }
            catch
            {
                throw;
            }
        }

        public static byte[] CloseRequest(int code, string reason)
        {
            try
            {
                byte[] close_bytes = new byte[2 + reason.Length];
                BitConverter.GetBytes(code).CopyTo(close_bytes, 0);

                byte temp = close_bytes[0];
                close_bytes[0] = close_bytes[1];
                close_bytes[1] = temp;

                Encoding.UTF8.GetBytes(reason).CopyTo(close_bytes, 2);
                return close_bytes;
            }
            catch
            {
                throw;
            }
        }
    }
}
