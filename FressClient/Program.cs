﻿using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Color = SFML.Graphics.Color;
using System.Threading.Tasks;


namespace FressClient
{
    class Program
    {

        public enum DeviceCap
        {
            /// <summary>
            /// Logical pixels inch in X
            /// </summary>
            LOGPIXELSX = 88,
            /// <summary>
            /// Logical pixels inch in Y
            /// </summary>
            LOGPIXELSY = 90,


            VERTRES = 10,

            DESKTOPVERTRES = 117

            // Other constants may be founded on pinvoke.net
        }

        public static float GetSystemScaling()
        {
            return 1f;
        }

        static void Main(string[] args)
        {
            //Rebex.Licensing.Key = "==AKOz7Fgv0W1Kau2iJwlo61vuaQ1v05EfMcFXUgg6T5rQ==";
            new Program().Run(args);
        }

        public static Font Font;
        public static readonly uint FontSize = 28;
        public static readonly uint MenuFontSize = 20;

        public Buffer CommandBuffer, ErrorBuffer;
        public Buffer[] Buffers;
        public Buffer CurrentBuffer => Buffers[CurrentBufferIndex];
        public int CurrentBufferIndex;

        public List<Drawable> Buttons = new List<Drawable>();

        public WindowConfig CurrentConfig = WindowConfig.None;

        public enum WindowConfig
        {
            None,
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
        public class Win {
           public static readonly int Lines = 36;
           public static readonly int Chars = 120;
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
        public static int MillisecondEventThrottle = 200;

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
            /*
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
            */
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
                    Buffers = new[] { new Buffer(new Vector2i(Win.Chars, Win.Lines)) };
                    break;
                case WindowConfig.Config_2A:
                    Buffers = new[] { new Buffer(new Vector2i(Win.Chars, Win.Lines/2)), new Buffer(new Vector2i(Win.Chars, Win.Lines/2)) { Position = new Vector2f(0, CharHeight * Win.Lines/2) } };
                    break;
                case WindowConfig.Config_2B:
                    Buffers = new[] { new Buffer(new Vector2i(Win.Chars/2, Win.Lines)), new Buffer(new Vector2i(Win.Chars/2, Win.Lines)) { Position = new Vector2f(CharWidth * Win.Chars/2, 0) } };
                    break;
                case WindowConfig.Config_3B:
                    Buffers = new[]{
                        new Buffer(new Vector2i(Win.Chars/2, Win.Lines)),
                        new Buffer(new Vector2i(Win.Chars/2, Win.Lines/2)){Position = new Vector2f(CharWidth * Win.Chars/2, 0)},
                        new Buffer(new Vector2i(Win.Chars/2, Win.Lines/2)){Position = new Vector2f(CharWidth * Win.Chars/2, CharHeight * Win.Lines/2)}
                    };
                    break;
                case WindowConfig.Config_3A:
                    Buffers = new[]{
                        new Buffer(new Vector2i(Win.Chars, Win.Lines/2)),
                        new Buffer(new Vector2i(Win.Chars/2, Win.Lines/2)){Position = new Vector2f(0, CharHeight * Win.Lines/2)},
                        new Buffer(new Vector2i(Win.Chars/2, Win.Lines/2)){Position = new Vector2f(CharWidth * Win.Chars/2, CharHeight * Win.Lines/2)}
                    };
                    break;
                case WindowConfig.Config_4A:
                    Buffers = new[]{
                        new Buffer(new Vector2i(Win.Chars/2, Win.Lines/2)),
                        new Buffer(new Vector2i(Win.Chars/2, Win.Lines/2)){Position = new Vector2f(CharWidth * Win.Chars/2, 0)},
                        new Buffer(new Vector2i(Win.Chars/2, Win.Lines/2)){Position = new Vector2f(0, CharHeight * Win.Lines/2)},
                        new Buffer(new Vector2i(Win.Chars/2, Win.Lines/2)){Position = new Vector2f(CharWidth * Win.Chars/2, CharHeight * Win.Lines/2)}
                    };
                    break;
                default:
                    return;
            }

