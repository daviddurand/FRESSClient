﻿using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FressClient
{
    public class Buffer : Transformable, Drawable
    {
        private readonly Text _drawableText;
        private RectangleShape _cursor;
        private RectangleShape _border;
        private RectangleShape _textHighlight;
        private int _cursorIndex;
        private string _bufferText = "";

        private int _windowNumber;

        public bool DisplayCursor { get; set; } = false;

        public bool DisableFormatting { get; set; } = false;

        public Vector2i CharacterSize { get; set; }

        public bool Active
        {
            set => _border.OutlineColor = value ? new Color(0xff, 0, 0) : new Color(0x7f, 0x7f, 0x7f);
        }

        public uint CurrentChar
        {
            get
            {
                if (_cursorIndex < BufferText.Length)
                {
                    char curChar = BufferText[_cursorIndex];
                    if (curChar == '\n' || curChar == '\t') return ' ';
                    return curChar;
                }

                return ' ';
            }
        }

        public Buffer(Vector2i characterSize)
        {
            _drawableText = new Text(BufferText, Program.Font, Program.FontSize);
            _cursor = new RectangleShape() { FillColor = new Color(Color.White) };
            _border = new RectangleShape() { FillColor = new Color(0, 0, 0, 0), OutlineColor = new Color(0x7f, 0x7f, 0x7f), OutlineThickness = 1 };
            _textHighlight = new RectangleShape() { FillColor = new Color(0x77, 0x77, 0x77) };
            CharacterSize = characterSize;
        }

        public void SetWindowNumber(int windowNumber)
        {
            _windowNumber = windowNumber;
        }

        public string BufferText
        {
            get => _bufferText;
            set
            {
                _bufferText = value;
                _cursorIndex = Math.Min(_cursorIndex, _bufferText.Length);
            }
        }

        public event Action<string, Mouse.Button> TextClicked;

        public void Draw(RenderTarget target, RenderStates states)
        {
            states.Transform.Combine(Transform);
            FloatRect size = _drawableText.Font.GetGlyph('a', _drawableText.CharacterSize, false, 0).Bounds;
            float height = Program.CharHeight;
            _cursor.Size = new Vector2f(size.Width, height - 1);
            _drawableText.DisplayedString = "";

            string[] lines = BufferText.Split("\n");
            Vector2f? cursorPos = null;
            Vector2f currentPosition = new Vector2f(0, 0);
            int linesDrawn = 0;
            Text.Styles style = 0;
                int charsDrawn = 0;
            foreach (string line in lines)
            {
                if (linesDrawn++ >= CharacterSize.Y)
                {
                    break;
                }
                string drawable = line;
                Vector2f? pos;
                // while (drawable.Length > CharacterSize.X)
                // {
                    // string l = drawable.Substring(0, CharacterSize.X);
                    // drawable = drawable.Substring(CharacterSize.X);
                    // (pos, style, charsDrawn) = DrawString(l, target, states, style, currentPosition, charsDrawn);
                    // cursorPos = cursorPos ?? pos;
                    // currentPosition.Y += height;
                // }

                (pos, style, charsDrawn) = DrawString(drawable, target, states, style, currentPosition, charsDrawn);
                cursorPos = cursorPos ?? pos;
                charsDrawn++;//for newline
                currentPosition.Y += height;
            }

            if (cursorPos == null)
            {
                Vector2f p = _drawableText.FindCharacterPos((uint) _drawableText.DisplayedString.Length);
                cursorPos = new Vector2f(p.X, currentPosition.Y - height);
            }

            if (DisplayCursor)
            {
                _cursor.Position = new Vector2f(cursorPos.Value.X, cursorPos.Value.Y + 1);
                target.Draw(_cursor, states);
            }

            _border.Size = new Vector2f(CharacterSize.X * Program.CharWidth, CharacterSize.Y * Program.CharHeight);
            target.Draw(_border, states);
        }

        private (Vector2f?, Text.Styles, int) DrawString(string s, RenderTarget target, RenderStates states, Text.Styles style, Vector2f position, int charsRendered)
        {
            List<(string, Text.Styles, int, int)> SplitString(string str, Text.Styles initialStyle)
            {
                if (DisableFormatting)
                {
                    return new List<(string, Text.Styles, int, int)> { (str, 0, 0, 0) };
                }

                var strings = new List<(string, Text.Styles, int, int)>();
                int openItalicIndex = -2, openBoldIndex = -2;
                int closeIndex = -2;
                int start = 0;
                for (int i = 0; i < str.Length;)
                {
                    if (openItalicIndex == -2) openItalicIndex = str.IndexOf("!(1", i);
                    if (openBoldIndex == -2) openBoldIndex = str.IndexOf("!(0", i);
                    if (closeIndex == -2) closeIndex = str.IndexOf("!)", i);

                    if (str.Substring(i,1) == "%" && str.Length > i+1)
                    {
                        if (start != i)
                        {
                            string segment = str.Substring(start, i - start);
                            strings.Add((segment, initialStyle, 0, 0));
                        }
                        string seg2 = str.Substring(i, 2);
                        strings.Add((seg2, Text.Styles.Bold, 0, 0));
                        initialStyle = 0;
                        i += 2;
                        start = i; 
                        continue;
                    }
                    if (i == openItalicIndex)
                    {
                        if (start != i)
                        {
                            string segment = str.Substring(start, i - start);
                            strings.Add((segment, initialStyle, 0, 0));
                        }
                        initialStyle = Text.Styles.Italic;

                        i += 3;
                        start = i;
                        openItalicIndex = -2;
                        continue;
                    }

                    if (i == openBoldIndex)
                    {
                        if (start != i)
                        {
                            string segment = str.Substring(start, i - start);
                            strings.Add((segment, initialStyle, 0, 0));
                        }
                        initialStyle = Text.Styles.Bold;

                        i += 3;
                        start = i;
                        openBoldIndex = -2;
                        continue;
                    }

                    if (i == closeIndex)
                    {
                        if (start != i)
                        {
                            string segment = str.Substring(start, i - start);
                            strings.Add((segment, initialStyle, 3, 5));
                        }
                        initialStyle = 0;
                        i += 2;
                        start = i;
                        closeIndex = -2;
                        continue;
                    }

                    ++i;
                }

                if (start != str.Length)
                {
                    string segment = str.Substring(start);
                    strings.Add((segment, initialStyle, initialStyle == Text.Styles.Regular ? 0 : 3,
                        initialStyle == Text.Styles.Regular ? 0 : 5));
                }

                return strings;
            }

            List<(string, Text.Styles, int, int)> splits = SplitString(s, style);
            Vector2f? characterPos = null;
            int count = charsRendered;
            var starti = Math.Min(StartIndex, EndIndex);
            var endi = Math.Max(StartIndex, EndIndex) + 1;//Make it inclusive
            foreach ((string subStr, Text.Styles subStyle, int hiddenStart, int hiddenTotal) in splits)
            {
                _drawableText.DisplayedString = subStr;
                _drawableText.CharacterSize = style == Text.Styles.Bold ? Program.FontSize : Program.FontSize;
                _drawableText.Position = position;
                _drawableText.Style = subStyle;
                if (_cursorIndex >= count && _cursorIndex <= count + subStr.Length)
                {
                    characterPos = _drawableText.FindCharacterPos((uint)(_cursorIndex - count));
                    characterPos = new Vector2f(characterPos.Value.X, position.Y);
                }

                var strLen = subStyle == Text.Styles.Regular ? subStr.Length : subStr.Length + hiddenTotal;
                if (starti != -1 && endi != -1)
                {
                    var currentStart = starti > count + strLen ? -1 : Math.Max(starti, count) - count;
                    var currentEnd = endi <= count ? -1 : Math.Min(endi, count + strLen) - count;
                    if (currentStart != -1 && currentEnd != -1)
                    {
                        if (subStyle != Text.Styles.Regular)
                        {
                            currentStart = Math.Max(0, currentStart - hiddenStart);
                            currentEnd = Math.Max(0, currentEnd - hiddenStart);
                        }
                        var startPos = _drawableText.FindCharacterPos((uint)currentStart);
                        startPos.X += position.X;
                        var endPos = _drawableText.FindCharacterPos((uint)currentEnd);
                        endPos.X += position.X;
                        _textHighlight.Position = new Vector2f(startPos.X, position.Y + 5);
                        _textHighlight.Size = new Vector2f(endPos.X - startPos.X, Program.CharHeight);
                        target.Draw(_textHighlight, states);
                    }
                }
                target.Draw(_drawableText, states);

                position.X += _drawableText.GetLocalBounds().Width;
                count += strLen;
            }


            return (characterPos, style, count);
        }

        public void HandleText(TextEventArgs args)
        {
            if (args.Unicode == "\u001b")
            {
                return;
            }
            string newChar = args.Unicode;
            if (newChar == "\b")
            {
                Backspace();
                return;
            }

            if (newChar == "\r") newChar = "\n";
            BufferText = BufferText.Insert(_cursorIndex, newChar);
            CursorRight();
        }

        public void Backspace()
        {
            string str = BufferText;
            if (str.Length > 0 && _cursorIndex > 0)
            {
                CursorLeft();
                BufferText = str.Remove(_cursorIndex, 1);
            }
        }

        public void CursorLeft()
        {
            _cursorIndex = Math.Max(0, _cursorIndex - 1);
        }

        public void CursorRight()
        {
            _cursorIndex = Math.Min(BufferText.Length, _cursorIndex + 1);
        }

        public void GoToEnd()
        {
            _cursorIndex = BufferText.Length;
        }
         
        public void GoToStart()
        {
            _cursorIndex = 0;
        }
         
        public void Append(string s)
        {
            BufferText += s.Replace("\r", "");
            GoToEnd();
        }

        private int GetIndex(float x, float y)
        {
            FloatRect rect = new FloatRect(Position, new Vector2f(Program.CharWidth, Program.CharHeight));
            int col = 0;
            for (int i = 0; i < BufferText.Length; ++i)
            {
                if (BufferText.ElementAt(i) == '!') {
                    if (BufferText.IndexOf("!(1", i) == i || BufferText.IndexOf("!(0", i) == i)
                    {
                        i += 2;
                        continue;
                    } else if (BufferText.IndexOf("!)", i) == i)
                    {
                        i++;
                        continue;
                    }
                }
                if (col == CharacterSize.X || BufferText[i] == '\n')
                {
                    if (x >= rect.Left && y >= rect.Top && y < rect.Top + rect.Height)
                    {
                        return i;
                    }
                    col = 0;
                    rect.Left = Position.X;
                    rect.Top += rect.Height;
                    if (BufferText[i] == '\n') continue;
                }

                if (col == 0 && x < rect.Left && y >= rect.Top && y < rect.Top + rect.Height)
                {
                    return i;
                }

                if (rect.Contains(x, y))
                {
                    return i;
                }

                col++;
                rect.Left += rect.Width;
            }

            return BufferText.Length;
        }

        private int StartIndex = -1, EndIndex = -1;

        public void HandleMousePress(float x, float y, Mouse.Button button)
        {
            int index = GetIndex(x, y);
            if (index == -1)
            {
                return;
            }

            StartIndex = index;
        }

        public void HandleMouseMove(float x, float y)
        {
            if (StartIndex == -1)
            {
                return;
            }

            int index = GetIndex(x, y);
            if (index == -1)
            {
                return;
            }

            EndIndex = index;
        }

        public void HandleMouseReleased(float x, float y, Mouse.Button button)
        {
            int index = GetIndex(x, y);
            if (index == -1)
            {
                return;
            }

            EndIndex = index;
            SendText(button);
            MouseReleased();
        }

        public void MouseReleased()
        {
            StartIndex = -1;
            EndIndex = -1;
        }

        protected virtual void SendText(Mouse.Button button)
        {
            if (StartIndex == -1 || EndIndex == -1)
            {
                return;
            }

            string translation = "0123456789!$*_;?";
            string GetLP(int index)
            {
                index = Math.Min(Math.Max(index, 0), BufferText.Length);
                Console.WriteLine("Index: " + index);
                Console.WriteLine("text: " + BufferText.Substring(index, Math.Min(8,BufferText.Length-index)));

                string s = "`";
                uint temp = (uint) index;
                //temp &= 0xfe; //Should always be even??
                for (int i = 0; i < 4; ++i)
                {
                    char c = translation[(int)(temp & 0xf)];
                    s += c;
                    temp >>= 4;
                }

                s += _windowNumber + 1;

                return s;
            }

            int startI = Math.Min(StartIndex, EndIndex);
            startI -= BufferText.Substring(0, startI).Count(c => c == '\n');
            int endI = Math.Max(StartIndex, EndIndex);
            endI -= BufferText.Substring(0, endI).Count(c => c == '\n');
            if (StartIndex == EndIndex)
            {
                TextClicked?.Invoke(GetLP(endI), button);
            }
            else
            {
                TextClicked?.Invoke(GetLP(startI) +GetLP(endI), Mouse.Button.Left);
            }

        }
    }
}
