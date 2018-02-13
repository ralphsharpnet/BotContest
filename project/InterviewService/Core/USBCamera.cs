using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing;

namespace SpeechToTextWPF
{
    /// <summary>
    /// USB camera handler
    /// </summary>
    public class USBCamera
    {
        /// <summary>
        /// Message to handle
        /// </summary>
        private const int WM_USER = 0x400;
        private const int WS_CHILD = 0x40000000;
        private const int WS_VISIBLE = 0x10000000;
        private const int WM_CAP_START = WM_USER;
        private const int WM_CAP_STOP = WM_CAP_START + 68;
        private const int WM_CAP_DRIVER_CONNECT = WM_CAP_START + 10;
        private const int WM_CAP_DRIVER_DISCONNECT = WM_CAP_START + 11;
        private const int WM_CAP_SAVEDIB = WM_CAP_START + 25;
        private const int WM_CAP_GRAB_FRAME = WM_CAP_START + 60;
        private const int WM_CAP_SEQUENCE = WM_CAP_START + 62;
        private const int WM_CAP_FILE_SET_CAPTURE_FILEA = WM_CAP_START + 20;
        private const int WM_CAP_SEQUENCE_NOFILE = WM_CAP_START + 63;
        private const int WM_CAP_SET_OVERLAY = WM_CAP_START + 51;
        private const int WM_CAP_SET_PREVIEW = WM_CAP_START + 50;
        private const int WM_CAP_SET_CALLBACK_VIDEOSTREAM = WM_CAP_START + 6;
        private const int WM_CAP_SET_CALLBACK_ERROR = WM_CAP_START + 2;
        private const int WM_CAP_SET_CALLBACK_STATUSA = WM_CAP_START + 3;
        private const int WM_CAP_SET_CALLBACK_FRAME = WM_CAP_START + 5;
        private const int WM_CAP_SET_SCALE = WM_CAP_START + 53;
        private const int WM_CAP_SET_PREVIEWRATE = WM_CAP_START + 52;

        /// <summary>
        /// Handle controller of the Camera Capture device
        /// </summary>
        private IntPtr hWndC;

        /// <summary>
        /// Handle of the control where the camera display the images
        /// </summary>
        private IntPtr mControlPtr;

        /// <summary>
        /// Width of the panel camera
        /// </summary>
        private int mWidth;

        /// <summary>
        /// Height of the panel camera
        /// </summary>
        private int mHeight;

        /// <summary>
        /// left position of the panel camera
        /// </summary>
        private int mLeft;

        /// <summary>
        /// top position of the panel camera
        /// </summary>
        private int mTop;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="handle">control handle </param>
        /// <param name="left">x or left position</param>
        /// <param name="top">y or top position</param>
        /// <param name="width">width </param>
        /// <param name="height">height </param>
        public USBCamera(IntPtr handle, int left, int top, int width, int height)
        {
            mControlPtr = handle;
            mWidth = width;
            mHeight = height;
            mLeft = left;
            mTop = top;
        }

        [DllImport("avicap32.dll")]
        private static extern IntPtr capCreateCaptureWindowA(byte[] lpszWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, int nID);
        [DllImport("avicap32.dll")]
        private static extern int capGetVideoFormat(IntPtr hWnd, IntPtr psVideoFormat, int wSize);
        [DllImport("User32.dll")]
        private static extern bool SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Start to ge the camera device
        /// </summary>
        public void Start()
        {
            byte[] lpszName = new byte[100];

            hWndC = capCreateCaptureWindowA(lpszName, WS_CHILD | WS_VISIBLE, mLeft, mTop, mWidth, mHeight, mControlPtr, 0);
            
            if (hWndC.ToInt32() != 0)
            {
                SendMessage(hWndC, WM_CAP_SET_CALLBACK_VIDEOSTREAM, IntPtr.Zero, IntPtr.Zero);
                SendMessage(hWndC, WM_CAP_SET_CALLBACK_ERROR, IntPtr.Zero, IntPtr.Zero);
                SendMessage(hWndC, WM_CAP_SET_CALLBACK_STATUSA, IntPtr.Zero, IntPtr.Zero);
                bool result = SendMessage(hWndC, WM_CAP_DRIVER_CONNECT, IntPtr.Zero, IntPtr.Zero);
                SendMessage(hWndC, WM_CAP_SET_SCALE, (IntPtr)1, IntPtr.Zero);
                SendMessage(hWndC, WM_CAP_SET_PREVIEWRATE, (IntPtr)66, IntPtr.Zero);
                SendMessage(hWndC, WM_CAP_SET_OVERLAY, (IntPtr)1, IntPtr.Zero);
                SendMessage(hWndC, WM_CAP_SET_PREVIEW, (IntPtr)1, IntPtr.Zero);
                
                if (result)
                {
                    Console.WriteLine("ERROR: Cannot creater device");
                }
            }
        }
        /// <summary>
        /// Release the camera
        /// </summary>
        public void Stop()
        {
            bool result = SendMessage(hWndC, WM_CAP_DRIVER_DISCONNECT, IntPtr.Zero, IntPtr.Zero);
            if (result)
            {
                Console.WriteLine("ERROR: Cannot disconnect of device");
            }
        }

        ///<summary>
        ///Start to record camera 
        ///</summary>
        ///<param name="path">path where to save the avi file</param>
        public void capture(string path)
        {
            IntPtr hBmp = Marshal.StringToHGlobalAnsi(path);
            
            SendMessage(hWndC, WM_CAP_FILE_SET_CAPTURE_FILEA, IntPtr.Zero, hBmp);
            SendMessage(hWndC, WM_CAP_SEQUENCE, IntPtr.Zero, IntPtr.Zero);
        }

        ///<summary>
        ///Stop to capture
        ///</summary>
        public void StopCapture()
        {
            SendMessage(hWndC, WM_CAP_STOP, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