            for (int index = 0; index < Buffers.Length; index++)
            {
                Buffer buffer = Buffers[index];
                buffer.SetWindowNumber(index);
                Vector2f bufferPosition = buffer.Position;
                bufferPosition.Y += (1 * CharHeight);
                buffer.Position = bufferPosition;
                int i = index;
                buffer.TextClicked += (s, button) => BufferOnTextClicked(s, button, i);
            }

            CurrentBufferIndex = 0;

            CurrentConfig = config;
        }

        private void BufferOnTextClicked(string s, Mouse.Button button, int windowNumber)
        {
            if (button == Mouse.Button.Left)
            {
                CommandBuffer.Append("/" + s);
            }
            else if (button == Mouse.Button.Right)
            {
                SubmitCommand("j/" + s);
            }

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
                CurrentBuffer.Active = false;
                CurrentBufferIndex = index;
                CurrentBuffer.Active = true;
            }
        }

        private Regex _windowCommand = new Regex(@"^\\(?<winNum>\d)(?<curWinNum>\d)(?<flag1>2)(?<flag2>\d)(?<op1>\d)(?<op2>\d)", RegexOptions.Singleline);
        private Regex _commandWithTextRegex = new Regex(@"^\\(?<winNum>\d)(?<curWinNum>\d)(?<flag1>\d)(?<flag2>\d)(?>\t(?<line>[^\r¦]*\r\n))*\t¦", RegexOptions.Singleline);
        private Regex _specialCommandRegex = new Regex(@"^\\(?<winNum>\d)(?<curWinNum>\d)(?<flag1>4)(?<flag2>\d)\t(?<data>[^¦]*?)\r\n\t»",  RegexOptions.Singleline);
        private Regex _residueRegex = new Regex(@"^(?<junk>[^\\]+)(?<data>\\.*)?$", RegexOptions.Singleline);
        private Regex _badcommand = new Regex(@"^(?<junk>\\[^\\]+)(?<data>\\.*)$", RegexOptions.Singleline);
        private string _responseBuffer = "";

