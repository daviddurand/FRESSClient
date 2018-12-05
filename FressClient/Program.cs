﻿using Rebex.Net;
using Rebex.TerminalEmulation;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Linq;
using System.Text;

namespace FressClient
{
    class Program
    {

        static void Main(string[] args)
        {
            Rebex.Licensing.Key = "==AKOz7Fgv0W1Kau2iJwlo61vuaQ1v05EfMcFXUgg6T5rQ==";
            new Program().Run(args);
        }

        public static Font Font;
        public static readonly uint FontSize = 14;

        public Buffer CommandBuffer;
        public Buffer[] Buffers;
        public Buffer CurrentBuffer => Buffers[CurrentBufferIndex];
        public int CurrentBufferIndex;

        public WindowConfig CurrentConfig;

        public enum WindowConfig
        {
            Config_1A,
            Config_1B, //Don't use
            Config_2A,
            Config_2B,
            Config_3A,
            Config_3B,
            Config_4A,
        }

        [Flags]
        public enum Flag1
        {
            InputMode = 0x1,
            TxWindowDimensions = 0x2,
            TxSpecialMessage = 0x4,
            TxBinaryData = 0x8,
        }

        [Flags]
        public enum Flag2
        {
            MsgLineInBuffer = 0x1,
            Unlightpennable = 0x2,
            StartsInItalic = 0x4,
        }

        public static float CharWidth { get; private set; }
        public static float CharHeight { get; private set; }

        class Sender
        {
            // EBCDIC to ASCII
            private string[] CP37 =
            {
                /*          0123456789ABCDEF  */
                /* 0- */ "                ",
                /* 1- */ "                ",
                /* 2- */ "                ",
                /* 3- */ "                ",
                /* 4- */ "           .<(+|",
                /* 5- */ "&         !$*);~",
                /* 6- */ "-/        |,%_>?",
                /* 7- */ "         `:#@'=\"",
                /* 8- */ " abcdefghi      ",
                /* 9- */ " jklmnopqr      ",
                /* A- */ " ~stuvwxyz      ",
                /* B- */ "^         []  ' ",
                /* C- */ "{ABCDEFGHI-     ",
                /* D- */ "}JKLMNOPQR      ",
                /* E- */ "\\ STUVWXYZ      ",
                /* F- */ "0123456789      "
            };

            private byte[] ASCIItoEBCDIC =
            {
                0x00, 0x01, 0x02, 0x03, 0x1A, 0x09, 0x1A, 0x7F,
                0x1A, 0x1A, 0x1A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x10, 0x11, 0x12, 0x13, 0x3C, 0x3D, 0x32, 0x26,
                0x18, 0x19, 0x3F, 0x27, 0x1C, 0x1D, 0x1E, 0x1F,
                0x40, 0x4F, 0x7F, 0x7B, 0x5B, 0x6C, 0x50, 0x7D,
                0x4D, 0x5D, 0x5C, 0x4E, 0x6B, 0x60, 0x4B, 0x61,
                0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7,
                0xF8, 0xF9, 0x7A, 0x5E, 0x4C, 0x7E, 0x6E, 0x6F,
                0x7C, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7,
                0xC8, 0xC9, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6,
                0xD7, 0xD8, 0xD9, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6,
                0xE7, 0xE8, 0xE9, 0x4A, 0xE0, 0x5A, 0x5F, 0x6D,
                0x79, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
                0x88, 0x89, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96,
                0x97, 0x98, 0x99, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6,
                0xA7, 0xA8, 0xA9, 0xC0, 0x6A, 0xD0, 0xA1, 0x07,
            };

            private byte ToASCII(byte b)
            {
                return (byte)CP37[b / 16][b % 16];
            }

            private byte ToEBCDIC(byte b)
            {
                return b < 0x80 ? ASCIItoEBCDIC[b] : (byte)0x3F;
            }

