using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace Hackathon_Game
{
    public abstract class State
    {
        protected const string USERINFO = @"_data.txt";

        public State()
        {

        }

        private void LoadContent() { }

        public abstract void Draw(SpriteBatch spriteBatch);

        public abstract void Update(GameTime gt);
    }

    class SplashState : State
    {
        Texture2D splash;
        SpriteFont font;
        private DateTime began;

        ContentManager content;
        GraphicsDevice graphics;

        public SplashState(ContentManager content, GraphicsDevice gd)
        {
            this.content = content;
            graphics = gd;
            splash = content.Load<Texture2D>("splash_screen");
            font = content.Load<SpriteFont>("LCD");
            began = DateTime.Now;
        }

        private void LoadContent()
        {
            throw new NotImplementedException();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();
            spriteBatch.Draw(splash, new Rectangle(0, 0, 500, 500), Color.White);
            spriteBatch.DrawString(font, "< Press any key to continue. >", new Vector2(50, 400), Color.White);
            spriteBatch.End();
        }


        public override void Update(GameTime gt)
        {
            if (Keyboard.GetState().GetPressedKeys().Length > 0)
            {
                Game1.gsm.Pop();
                Game1.gsm.Push(new MenuState(content, graphics));
            }
        }
    }

    class MenuState : State
    {
        Button Singleplayer, Multiplayer,
                Options, Exit,
                Create, Join;
        SpriteFont font;
        GraphicsDevice graphics;
        ContentManager content;
        SoundEffect menu;

        UIDialog createUsername;

        bool inLobby;
        string createdName;
        byte[] buffer;

        public MenuState(ContentManager content, GraphicsDevice gd)
        {
            Singleplayer = new Button(gd, "Singleplayer", Color.Crimson, Color.White, new Rectangle(150, 50, 200, 50));
            Multiplayer = new Button(gd, "Multiplayer", Color.Crimson, Color.White, new Rectangle(150, 150, 200, 50));
            Options = new Button(gd, "Options", Color.Crimson, Color.White, new Rectangle(150, 250, 200, 50));
            Exit = new Button(gd, "Exit", Color.Crimson, Color.White, new Rectangle(150, 350, 200, 50));
            inLobby = false;
            createdName = "";

            this.content = content;
            graphics = gd;
            font = content.Load<SpriteFont>("LCD");

            buffer = new byte[1024];
            this.LoadContent(gd);
            createUsername = new UIDialog(gd, content, "Enter Username:", new Rectangle(100, 100, 300, 300), 16, 10, Color.Crimson, Color.Black, Color.Gray);          
        }

        private void LoadContent(GraphicsDevice gd)
        {
            menu = content.Load<SoundEffect>("Amoveo_Menu");
            menu.Play();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Singleplayer.Draw(spriteBatch, font);
            Multiplayer.Draw(spriteBatch, font);
            Options.Draw(spriteBatch, font);
            Exit.Draw(spriteBatch, font);
            createUsername.Draw(spriteBatch);
        }


        public override void Update(GameTime gt)
        {
            Singleplayer.Update();
            Multiplayer.Update();
            Options.Update();
            Exit.Update();

            createUsername.Update(gt);
            if (createUsername.PressedEnter)
            {
                createUsername.SaveToFile(USERINFO);
                createUsername.IsVisible = false;
            }

            #region Load SinglePlayer
            if (Singleplayer.Clicked)
            {
                Singleplayer.Clicked = false;
                string[] userData = null;

                //Does the player have local data?
                if (File.Exists(USERINFO))
                {
                    userData = File.ReadAllLines(USERINFO);
                    //User has data
                    if (userData.Length > 0)
                    {
                        Game1.gsm.Pop();
                        Game1.gsm.Push(new SPlayState(graphics, content));
                    }
                    //create info
                    else
                    {
                        createUsername.IsVisible = true;
                    }
                }
                //Create new info
                else
                {
                    createUsername.IsVisible = true;
                }
            }
            #endregion

            #region Load MultiPlayer
            //Start multiplayer process
            if (Multiplayer.Clicked)
            {
                Multiplayer.Clicked = false;
                string[] userData = null;

                //Does the player have local data?
                if (File.Exists(USERINFO))
                {
                    userData = File.ReadAllLines(USERINFO);
                    //User has data
                    if (userData.Length > 0)
                    {
                        Game1.gsm.Pop();
                        Game1.gsm.Push(new LobbyState(content, graphics));
                    }
                    //create info
                    else
                    {
                        createUsername.IsVisible = true;
                    }
                }
                //Create new info
                else
                {
                    createUsername.IsVisible = true;
                }
            }
            #endregion
        }
    }

    class SPlayState : State
    {
        SPlayer player;
        SpriteFont font;

        public SPlayState(GraphicsDevice gd, ContentManager content)
        {
            font = content.Load<SpriteFont>("LCD 14");
            player = new SPlayer(gd, FindOnlineName(), new Vector2(gd.Viewport.Width / 2, gd.Viewport.Height / 2));
        }

        private string FindOnlineName()
        {
            string[] lines = File.ReadAllLines(USERINFO);
            return lines[0];
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            player.Draw(spriteBatch, font);
        }


        public override void Update(GameTime gt)
        {
            player.Update();
        }
    }

    class LobbyState : State
    {
        ContentManager content;
        GraphicsDevice graphics;
        public UIDialog lobbyName;
        SpriteFont font;

        Texture2D lineTexture;

        List<string> playerNames;
        List<Button> lobbies;
        Packet packet;
        byte[] buffer;

        Button create, join, start;
        private string createdName, currentLobby;
        string title;

        public LobbyState(ContentManager content, GraphicsDevice gd)
        {
            font = content.Load<SpriteFont>("LCD 14");

            buffer = new byte[1024];
            this.content = content;
            graphics = gd;

            lobbyName = new UIDialog(gd, content, "Lobby Name:", new Rectangle(50, 100, 400, 200), 16, 20, Color.Crimson, Color.Black, Color.Gray);
            lobbyName.IsVisible = false;
            title = "Lobbies:";

            //Set the data to solid color
            Color[] data = new Color[1];
            data[0] = Color.Black;
            lineTexture = new Texture2D(gd, 1, 1);
            lineTexture.SetData(data);

            playerNames = new List<string>();
            lobbies = new List<Button>();
            start = new Button(graphics, "Start!", Color.Black, Color.Crimson, new Rectangle(Game1.GAME_WIDTH / 2 - 50, Game1.GAME_HEIGHT - 150, 100, 50));
            Start();
        }

        private void Start()
        {
            string[] userData = null;

            //Does the player have local data?
            if (File.Exists(USERINFO))
            {
                userData = File.ReadAllLines(USERINFO);
                //User has data
                if (userData.Length > 0)
                {
                    createdName = userData[0];
                    //Ask user to create or join lobby, so initialize our buttons.
                    create = new Button(graphics, "Create", Color.Crimson, Color.Gray, new Rectangle(100, 50, 300, 150));
                    join = new Button(graphics, "Join", Color.Crimson, Color.Gray, new Rectangle(100, 250, 300, 150));
                    Network.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    Thread.Sleep(350);
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            join.Draw(spriteBatch, font);
            create.Draw(spriteBatch, font);
            lobbyName.Draw(spriteBatch);
            //We have enough to start a game
            if (playerNames.Count >= 2)
            {
                start.Draw(spriteBatch, font);
            }

            foreach (Button b in lobbies)
            {
                b.Draw(spriteBatch, font);
            } 

            spriteBatch.Begin();
            spriteBatch.DrawString(font, title, new Vector2(Game1.GAME_WIDTH/2 - (int)font.MeasureString(title).Length()/2, 25), Color.Black);

            if (playerNames.Count > 0)
            {
                spriteBatch.DrawString(font, "Players in lobby:", new Vector2(Game1.GAME_WIDTH/10, 75), Color.Black);
                //draw line under "players in lobby:"
                spriteBatch.Draw(lineTexture, new Rectangle(25, 75 + (int)font.MeasureString("Players in lobby:").Y, Game1.GAME_WIDTH - 50, 5), Color.Black);
            }
            
            for (int i = 0; i < playerNames.Count; i++)
            {
                spriteBatch.DrawString(font, playerNames[i], new Vector2(75, (i+1)*(font.MeasureString("T").Y+5) + 90), Color.Black);
            }
            spriteBatch.End();
        }


        public override void Update(GameTime gt)
        {
            lobbyName.Update(gt);
            if (lobbyName.PressedEnter)
            {
                currentLobby = lobbyName.Text;
                lobbyName.PressedEnter = false;
                //Start displaying the list of players in the lobby
                lobbyName.IsVisible = false;
                Packet packet = new Packet(OPCode.CREATE, lobbyName.Text);
                try
                {
                    Network.Socket.BeginSendTo(packet.ToBytes(), 0, packet.Length, SocketFlags.None, Network.EndPoint, new AsyncCallback(SendData), null);
                    packet = new Packet(OPCode.JOIN, lobbyName.Text + "," + createdName);
                    Network.Socket.BeginSendTo(packet.ToBytes(), 0, packet.Length, SocketFlags.None, Network.EndPoint, new AsyncCallback(SendData), null);
                    Network.Socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref Network.ePoint, new AsyncCallback(ReceiveData), null);
                    title = lobbyName.Text;
                    create.IsVisible = false;
                    join.IsVisible = false;
                }
                catch (SocketException)
                {
                    
                    throw;
                }
            }

            create.Update();
            join.Update();
            start.Update();
            
            if (create.Clicked)
            {
                create.Clicked = false;
                lobbyName.IsVisible = true;
            }
            if (join.Clicked)
            {
                join.Clicked = false;
                try
                {
                    Packet packet = new Packet(OPCode.SEARCH);
                    Network.Socket.BeginSendTo(packet.ToBytes(), 0, packet.Length, SocketFlags.None, Network.EndPoint, new AsyncCallback(SendData), null);
                    Network.Socket.Receive(buffer, SocketFlags.None);
                    packet = new Packet(Encoding.ASCII.GetString(buffer));

                    //handle data
                    if (packet.OP == OPCode.LOBBY)
                    {
                        string[] names = packet.Data.Split(',');
                        //Dynamically make buttons so the user can select different lobbies to enter
                        for (int i = 0; i < names.Length; i++)
                        {
                            lobbies.Add(new Button(graphics, names[i], Color.Crimson, Color.Black, new Rectangle(50, (i+1)*100, 200, 50)));
                        }
                    }

                    //Get rid of create and join buttons
                    create.IsVisible = false;
                    join.IsVisible = false;
                }
                catch (SocketException)
                {
                    
                    throw;
                }
            }

            //Added Lobbies updater
            bool somethingWasClicked = false;
            foreach (Button b in lobbies)
            {
                b.Update();
                if (b.Clicked) //Send JOIN op
                {
                    b.Clicked = false;
                    try
                    {
                        Packet packet = new Packet(OPCode.JOIN, b.Text + "," + createdName);
                        currentLobby = b.Text;
                        Network.Socket.BeginSendTo(packet.ToBytes(), 0, packet.Length, SocketFlags.None, Network.EndPoint, new AsyncCallback(SendData), null);
                        //Wait for packet of names.
                        Network.Socket.Receive(buffer, SocketFlags.None);
                        packet = new Packet(Encoding.ASCII.GetString(buffer));

                        //handle data
                        if (packet.OP == OPCode.LOBBY)
                        {
                            List<string> names = new List<string>();
                            names.AddRange(packet.Data.Split(','));
                            playerNames = names;
                        } 

                        somethingWasClicked = true;
                    }
                    catch (SocketException)
                    {
                        
                        throw;
                    }
                    break;
                }
            }
            if (somethingWasClicked)
            {
                foreach (Button b in lobbies)
                {
                    b.IsVisible = false;
                }
            }

            if (start.Clicked)
            {
                Packet packet = new Packet(OPCode.START, currentLobby);
                Network.Socket.BeginSendTo(packet.ToBytes(), 0, packet.Length, SocketFlags.None, Network.EndPoint, new AsyncCallback(SendData), null);
                Game1.gsm.Pop();
                Game1.gsm.Push(new MPlayState(graphics, content, new string[] { "hahaha" }));

            }
        }

        private void ReceiveData(IAsyncResult ar)
        {
            try
            {
                //Receive data from server
                Network.Socket.EndReceive(ar);

                packet = new Packet(Encoding.ASCII.GetString(buffer));

                buffer = new byte[1024];

                
                if (packet.OP == OPCode.START)
                {
                    
                    Game1.gsm.Pop();
                    Game1.gsm.Push(new MPlayState(graphics, content, new string[] { "hahaha" }));
                    Network.Socket.EndReceive(ar);
                    Network.Socket.EndSend(ar);
                }
                if (packet.OP == OPCode.LOBBY)
                {
                    List<string> names = new List<string>();
                    names.AddRange(packet.Data.Split(','));
                    playerNames = names;
                }

                Network.Socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref Network.ePoint, new AsyncCallback(ReceiveData), null);
                
            }
            catch (Exception) { }
        }

        private void SendData(IAsyncResult ar)
        {
            try
            {
                Network.Socket.EndSend(ar);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    class MPlayState : State
    {
        MPlayer player;
        List<MPlayer> enemies;

        SpriteFont font;
        GraphicsDevice graphics;

        string localName;

        private Packet sendPacket, received;
        private byte[] buffer;

        public MPlayState(GraphicsDevice gd, ContentManager content, string[] players)
        {
            localName = FindOnlineName();
            graphics = gd;
            font = content.Load<SpriteFont>("LCD 14");
            player = new MPlayer(gd, localName, new Vector2(gd.Viewport.Width / 2, gd.Viewport.Height / 2), false);
            enemies = new List<MPlayer>();
            foreach (string name in players)
            {
                if (name != localName)
                {
                    enemies.Add(new MPlayer(gd, name, new Vector2(0, 0), true));                   
                }
            }

            buffer = new byte[1024];
            sendPacket = new Packet(OPCode.MOVE, player.Position.X + "," + player.Position.Y);
            received = new Packet();
            StartNetwork();
        }

        private void StartNetwork()
        {

            //Network
            //Send position async to server
            Network.Socket.BeginSendTo(sendPacket.ToBytes(), 0, sendPacket.Length, SocketFlags.None, Network.EndPoint, new AsyncCallback(SendData), null);
            //Receive data async from server.
            
            Network.Socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref Network.ePoint, new AsyncCallback(ReceiveData), null);
        }
        #region AsyncCallbacks
        private void ReceiveData(IAsyncResult ar)
        {
            try
            {
                //Receive data from server
                Network.Socket.EndReceive(ar);

                received = new Packet(Encoding.ASCII.GetString(buffer));

                buffer = new byte[1024];

                PacketHandler();
                Network.Socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref Network.ePoint, new AsyncCallback(ReceiveData), null);
                
            }
            catch (Exception) { }
        }

        private void PacketHandler()
        {
            switch (received.OP)
            {
                case OPCode.DISCONNECT:
                   // enemy = new MPlayer(graphics, "Bot 1", new Vector2(graphics.Viewport.Width / 2, graphics.Viewport.Height / 2), true);
                    break;
                case OPCode.SHOOT:
                    break;
                case OPCode.DIE:
                    break;
                case OPCode.MOVE:
                    enemies[0].Position = received.Position;
                    break;
                default:
                    break;
            }
        }

        private void SendData(IAsyncResult ar)
        {
            try
            {
                Network.Socket.EndSend(ar);


                Network.Socket.BeginSendTo(sendPacket.ToBytes(), 0, sendPacket.Length, SocketFlags.None, Network.EndPoint, new AsyncCallback(SendData), null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendData Error: " + ex.Message);
            }
        }
        #endregion

        private string FindOnlineName()
        {
            string[] lines = File.ReadAllLines(USERINFO);
            return lines[0];
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            player.Draw(spriteBatch, font);
            foreach (MPlayer p in enemies)
            {
                p.Draw(spriteBatch, font);
            }
                
        }


        public override void Update(GameTime gt)
        {
            sendPacket = new Packet(OPCode.MOVE, player.Position.X.ToString() + "," + player.Position.Y.ToString());
            player.Update();
            foreach (MPlayer p in enemies)
            {
                p.Update();
            }
        }

    }
}