        private void HandleResponse(string response)
        {
            void HandleCommand(int window, int currentWindow, Flag1 flag1, Flag2 flag2, int? windowConfig, string text)
            {
                if (flag1.HasFlag(Flag1.TxWindowDimensions))
                {
                    Debug.Assert(windowConfig.HasValue);
                    SetWindowConfig(((WindowConfig[])Enum.GetValues(typeof(WindowConfig)))[windowConfig ?? 0]);
                }

                SetCurrentWindow(currentWindow - 1);

                Console.WriteLine($"Window Number: {window}, CurrentWindow: {currentWindow}, Flag1: {flag1}, Flag2: {flag2}");

                if (flag1.HasFlag(Flag1.TxSpecialMessage) && text != null)
                {
                    Console.WriteLine($"Special message: {text}");
                    ErrorBuffer.BufferText = text.Replace("\r", "");
                }
                else if (!flag1.HasFlag(Flag1.TxBinaryData) && text != null)
                {
                    if (window <= Buffers.Length)
                        Buffers[window - 1].BufferText = text.Replace("\r", "");
                    else Console.WriteLine($"Fress talking to nonexistent window {window}. Contents: {text}");
                }
            }
            bool ParseCommand()
            {
                if (_responseBuffer.Length == 0)
                    return false;
                Match residueMatch = _residueRegex.Match(_responseBuffer);
                if (residueMatch.Success)
                {
                    Console.WriteLine("Non-FRESS output: ");
                    Console.WriteLine(residueMatch.Groups["junk"].Captures[0].Value);
                    if (residueMatch.Groups["data"].Captures.Count > 0)
                    {
                        _responseBuffer = residueMatch.Groups["data"].Captures[0].Value;
                    }
                    else { 
                        _responseBuffer = "";
                    }
                }
                Match commandMatch = _windowCommand.Match(_responseBuffer);
                if (commandMatch.Success)
                {
                    int window = commandMatch.Groups["winNum"].Captures[0].Value[0] - '0';
                    int currentWindow = commandMatch.Groups["curWinNum"].Captures[0].Value[0] - '0';
                    Flag1 flag1 = (Flag1)commandMatch.Groups["flag1"].Captures[0].Value[0] - '0';
                    Flag2 flag2 = (Flag2)commandMatch.Groups["flag2"].Captures[0].Value[0] - '0';
                    int? windowConfig = null;
                    Group windowConfigMatch = commandMatch.Groups["op1"];
                    if (windowConfigMatch.Success)
                    {
                        windowConfig = windowConfigMatch.Captures[0].Value[0] - '0';
                    }
                    HandleCommand(window, currentWindow, flag1, flag2, windowConfig, null);
                    _responseBuffer = _responseBuffer.Substring(commandMatch.Index + commandMatch.Length); //Exclude extra matched \
                    return true;
                }

                Match commandTextMatch = _commandWithTextRegex.Match(_responseBuffer);
                if (commandTextMatch.Success)
                {
                    int window = commandTextMatch.Groups["winNum"].Captures[0].Value[0] - '0';
                    int currentWindow = commandTextMatch.Groups["curWinNum"].Captures[0].Value[0] - '0';
                    Flag1 flag1 = (Flag1)commandTextMatch.Groups["flag1"].Captures[0].Value[0] - '0';
                    Flag2 flag2 = (Flag2)commandTextMatch.Groups["flag2"].Captures[0].Value[0] - '0';

                    string text = "";
                    if (commandTextMatch.Groups["line"].Success)
                    {
                        foreach(Capture cap in commandTextMatch.Groups["line"].Captures) {
                            string line = cap.Value;
                            //trim delimiters from matched string
                            text = text + line.Substring(0, line.Length);
                        }
                    }
                    HandleCommand(window, currentWindow, flag1, flag2, null, text);
                    _responseBuffer = _responseBuffer.Substring(commandTextMatch.Index + commandTextMatch.Length);
                    return true;
                }

                Match specialCommandMatch = _specialCommandRegex.Match(_responseBuffer);
                if (specialCommandMatch.Success)
                {
                    int window = specialCommandMatch.Groups["winNum"].Captures[0].Value[0] - '0';
                    int currentWindow = specialCommandMatch.Groups["curWinNum"].Captures[0].Value[0] - '0';
                    Flag1 flag1 = (Flag1)specialCommandMatch.Groups["flag1"].Captures[0].Value[0] - '0';
                    Flag2 flag2 = (Flag2)specialCommandMatch.Groups["flag2"].Captures[0].Value[0] - '0';

                    if (flag1.HasFlag(Flag1.TxSpecialMessage))
                    {

                        string text = specialCommandMatch.Groups["data"].Captures[0].Value;
                        HandleCommand(window, currentWindow, flag1, flag2, null, text);
                        _responseBuffer = _responseBuffer.Substring(specialCommandMatch.Index + specialCommandMatch.Length);
                        return true;
                    }
                }
                Match bad = _badcommand.Match(_responseBuffer);
                if (bad.Success && bad.Length != 0)
                {
                    if (bad.Groups["junk"].Success)
                    {
                        Console.WriteLine("Bad FRESS Protocol or Non-FRESS output: ");
                        Console.WriteLine(bad.Groups["junk"].Captures[0].Value);
                    }
                    if (bad.Groups["data"].Success)
                        _responseBuffer = bad.Groups["data"].Captures[0].Value;
                    Console.WriteLine("Remaining to process: ");
                    Console.WriteLine(_responseBuffer);
                }
                return false;
            }
            _responseBuffer += response;
            while (ParseCommand()) ;

        }

        //public Scripting Scripting;
        public TelnetSocket Socket;
        private BlockingCollection<string> _commands = new BlockingCollection<string>(5);
        private void SubmitCommand(string command)
        {
            System.Threading.Thread.Yield();
            _commands.Add(command);
        }
        private DateTime last_time = DateTime.Now;