            public byte[] ToEBDIC(string s)
            {
                return Encoding.ASCII.GetBytes(s).Select(ToEBCDIC).ToArray();
            }

            public string ToASCII(string s)
            {
                return Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(s).Select(ToASCII).ToArray());
            }
        }

        void SetWindowConfig(WindowConfig config)
        {
            if (config == CurrentConfig)
            {
                return;
            }
            switch (config)
            {
                case WindowConfig.Config_1A:
                    Buffers = new[] { new Buffer(new Vector2i(130, 40)) };
                    break;
                case WindowConfig.Config_2A:
                    Buffers = new[] { new Buffer(new Vector2i(130, 20)), new Buffer(new Vector2i(130, 20)) { Position = new Vector2f(0, CharHeight * 20) } };
                    break;
                case WindowConfig.Config_2B:
                    Buffers = new[] { new Buffer(new Vector2i(65, 40)), new Buffer(new Vector2i(65, 40)) { Position = new Vector2f(CharWidth * 65, 0) } };
                    break;
                case WindowConfig.Config_3B:
                    Buffers = new[]{
                        new Buffer(new Vector2i(65, 40)),
                        new Buffer(new Vector2i(65, 20)){Position = new Vector2f(CharWidth * 65, 0)},
                        new Buffer(new Vector2i(65, 20)){Position = new Vector2f(CharWidth * 65, CharHeight * 20)}
                    };
                    break;
                case WindowConfig.Config_3A:
                    Buffers = new[]{
                        new Buffer(new Vector2i(130, 20)),
                        new Buffer(new Vector2i(65, 20)){Position = new Vector2f(0, CharHeight * 20)},
                        new Buffer(new Vector2i(65, 20)){Position = new Vector2f(CharWidth * 65, CharHeight * 20)}
                    };
                    break;
                case WindowConfig.Config_4A:
                    Buffers = new[]{
                        new Buffer(new Vector2i(65, 20)),
                        new Buffer(new Vector2i(65, 20)){Position = new Vector2f(CharWidth * 65, 0)},
                        new Buffer(new Vector2i(65, 20)){Position = new Vector2f(0, CharHeight * 20)},
                        new Buffer(new Vector2i(65, 20)){Position = new Vector2f(CharWidth * 65, CharHeight * 20)}
                    };
                    break;
                default:
                    return;
            }

            CurrentBufferIndex = 0;

            CurrentConfig = config;
        }

        public void SetWindowConfig(string config)
        {
            if (Enum.TryParse($"config_{config}", false, out WindowConfig result))
            {
                SetWindowConfig(result);
            }
        }

        public void SetCurrentWindow(int index)
        {
            if (index >= 0 && index < Buffers.Length)
            {
                CurrentBufferIndex = index;
            }

        }

        private void ParseResponse(string res)
        {
            string[] contents = res.Split('|');
            foreach (string content in contents)
            {
                if (content.StartsWith('\\')) //Command
                {
                    int windowNumber = content[1] - '0';
                    int currentWindowNumber = content[2] - '0';
                    Flag1 flag1 = (Flag1)(content[3] - '0');
                    Flag2 flag2 = (Flag2)(content[4] - '0');

                    if (flag1.HasFlag(Flag1.TxWindowDimensions))
                    {
                        int windowConfig = content[5] - '0';
                        SetWindowConfig(((WindowConfig[])Enum.GetValues(typeof(WindowConfig)))[windowConfig - 1]);
                    }

                    CurrentBufferIndex = currentWindowNumber - 1;

                    Console.WriteLine($"Window Number: {windowNumber}, CurrentWindow: {currentWindowNumber}, Flag1: {flag1}, Flag2: {flag2}");

                    if (!flag1.HasFlag(Flag1.TxSpecialMessage) && !flag1.HasFlag(Flag1.TxBinaryData))
                    {
                        int space = content.IndexOf(' ');
                        if (space >= 0)
                        {
                            string text = content.Substring(space + 1);
                            //Console.WriteLine($"Data: {text}");
                            if (text.Any())
                            {
                                Buffers[windowNumber - 1].BufferText = text.Replace("\r", "");
                            }
                        }
                    }
                }
            }
        }

