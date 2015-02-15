using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace hackathon_simple_server
{
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

    class Program
    {
        public static byte[] receiveData = new byte[1024];
        public static Socket serverSocket;

        public static Dictionary<string, Lobby> lobbies = new Dictionary<string, Lobby>();
        public static Dictionary<Player, string> History = new Dictionary<Player, string>();
        static void Main(string[] args)
        {
            int port = 2222;
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);


            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serverSocket.Bind(endPoint);

            // Display some information
            Console.WriteLine("Starting Udp receiving on port: " + port);
            Console.WriteLine("Press any key to quit.");
            Console.WriteLine("-------------------------------\n");

            //Waits for client to connect
            IPEndPoint clients = new IPEndPoint(IPAddress.Any, 0);
            //Stores client
            EndPoint epSender = (EndPoint)clients;

            //Start listening for incoming data.
            serverSocket.BeginReceiveFrom(receiveData, 0, receiveData.Length, SocketFlags.None, ref epSender, new AsyncCallback(ReceiveData), epSender);

            // Wait for any key to terminate application
            Console.ReadKey();
        }

        private static void ReceiveData(IAsyncResult asyncResult)
        {
            try
            {
                byte[] data;

                // Initialise a packet object to store the received data
                Packet packet = new Packet(Encoding.ASCII.GetString(receiveData));

                Console.WriteLine(packet.OP + ":" + packet.Data);

                // Initialise the IPEndPoint for the clients
                IPEndPoint clients = new IPEndPoint(IPAddress.Any, 0);

                // Initialise the EndPoint for the clients
                EndPoint epSender = (EndPoint)clients;

                // Receive all data
                serverSocket.EndReceiveFrom(asyncResult, ref epSender);

                bool add = true;
                switch (packet.OP)
                {
                    //CREATE:lobbyName
                    case OPCode.CREATE:

                        //Search to see if a lobby with the name already exists
                        if (packet.Data != null && !lobbies.ContainsKey(packet.Data))
                        {
                            lobbies.Add(packet.Data, new Lobby(packet.Data));
                            Console.WriteLine(packet.Data + " lobby was created.");
                        }
                        break;
                    
                    //JOIN:lobbyName,playerName
                    case OPCode.JOIN:
                        string lobbyName = packet.Data.Split(',')[0], playerName = packet.Data.Split(',')[1];

                        Player player = new Player(epSender, playerName, new Vector2(0, 0));
                        //Search to see if an endpoint client already exists
                        foreach (Player p in lobbies[lobbyName].players)
                        {
                            if (p.endPoint.ToString() == player.endPoint.ToString())
                            {
                                add = false;
                                break;
                            }
                        }
                        if (add)
                        {
                            lobbies[lobbyName].players.Add(player);
                            Console.WriteLine(playerName + " was added to the list of players.");
                        }

                        Packet lobbyInfo = new Packet();
                        lobbyInfo.OP = OPCode.LOBBY;
                        foreach (Player p in lobbies[lobbyName].players)
                        {
                            lobbyInfo.Data += p.Name + ",";
                        }
                        //Clip ending comma
                        lobbyInfo.Data = lobbyInfo.Data.Substring(0, lobbyInfo.Data.Length - 1);

                        Console.WriteLine("Sending list of players in lobby");
                        foreach (Player p in lobbies[lobbyName].players)
                        {
                            serverSocket.BeginSendTo(lobbyInfo.ToBytes(), 0, lobbyInfo.Length, SocketFlags.None, p.endPoint, new AsyncCallback(SendData), null);
                        }
                        break;

                    case OPCode.SEARCH:
                        Packet info = new Packet(OPCode.LOBBY);
                        foreach (string l in lobbies.Keys)
                        {
                            info.Data += l + ",";
                        }
                        //Clip ending comma
                        info.Data = info.Data.Substring(0, info.Data.Length - 1);
                        //Send client a list of current lobbies
                        serverSocket.BeginSendTo(info.ToBytes(), 0, info.Length, SocketFlags.None, epSender, new AsyncCallback(SendData), epSender);

                        break;

                    case OPCode.START:
                        packet.OP = OPCode.START;
                        foreach (Player p in lobbies[packet.Data].players)
                        {
                            if (p.endPoint.ToString() != epSender.ToString())
                            {
                               serverSocket.BeginSendTo(packet.ToBytes(), 0, packet.Length, SocketFlags.None, p.endPoint, new AsyncCallback(SendData), null);
                            }
                        }
                        break;

                    case OPCode.DISCONNECT:
                        // Remove current client from list
                        //foreach (Player p in Lobby.players)
                        //{
                        //    if (p.endPoint.Equals(epSender))
                        //    {
                        //        Console.WriteLine(p.Name + " has disconnected...");
                        //        Lobby.players.Remove(p);
                        //        break;
                        //    }
                        //}

                        //foreach (Player p in Lobby.RedTeam)
                        //{
                        //    if (p.endPoint.Equals(epSender))
                        //    {
                        //        Console.WriteLine(p.Name + " has disconnected...");
                        //        Lobby.RedTeam.Remove(p);
                        //        break;
                        //    }
                        //}
                        break;

                    case OPCode.MOVE:

                        //Get packet as byte array
                        data = packet.ToBytes();

                        //Find the match the client is in
                        Lobby current = new Lobby();
                        foreach (KeyValuePair<string, Lobby> lobby in lobbies)
                        {
                            foreach (Player p in lobby.Value.players)
                            {
                                if (p.endPoint.ToString() == epSender.ToString())
                                {
                                    current = lobby.Value;
                                    break;
                                } 
                            }
                        }

                        foreach (Player p in current.players)
                        {
                            // Broadcast to all logged on users
                            if (p.endPoint.ToString() != epSender.ToString())
                            {
                                Console.WriteLine("Sending " + packet.OP + ":" + packet.Data + " to " + p.Name);
                                serverSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, p.endPoint, new AsyncCallback(SendData), p.endPoint);
                            }
                        }

                        break;

                    case OPCode.DIE:
                        break;

                    case OPCode.SHOOT:
                        break;
                }

                receiveData = new byte[1024];
                // Listen for more connections again...
                serverSocket.BeginReceiveFrom(receiveData, 0, receiveData.Length, SocketFlags.None, ref epSender, new AsyncCallback(ReceiveData), epSender);

                //// Update status through a delegate
                //this.Invoke(this.updateStatusDelegate, new object[] { sendData.ChatMessage });
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReceiveData Error: " + ex.Message);
                Console.WriteLine(Encoding.ASCII.GetString(receiveData));
            }
        }

        public static void SendData(IAsyncResult asyncResult)
        {
            try
            {
                serverSocket.EndSend(asyncResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendData Error: " + ex.Message);
            }
        }
    }

    class Lobby
    {
        public List<Player> players = new List<Player>();
        public List<Player> RedTeam = new List<Player>(), BlueTeam = new List<Player>();
        public string Name;
        public int ID = 0;
        public int RedPoints = 0, BluePoints = 0;

        public Lobby() { }
        public Lobby(string name)
        {
            Name = name;
        }
    }

    class Player
    {
        public EndPoint endPoint;
        public string Name;
        public Vector2 Position;

        public Player(EndPoint ep, string name, Vector2 pos)
        {
            endPoint = ep;
            Name = name;
            Position = pos;
        }
    }

    class Packet
    {
        public OPCode OP;
        public string Data;

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
