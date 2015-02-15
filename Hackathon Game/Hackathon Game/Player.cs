using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Hackathon_Game
{
    class SPlayer
    {
        public Vector2 Position;
        public string Name;
        public Texture2D Texture;

        public SPlayer(GraphicsDevice gd, string name, Vector2 pos)
        {
            Position = pos;
            Name = name;

            LoadContent(gd);
            byte[] data = Encoding.ASCII.GetBytes(Name);
        }

        public void LoadContent(GraphicsDevice gd)
        {
            Texture = new Texture2D(gd, 30, 30);
            //Set the data to solid color
            Color[] data = new Color[30 * 30];
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] = Color.Black;
            }
            Texture.SetData(data);
        }

        public void Draw(SpriteBatch sb, SpriteFont font)
        {
            sb.Begin();
            sb.Draw(Texture, Position, Color.Black);
            sb.DrawString(font, Name, new Vector2(Position.X + 15 - font.MeasureString(Name).X / 2, Position.Y - font.MeasureString(Name).Y), Color.Black);
            sb.End();
        }

        public void Update()
        {
            if (Keyboard.GetState().IsKeyDown(Keys.A))
            {
                Position.X -= 5;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.D))
            {
                Position.X += 5;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.W))
            {
                Position.Y -= 5;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.S))
            {
                Position.Y += 5;
            }
        }
    }

    class MPlayer
    {
        public Vector2 Position;
        public string Name;
        public Texture2D Texture;
        private bool online;


        public MPlayer(GraphicsDevice gd, string name, Vector2 pos, bool onlinePlayer)
        {
            online = onlinePlayer;
            Position = pos;
            Name = name;

            LoadContent(gd);
        }

        public void LoadContent(GraphicsDevice gd)
        {
            Texture = new Texture2D(gd, 30, 30);
            //Set the data to solid color
            Color[] data = new Color[30 * 30];
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] = Color.Black;
            }
            Texture.SetData(data);
        }

        public void Draw(SpriteBatch sb, SpriteFont font)
        {
            sb.Begin();
            sb.Draw(Texture, Position, Color.Black);
            sb.DrawString(font, Name, new Vector2(Position.X + 15 - font.MeasureString(Name).X/2, Position.Y - font.MeasureString(Name).Y), Color.Black);
            sb.End();
        }

        public void Update()
        {
            if (!online)
            {

                if (Keyboard.GetState().IsKeyDown(Keys.A))
                {
                    Position.X -= 5;
                }

                if (Keyboard.GetState().IsKeyDown(Keys.D))
                {
                    Position.X += 5;
                }

                if (Keyboard.GetState().IsKeyDown(Keys.W))
                {
                    Position.Y -= 5;
                }

                if (Keyboard.GetState().IsKeyDown(Keys.S))
                {
                    Position.Y += 5;
                }
            }
        }
    }
}