        public Scripting Scripting;
        private void SubmitCommand(string command)
        {
            if (!string.IsNullOrWhiteSpace(command))
            {
                Scripting.Send(command + "\r\n");
            }

            string res = Scripting.ReadUntil(ScriptEvent.Timeout);
            if (res != null)
            {
                ParseResponse(res);
            }
        }

        private void Run(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Not enough arguments");
                Console.Read();
                return;
            }

            Font = new Font("resources/courier.ttf");
            CharWidth = Font.GetGlyph('a', FontSize, false, 0).Bounds.Width;
            CharHeight = Font.GetLineSpacing(FontSize);

            RenderWindow window = new RenderWindow(new VideoMode((uint) (CharWidth * 66 * 2), (uint) (CharHeight * 41)), "FRESS");
            window.KeyPressed += WindowOnKeyPressed;
            window.TextEntered += Window_TextEntered;
            window.Closed += WindowOnClosed;

            CommandBuffer = new Buffer(new Vector2i(130, 1)) {Position = new Vector2f(0, CharHeight * 40)};
            SetWindowConfig(WindowConfig.Config_2B);

            string ip = args[0];
            int port = int.Parse(args[1]);
            Telnet server = new Telnet(ip, port);
            Scripting = server.StartScripting(new TerminalOptions() {TerminalType = TerminalType.Ansi, NewLineSequence = NewLineSequence.CRLF});
            var scripting = Scripting;
            scripting.Timeout = 300;

            string res = scripting.ReadUntil(ScriptEvent.Timeout);
            Console.WriteLine(res);
            scripting.Send(ConsoleKey.Enter, 0);
            res = scripting.ReadUntil(ScriptEvent.Timeout);
            Console.WriteLine(res);
            if (res.StartsWith("CMS"))
            {

            }
            else
            {
                scripting.Send("l dgd plasmate");
                Console.WriteLine(scripting.ReadUntil(ScriptEvent.Timeout));
                scripting.Send("b");
                Console.WriteLine(scripting.ReadUntil(ScriptEvent.Timeout));
                scripting.Send(ConsoleKey.Enter, 0);
                res = scripting.ReadUntil(ScriptEvent.Timeout);
                Console.WriteLine(res);
                if (!res.StartsWith("CMS"))
                {
                    //Console.WriteLine("Error connecting to 370");
                    //return;
                }
            }

            var initialCommand = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(initialCommand))
            {
                SubmitCommand(initialCommand);
            }

            while (window.IsOpen)
            {
                window.Clear(new SFML.Graphics.Color(0, 0, 50));
                window.DispatchEvents();

                foreach (Buffer buffer in Buffers)
                {
                    window.Draw(buffer);
                }

                window.Draw(CommandBuffer);

                window.Display();
            }
        }

        private void WindowOnKeyPressed(object sender, KeyEventArgs keyEventArgs)
        {
            switch (keyEventArgs.Code)
            {
                case Keyboard.Key.Left:
                    CurrentBuffer.CursorLeft();
                    break;
                case Keyboard.Key.Right:
                    CurrentBuffer.CursorRight();
                    break;
                default:
                    break;
            }
        }

        private void Window_TextEntered(object sender, TextEventArgs e)
        {
            if (e.Unicode == "\r")
            {
                SubmitCommand(CommandBuffer.BufferText);
                CommandBuffer.BufferText = "";
            }
            else
            {
                CommandBuffer.HandleText(e);
            }
        }

        private void WindowOnClosed(object sender, EventArgs eventArgs)
        {
            (sender as Window)?.Close();
        }
    }
}
