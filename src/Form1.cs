using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using KeyboardHook1;

namespace AutoHarp
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(ref Point lpPoint);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);


        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        const UInt32 WM_KEYDOWN = 0x0100;

        [DllImport("user32.dll")]
        internal static extern uint SendInput(uint nInputs,
    [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        public static void SendKeyPress(KeyboardHook.VKeys keyCode)
        {
            INPUT input = new INPUT
            {
                Type = 1
            };
            input.Data.Keyboard = new KEYBDINPUT()
            {
                Vk = (ushort)keyCode,
                Scan = 0,
                Flags = 0,
                Time = 0,
                ExtraInfo = IntPtr.Zero,
            };

            INPUT input2 = new INPUT
            {
                Type = 1
            };
            input2.Data.Keyboard = new KEYBDINPUT()
            {
                Vk = (ushort)keyCode,
                Scan = 0,
                Flags = 2,
                Time = 0,
                ExtraInfo = IntPtr.Zero
            };
            INPUT[] inputs = new INPUT[] { input, input2 };
            if (SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
                throw new Exception();
        }

        public static void SendKeyDown(KeyboardHook.VKeys keyCode)
        {
            INPUT input = new INPUT
            {
                Type = 1
            };
            input.Data.Keyboard = new KEYBDINPUT();
            input.Data.Keyboard.Vk = (ushort)keyCode;
            input.Data.Keyboard.Scan = 0;
            input.Data.Keyboard.Flags = 0;
            input.Data.Keyboard.Time = 0;
            input.Data.Keyboard.ExtraInfo = IntPtr.Zero;
            INPUT[] inputs = new INPUT[] { input };
            if (SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
            {
                throw new Exception();
            }
        }

        public static void SendKeyUp(KeyboardHook.VKeys keyCode)
        {
            INPUT input = new INPUT
            {
                Type = 1
            };
            input.Data.Keyboard = new KEYBDINPUT();
            input.Data.Keyboard.Vk = (ushort)keyCode;
            input.Data.Keyboard.Scan = 0;
            input.Data.Keyboard.Flags = 2;
            input.Data.Keyboard.Time = 0;
            input.Data.Keyboard.ExtraInfo = IntPtr.Zero;
            INPUT[] inputs = new INPUT[] { input };
            if (SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
                throw new Exception();

        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT
        {
            public uint Type;
            public MOUSEKEYBDHARDWAREINPUT Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public HARDWAREINPUT Hardware;
            [FieldOffset(0)]
            public KEYBDINPUT Keyboard;
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HARDWAREINPUT
        {
            public uint Msg;
            public ushort ParamL;
            public ushort ParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            public ushort Vk;
            public ushort Scan;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            public int X;
            public int Y;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }










        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        int x1 = 1092;
        int x2 = 695;
        int y1 = 1464;
        int y2 = 644;

        public Color GetColor(int x, int y)
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(IntPtr.Zero, hdc);
            return Color.FromArgb((int)pixel);
        }

        Bitmap screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
        public Color GetColorAt(Point location)
        {
            using (Graphics gdest = Graphics.FromImage(screenPixel))
            {
                using (Graphics gsrc = Graphics.FromHwnd(IntPtr.Zero))
                {
                    IntPtr hSrcDC = gsrc.GetHdc();
                    IntPtr hDC = gdest.GetHdc();
                    int retval = BitBlt(hDC, 0, 0, 1, 1, hSrcDC, location.X, location.Y, (int)CopyPixelOperation.SourceCopy);
                    gdest.ReleaseHdc();
                    gsrc.ReleaseHdc();
                }
            }

            return screenPixel.GetPixel(0, 0);
        }


        Point REF1;
        public Form1()
        {
            InitializeComponent();
        }

        KeyboardHook _listener;

        Process[] processes;

        private void Form1_Load(object sender, EventArgs e)
        {
            _listener = new KeyboardHook();
            _listener.Install();
            _listener.KeyDown += _listener_KeyDown;
            _listener.KeyUp += _listener_KeyUp;

            findColor = Color.FromArgb(255, 255, 255);
            processes = Process.GetProcesses();
            //processes = Process.GetProcessesByName("javaw");





            for (int i = 0; i < processes.Length; i++)
            {
            Debug.WriteLine(processes[i].ProcessName); //2 minecraft launcher processes, and 1 javaw

            }
        }

        private void _listener_KeyUp(KeyboardHook.VKeys key)
        {
            //toggle = false;
        }

        bool toggle = false;

        Color findColor; 
        Color detectColor;

        // These must be changed based on you minecraft GUI scale and resolution
        int[] positions = new int[] { 1118, 1173, 1226, 1282, 1334, 1389, 1444 };

        KeyboardHook.VKeys CurrentKey;
        private void _listener_KeyDown(KeyboardHook.VKeys key)
        {
            CurrentKey = key;
             if (CurrentKey == KeyboardHook.VKeys.LCONTROL)
             {
                Debug.WriteLine("Toggled On");
                toggle = true;
            }

            if (CurrentKey == KeyboardHook.VKeys.TAB)
            {
                Console.WriteLine("Toggled Off");
                toggle = false;
            }
        }

        public void Loop()
        {
            if (toggle)
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    if (reset)
                    {
                        reset = false;
                        break;
                    }
                    Caller(i);
                }
                Loop();
            }
        }

        bool reset;

        public void Caller(int i)
        {
            if (toggle)
            {
                detectColor = GetColorAt(new Point(positions[i], 666));
                if (detectColor.R == findColor.R && detectColor.G == findColor.G && detectColor.B == findColor.B)
                {
                    SetCursorPos(positions[i], 666);
                    Click(positions[i], 666);
                    reset = true;
                }
            }
        }
        int savedX;
        public void Click(int x, int y)
        {
            if (x == savedX)
            {
                return;
            }
                savedX = x;
        }

        public void RunKey(KeyboardHook.VKeys vkey)
        {
            SendKeyDown(vkey);
            SendKeyPress(vkey);
            SendKeyUp(vkey);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Loop();
        }
    }
}
