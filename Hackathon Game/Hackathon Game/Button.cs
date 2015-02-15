using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Hackathon_Game
{
    class Button
    {
        public Texture2D Texture;
        public Rectangle Position;
        public Color BackgroundColor, TextColor;
        private Color holder;
        public bool Clicked;
        public bool IsVisible = true;
        private string text;

        private bool colorChanged = false;

        public Button(GraphicsDevice gd, string text, Color b, Color t, Rectangle pos)
        {
            Clicked = false;
            this.text = text;
            Position = pos;
            holder = BackgroundColor = b;
            TextColor = t;
            LoadContent(gd);
        }

        public string Text
        {
            get { return text; }
        }

        public void LoadContent(GraphicsDevice gd)
        {
            Texture = new Texture2D(gd, 100, 30);
            //Set the data to solid color
            Color[] data = new Color[100 * 30];
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] = Color.Chocolate;
            }
            Texture.SetData(data);
        }

        public void Draw(SpriteBatch sb, SpriteFont font)
        {
            if (IsVisible)
            {
                sb.Begin();
                sb.Draw(Texture, Position, BackgroundColor);
                sb.DrawString(font, text, new Vector2(Position.Center.X - font.MeasureString(text).X / 2, Position.Center.Y - font.MeasureString(text).Y / 2), TextColor);
                sb.End();
            }
        }

        public void Update()
        {
            if (IsVisible)
            {
                OnEnter();
                OnExit();
            }
        }

        public void OnEnter()
        {
            if ((Position.Left < Mouse.GetState().X && Mouse.GetState().X < Position.Right)
                && (Position.Bottom > Mouse.GetState().Y && Mouse.GetState().Y > Position.Top))
            {
                if (!colorChanged)
                {
                    Random rand = new Random();
                    int r = Math.Abs(BackgroundColor.R - rand.Next(1, 255)), b = Math.Abs(BackgroundColor.B - rand.Next(1, 255)), g = Math.Abs(BackgroundColor.G - rand.Next(1, 255));
                    BackgroundColor = new Color(r, g, b);
                    colorChanged = true;
                }
                
                if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                {
                    Clicked = true;
                }
            }
        }

        public void OnExit()
        {
            if ((Position.Left > Mouse.GetState().X || Mouse.GetState().X > Position.Right)
                || (Position.Bottom < Mouse.GetState().Y || Mouse.GetState().Y < Position.Top))
            {
                BackgroundColor = holder;
                colorChanged = false;
            }
        }

    }

}
