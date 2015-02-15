using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Hackathon_Game
{
    class UIDialog
    {
        Texture2D backgroundTexture, borderTexture;
        SpriteFont font;
        Rectangle Area;
        Color Background, Border, textColor;

        string title;

        Keys[] lastPressedKeys;
        StringBuilder input;
        public bool PressedEnter;
        public bool IsVisible;

        //Helps create the input field for the dialog
        string inputField;

        int inputPadding = 5, thickness;

        int cursorRate = 400;
        DateTime start = DateTime.Now;
        private bool showCursor;

        /// <summary>
        /// Creates a UI Dialog.
        /// </summary>
        /// <param name="gd">The game's graphics device</param>
        /// <param name="area">Position and size of the dialog.</param>
        /// <param name="capacity">Character capacity for the dialog.</param>
        /// <param name="backgroundColor">Background color of the dialog.</param>
        public UIDialog(GraphicsDevice gd, ContentManager content, string title, Rectangle area, int capacity, int borderThickness, Color backgroundColor, Color borderColor, Color textColor)
        {
            inputField = "";
            for (int i = 0; i < capacity+4; i++)
            {
                inputField += " ";
            }

            PressedEnter = false;
            IsVisible = false;
            showCursor = false;

            Area = area;
            Background = backgroundColor;
            Border = borderColor;
            thickness = borderThickness;
            this.textColor = textColor;
            font = content.Load<SpriteFont>("LCD 14");

            this.title = title;

            SetColor(gd, ref backgroundTexture, Background);
            SetColor(gd, ref borderTexture, Border);

            input = new StringBuilder(0, capacity);
            lastPressedKeys = new Keys[0];
        }

        private void SetColor(GraphicsDevice gd, ref Texture2D texture, Color color)
        {
            //Set the data to solid color
            Color[] data = new Color[1];
            data[0] = color;
            texture = new Texture2D(gd, 1, 1);
            texture.SetData(data);
        }

        public string Text
        {
            get { return input.ToString(); }
        }

        public void Draw(SpriteBatch sb)
        {
            if (IsVisible)
            {
                sb.Begin();
                //Draw background
                sb.Draw(backgroundTexture, Area, Background);
                //Draw Top Border
                sb.Draw(borderTexture, new Rectangle(Area.Left, Area.Top, Area.Width, thickness), Border);
                //Draw Right Border
                sb.Draw(borderTexture, new Rectangle(Area.Right - thickness, Area.Top, thickness, Area.Height), Border);
                //Draw Bottom Border
                sb.Draw(borderTexture, new Rectangle(Area.Left, Area.Bottom - thickness, Area.Width, thickness), Border);
                //Draw Left Border
                sb.Draw(borderTexture, new Rectangle(Area.Left, Area.Top, thickness, Area.Height), Border);

                //Draw input field
                sb.Draw(borderTexture, new Rectangle(Area.Center.X - (int)font.MeasureString(inputField.Substring(0, inputField.Length/2)).X, (int)(Area.Center.Y + 2 * font.MeasureString(" ").Y), (int)font.MeasureString(inputField).Length(), 10), Border);

                sb.DrawString(font, title, new Vector2(Area.Center.X - font.MeasureString(title).X / 2, Area.Top + 2 * font.MeasureString(title).Y), textColor);
               
                sb.DrawString(font, input.ToString(), new Vector2(Area.Center.X - font.MeasureString(input.ToString()).X / 2, Area.Center.Y + font.MeasureString(input.ToString()).Y), textColor);
                if (showCursor)
                {
                    sb.DrawString(font, "|", new Vector2((Area.Center.X - font.MeasureString(input.ToString()).X / 2) + font.MeasureString(input.ToString()).Length(), Area.Center.Y + font.MeasureString(" ").Y), textColor);
                }
                sb.End();
            }
        }

        public void Update(GameTime gt)
        {
            if (IsVisible)
            {
                KeyboardHandler();
                FlashingCursor();
            }
        }

        private void FlashingCursor()
        {
            DateTime temp = DateTime.Now;
            if (((temp.Millisecond - start.Millisecond) % cursorRate) <= 5)
            {
                showCursor = !showCursor;
                start = temp;
            }
            
        }

        private void KeyboardHandler()
        {
            Keys[] pressed = Keyboard.GetState().GetPressedKeys();

            //clip last character
            if ((!pressed.Contains(Keys.Back) && lastPressedKeys.Contains(Keys.Back)) && input.Length > 0)
            {
                input = input.Remove(input.Length - 1, 1);
            }

            foreach (Keys key in lastPressedKeys)
            {
                if (!pressed.Contains(key)
                    && (key == Keys.A || key == Keys.B || key == Keys.C || key == Keys.D || key == Keys.E || key == Keys.F || key == Keys.G || key == Keys.H
                    || key == Keys.I || key == Keys.J || key == Keys.K || key == Keys.L || key == Keys.M || key == Keys.N || key == Keys.O || key == Keys.P
                    || key == Keys.Q || key == Keys.R || key == Keys.S || key == Keys.T || key == Keys.U || key == Keys.V || key == Keys.W || key == Keys.X
                    || key == Keys.Y || key == Keys.Z) 
                    && input.Length < input.MaxCapacity)
                {
                    input.Append(key.ToString());
                }
                if (!pressed.Contains(key)
                    && key == Keys.Space && input.Length < input.MaxCapacity)
                {
                    input.Append(' ');
                }
            }


            //If user enters ENTER then exit dialog
            if ((!pressed.Contains(Keys.Enter) && lastPressedKeys.Contains(Keys.Enter)))
            {
                PressedEnter = true;
            }

            lastPressedKeys = pressed;
        }

        public void SaveToFile(string USERINFO)
        {
            if (!File.Exists(USERINFO))
            {
                using (FileStream file = File.Create(USERINFO))
                {
                    file.Write(Encoding.ASCII.GetBytes(Text), 0, Encoding.ASCII.GetBytes(Text).Length);
                }
            }
            else
            {
                File.WriteAllBytes(USERINFO, Encoding.ASCII.GetBytes(Text));
            }
        }
    }
}
