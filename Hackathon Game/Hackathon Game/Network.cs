using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Hackathon_Game
{
    public static class Network
    {
        public static Socket Socket = null;
        public static IPEndPoint EndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2222);
        public static EndPoint ePoint = (EndPoint)EndPoint;
    }

    enum OPCode
    {
        CREATE,
        JOIN,
        LOBBY,
        SEARCH,
        START,
        DISCONNECT,
        SHOOT,
        DIE,
        MOVE
    };

    class Packet
    {
        public OPCode OP;
        public string Data;
        public Vector2 Position;

        public Packet()
        {
            Data = string.Empty;
        }

        public Packet(string data)
        {
            string[] split = data.Split(':');
            Enum.TryParse(split[0], out OP);

            for (int i = 0; i < split[1].Length; i++)
            {
                char c;
                char.TryParse(split[1][i].ToString(), out c);
                if (c != '\0')
                {
                    Data += c;
                }
                else
                {
                    break;
                }
            }

            if (OP == OPCode.MOVE)
	        {
		        string[] pos = Data.Split(',');
                Position = new Vector2(Convert.ToInt32(pos[0]), Convert.ToInt32(pos[1]));
	        }
        }

        public Packet(OPCode op)
        {
            OP = op;
        }

        public Packet(OPCode op, string data)
        {
            OP = op;
            Data = data;
        }

        public int Length
        {
            get { return ToBytes().Length; }
        }

        public byte[] ToBytes()
        {
            return Encoding.ASCII.GetBytes(OP + ":" + Data); 
        }
    }
}