        private void StartSending()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    TimeSpan interval = DateTime.Now - last_time;
                    if (interval.TotalMilliseconds < MillisecondEventThrottle)
                    {
                        await Task.Delay(MillisecondEventThrottle - (int)interval.TotalMilliseconds);
                    }
                    String command = _commands.Take();
                    Socket.WriteCommand(command);
                    Console.WriteLine($"Sent command: {command}");
                    last_time = DateTime.Now;
                    System.Threading.Thread.Yield();
                    System.Threading.Thread.Yield();
                }
            }
            );
        }

        private void HandleRemoteResponses()
        {
            string res = Socket.Read();
            if (res != null)
            {
                HandleResponse(res);
            }
        }

        private Button AddButton(string name, string command)
        {
            Button button = new Button(name) {Size = new Vector2f(CharWidth*20 + 10, CharHeight+20)};
            button.Tapped += b =>
            {
                CommandBuffer.Append(command);
                MainWindow.RequestFocus();
            };
            return button;
        }

        private void AddButtons()
        {
            (string, string)[] patterns = new[]
            {
                ("Left end defer", "="),
                ("Right end defer", "=?"),
                ("Resolve deferred", "?/"),
                ("Choose word", "-w"),
                ("Choose line", "-w"),
                ("Choose order", "-o"),
            };

            (string, string)[] editing = new[]
            {
                ("Delete text", "d"),
                ("Insert text", "i"),
                ("Insert Annotation", "ia"),
                ("Move text", "m"),
                ("Move from workspace", "mws"),
                ("Move to label", "mt"),
                ("Revert", "rev"),
            };

            (string, string)[] viewing = new[]
            {
                ("Change window", "cw "),
                ("Display space", "ds "),
                ("Display Viewspecs", "dv "),
                ("Set Viewspecs", "sv/"),
                ("Set Keyword Display rqst", "skd/"),
                ("Query all files", "q/f"),
            };

            (string, string)[] structure = new[]
            {
                ("Block Trail continuous", "bt/"),
                ("Block Trail Tiscrete", "btd/"),
                ("Insert Block", "ib"),
                ("Insert Decimal Block", "idb"),
                ("Make Decimal Block", "mdb"),
                ("Make Decimal Reference", "mdr"),
                ("Insert Annotation", "ia"),
            };
            (string, string)[] structure2 = new[]{
                ("Make annotation", "ma"),
                ("Refer To Tnnotation", "rta"),
                ("Make Tump", "mj"),
                ("Make Tabel", "ml"),
                ("Make Splice", "ms"),
                ("Split editing area", "sa"),
            };

            (string, string)[] navigation = new[]
            {
                ("Jump", "j/"),
                ("Locate", "l/"),
                ("Return", "r"),
                ("Get label", "gl"),
                ("Get decimal label", "gdl"),
                ("Ring forwards", "r/f"),
                ("Ring backwards", "r/b"),
                ("Trail forwards", "tr/f"),
                ("Trail backwards", "tr/b"),
            };

            (string, (string, string)[])[] menus = new[]
            {
                ("Navigation", navigation),
                ("Editing", editing),
                ("Pattern", patterns),
                ("Viewing", viewing),
                ("Structure", structure),
                ("", structure2),
            };

            int xOff = 10;
            foreach ((string, (string, string)[]) menu in menus)
            {
                int yOff = 10;
                Text header = new Text(menu.Item1, Font, MenuFontSize)
                {
                    Position = new Vector2f(xOff, yOff),
                    FillColor = new Color(0, 0, 0)
                };
                Buttons.Add(header);
                foreach ((string, string) menuItem in menu.Item2)
                {
                    yOff += 20 +(int)CharHeight;
                    Button button = AddButton(menuItem.Item1, menuItem.Item2);
                    button.Position = new Vector2f(xOff, yOff);
                    Buttons.Add(button);
                }

                xOff += (int)(CharWidth * 20+ 20);
            }
        }

        private RenderWindow MainWindow { get; set; }

        private float _scaling;

        private void Run(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Not enough arguments. Needs ip and port");
                Console.Read();
                return;
            }

            Font = new Font("resources/Inconsolata-Regular.ttf");
            CharWidth = Font.GetGlyph('a', FontSize, false, 0).Advance;
            CharHeight = Font.GetLineSpacing(FontSize) - _scaling;

            _scaling = GetSystemScaling();

            RenderWindow commandWindow = new RenderWindow(new VideoMode((uint) (1085 * _scaling), (uint) (205* _scaling)), "Commands");
            MainWindow = new RenderWindow(new VideoMode((uint)(CharWidth * Win.Chars * _scaling), (uint)(CharHeight * Win.Lines*2+2 * _scaling)), "FRESS");
            RenderWindow window = MainWindow;
            window.KeyPressed += WindowOnKeyPressed;
            window.TextEntered += Window_TextEntered;
            window.MouseButtonPressed += WindowOnMouseButtonPressed;
            window.MouseButtonReleased += Window_MouseButtonReleased;
            window.MouseMoved += WindowOnMouseMoved;
            window.MouseWheelScrolled += Window_MouseWheelScrolled;
            window.Resized += WindowOnResized;
            window.Closed += WindowOnClosed;

            var view = new View(window.GetView());
            view.Size = new Vector2f(view.Size.X / _scaling, view.Size.Y / _scaling);
            view.Center = new Vector2f(view.Size.X / 2, view.Size.Y / 2);
            window.SetView(view);

            commandWindow.Resized += WindowOnResized;
            commandWindow.MouseButtonPressed += CommandWindowOnMouseButtonReleased;

            view = new View(commandWindow.GetView());
            view.Size = new Vector2f(view.Size.X / _scaling, view.Size.Y / _scaling);
            view.Center = new Vector2f(view.Size.X / 2, view.Size.Y / 2);
            commandWindow.SetView(view);

            commandWindow.Position = new Vector2i(0, 0);
            window.Position = new Vector2i(200, (int)commandWindow.Size.Y + Win.Lines);

            CommandBuffer = new Buffer(new Vector2i(Win.Chars/2, 1)) { Position = new Vector2f(0, 0), DisplayCursor = true, DisableFormatting = true };
            ErrorBuffer = new Buffer(new Vector2i(Win.Chars/2, 1)) { Position = new Vector2f(CharWidth * Win.Chars/2, 0) };
            SetWindowConfig(WindowConfig.Config_1A);
            SetCurrentWindow(0);

            AddButtons();

            string ip = args[0];
            int port = int.Parse(args[1]);
            Socket = new TelnetSocket(ip, port);
            StartSending();
            while (window.IsOpen)
            {
                window.Clear(new Color(0, 0, 50));
                commandWindow.Clear(new Color(0xc0, 0xc0, 0xc0));
                window.DispatchEvents();
                commandWindow.DispatchEvents();

                foreach (Buffer buffer in Buffers)
                {
                    window.Draw(buffer);
                }

                window.Draw(CommandBuffer);
                window.Draw(ErrorBuffer);

                foreach (Drawable rectangleShape in Buttons)
                {
                    commandWindow.Draw(rectangleShape);
                }
                commandWindow.Display();
                window.Display();
                HandleRemoteResponses();
                System.Threading.Thread.Yield();
            }
        }

        private void WindowOnResized(object sender, SizeEventArgs e)
        {
            Debug.WriteLine($"{e.Width}, {e.Height}");
            (sender as RenderWindow).SetView(new View(new FloatRect(0, 0, e.Width, e.Height)));
        }
        private DateTime LastScrolled = DateTime.Now;

            private void Window_MouseWheelScrolled(object sender, MouseWheelScrollEventArgs e)
        {
            TimeSpan interval = DateTime.Now - last_time;
            if (interval.TotalMilliseconds < MillisecondEventThrottle)
            {
                //System.Threading.Thread.Yield();
                return;
            }
            for (int index = 0; index < Buffers.Length; index++)
            {
                Buffer buffer = Buffers[index];
                FloatRect bounds = new FloatRect(buffer.Position,
                    new Vector2f(buffer.CharacterSize.X * CharWidth, buffer.CharacterSize.Y * CharHeight));
                if (bounds.Contains(e.X, e.Y))
                {
                    int val = ((int)(-e.Delta * 12));
                    if (val < -12) {
                        val = -12;
                    }
                    if (val > 12)
                    {
                        val = 12;
                    }
                    if (val != 0)
                    {
                        SubmitCommand("sc/"+val.ToString()+"/"+(index+1));
                    }

                    break;
                }
            }
        }

        private void CommandWindowOnMouseButtonReleased(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (mouseButtonEventArgs.Button != Mouse.Button.Left)
            {
                return;
            }
            foreach (Drawable button in Buttons)
            {
                if (button is Button b)
                {
                    b.TestTapped(mouseButtonEventArgs.X, mouseButtonEventArgs.Y);
                }
            }
        }

        private void WindowOnMouseButtonPressed(object sender, MouseButtonEventArgs e)
        {
            foreach (Buffer buffer in Buffers)
            {
                FloatRect bounds = new FloatRect(buffer.Position,
                    new Vector2f(buffer.CharacterSize.X * CharWidth, buffer.CharacterSize.Y * CharHeight));
                if (bounds.Contains(e.X, e.Y))
                {
                    if (e.Button == Mouse.Button.Left || e.Button == Mouse.Button.Right)
                    {
                        buffer.HandleMousePress(e.X, e.Y, e.Button);
                    }

                    break;
                }
            }
        }

        private void WindowOnMouseMoved(object sender, MouseMoveEventArgs e)
        {
            foreach (Buffer buffer in Buffers)
            {
                FloatRect bounds = new FloatRect(buffer.Position,
                    new Vector2f(buffer.CharacterSize.X * CharWidth, buffer.CharacterSize.Y * CharHeight));
                if (bounds.Contains(e.X, e.Y))
                {
                    buffer.HandleMouseMove(e.X, e.Y);

                    break;
                }
            }
        }

        private bool _shifted = false;
        private bool _control = false;
        private void Window_MouseButtonReleased(object sender, MouseButtonEventArgs e)
        {
            for (int index = 0; index < Buffers.Length; index++)
            {
                Buffer buffer = Buffers[index];
                FloatRect bounds = new FloatRect(buffer.Position,
                    new Vector2f(buffer.CharacterSize.X * CharWidth, buffer.CharacterSize.Y * CharHeight));
                if (bounds.Contains(e.X, e.Y))
                {
                    if (e.Button == Mouse.Button.Middle || _shifted)
                    {
                        SubmitCommand("cw " + (index + 1));
                    } else if (e.Button == Mouse.Button.Left || e.Button == Mouse.Button.Right)
                    {
                        buffer.HandleMouseReleased(e.X, e.Y, e.Button);
                    }
                    buffer.MouseReleased();
                    break;
                }
            }
        }
        private void WindowOnKeyReleased(object sender, KeyEventArgs keyEventArgs)
        {
        }

        private void WindowOnKeyPressed(object sender, KeyEventArgs keyEventArgs)
        {
            _shifted = keyEventArgs.Shift; // IsKeyPressed(Keyboard.Key.LShift) || Keyboard.IsKeyPressed(Keyboard.Key.RShift);
            _control = keyEventArgs.Control; //Keyboard.IsKeyPressed(Keyboard.Key.LControl) || Keyboard.IsKeyPressed(Keyboard.Key.RControl);
           if (_control)
           {
              switch(keyEventArgs.Code) {
                 case Keyboard.Key.A:
                    CommandBuffer.GoToStart();
                    break;
                 case Keyboard.Key.E:
                    CommandBuffer.GoToEnd();
                    break;
                 case Keyboard.Key.U:
                    CommandBuffer.BufferText = "";
                    break;
                 default:
                    break;
              }
           }
           else
           {
            switch (keyEventArgs.Code)
            {
                case Keyboard.Key.Left:
                    CommandBuffer.CursorLeft();
                    break;
                case Keyboard.Key.Right:
                    CommandBuffer.CursorRight();
                    break;
                case Keyboard.Key.Escape:
                    foreach (Buffer buffer in Buffers)
                    {
                        buffer.MouseReleased();
                    }
                    break;
                case Keyboard.Key.Delete:
                    CommandBuffer.BufferText = "";
                    break;
                default:
                    break;
            }
           }
        }

        private void Window_TextEntered(object sender, TextEventArgs e)
        {
            ErrorBuffer.BufferText = "";
            if (e.Unicode == "\r" || e.Unicode=="\n")
            {
                string command = CommandBuffer.BufferText;
                CommandBuffer.BufferText = "";
                SubmitCommand(command);
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
