using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace MainView.Framework
{
    public class EnumProcessHelper
    {
        #region public properties

        public struct ProcessInfo
        {
            public int ProcessId;
            public string ProcessName;
            public string ProcessWindowTitle;
            public string ProcessPath;
            public BitmapSource ProcessIcon;
            public IntPtr MainWindowHandle;
        }

        public List<ProcessInfo> ProcessList;

        #endregion

        public EnumProcessHelper()
        {
            ProcessList = new List<ProcessInfo>();
        }

        #region public method

        /// <summary>
        /// Enum all processes window.
        /// </summary>
        /// <returns></returns>
        public List<ProcessInfo> EnumWindows()
        {
            Process[] processes = Process.GetProcesses();
            foreach (var p in processes)
            {
                if (string.IsNullOrEmpty(p.MainWindowTitle))
                    continue;

                if (!WindowEnumerationHelper.IsWindowValidForCapture(p.MainWindowHandle))
                    continue;

                if (ProcessList.Exists(o => o.ProcessId == p.Id))
                    continue;

                ProcessList.Add(new ProcessInfo
                {
                    ProcessId = p.Id,
                    ProcessName = p.ProcessName,
                    ProcessWindowTitle = p.MainWindowTitle,
                    ProcessIcon = GetWindowIcon(p.MainWindowHandle),
                    MainWindowHandle = p.MainWindowHandle
                });
            }

            ProcessList.Sort((a, b) => a.ProcessId - b.ProcessId);

            return ProcessList;
        }

        /// <summary>
        /// Get process path.
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        public string GetProcessPath(int processId)
        {
            string result = "";
            try
            {
                string query = $"SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = {processId}";

                using ManagementObjectSearcher mos = new ManagementObjectSearcher(query);
                using ManagementObjectCollection moc = mos.Get();
                string ExecutablePath = (from mo in moc.Cast<ManagementObject>() select mo["ExecutablePath"]).First()?.ToString();

                result = ExecutablePath;

            }
            catch //(Exception ex)
            {
                //ex.HandleException();
            }
            return result;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Get process window icon.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        private BitmapSource GetWindowIcon(IntPtr handle)
        {
            if (SendMessageTimeout(handle,
                Wm.Geticon,
                new IntPtr(0),
                new IntPtr(0),
                SendMessageTimeoutFlags.AbortIfHung | SendMessageTimeoutFlags.Block,
                500,
                out var hIcon) == IntPtr.Zero)
            {
                hIcon = IntPtr.Zero;
            }

            Icon result = null;
            if (hIcon != IntPtr.Zero)
            {
                result = Icon.FromHandle(hIcon);
            }
            else
            {
                // Fetch icon from window class.
                if (IntPtr.Size == 8)
                {
                    hIcon = GetClassLong64(handle, (int)ClassLong.Icon);
                }
                else
                {
                    hIcon = new IntPtr(GetClassLong32(handle, (int)ClassLong.Icon));
                }

                if (hIcon.ToInt64() != 0)
                {
                    result = Icon.FromHandle(hIcon);
                }
            }

            return result != null
                ? Imaging.CreateBitmapSourceFromHIcon(result.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
                : null;
        }

        #endregion

        #region native methods and structs

        [Flags]
        public enum SendMessageTimeoutFlags : uint
        {
            AbortIfHung = 2,
            Block = 1,
            Normal = 0
        }

        /// <summary>
        /// Native Windows Message codes.
        /// </summary>
        internal static class Wm
        {
            public const int Geticon = 0x7f;
            public const int Lbuttondown = 0x0201;
            public const int Lbuttonup = 0x0202;
            public const int Lbuttondblclk = 0x0203;
            public const int Rbuttondown = 0x0204;
            public const int Rbuttonup = 0x0205;
            public const int Rbuttondblclk = 0x0206;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam, SendMessageTimeoutFlags flags, uint timeout,
            out IntPtr result);

        public enum ClassLong
        {
            Icon = -14,
            IconSmall = -34
        }

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtrW")]
        private static extern IntPtr GetClassLong64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetClassLongW")]
        private static extern int GetClassLong32(IntPtr hWnd, int nIndex);

        #endregion
    }

    static class WindowEnumerationHelper
    {
        enum GetAncestorFlags
        {
            // Retrieves the parent window. This does not include the owner, as it does with the GetParent function.
            GetParent = 1,
            // Retrieves the root window by walking the chain of parent windows.
            GetRoot = 2,
            // Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.
            GetRootOwner = 3
        }

        public enum GWL
        {
            GWL_WNDPROC = (-4),
            GWL_HINSTANCE = (-6),
            GWL_HWNDPARENT = (-8),
            GWL_STYLE = (-16),
            GWL_EXSTYLE = (-20),
            GWL_USERDATA = (-21),
            GWL_ID = (-12)
        }

        [Flags]
        private enum WindowStyles : uint
        {
            WS_BORDER = 0x800000,
            WS_CAPTION = 0xc00000,
            WS_CHILD = 0x40000000,
            WS_CLIPCHILDREN = 0x2000000,
            WS_CLIPSIBLINGS = 0x4000000,
            WS_DISABLED = 0x8000000,
            WS_DLGFRAME = 0x400000,
            WS_GROUP = 0x20000,
            WS_HSCROLL = 0x100000,
            WS_MAXIMIZE = 0x1000000,
            WS_MAXIMIZEBOX = 0x10000,
            WS_MINIMIZE = 0x20000000,
            WS_MINIMIZEBOX = 0x20000,
            WS_OVERLAPPED = 0x0,
            WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_SIZEFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
            WS_POPUP = 0x80000000u,
            WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
            WS_SIZEFRAME = 0x40000,
            WS_SYSMENU = 0x80000,
            WS_TABSTOP = 0x10000,
            WS_VISIBLE = 0x10000000,
            WS_VSCROLL = 0x200000
        }

        enum DWMWINDOWATTRIBUTE : uint
        {
            NCRenderingEnabled = 1,
            NCRenderingPolicy,
            TransitionsForceDisabled,
            AllowNCPaint,
            CaptionButtonBounds,
            NonClientRtlLayout,
            ForceIconicRepresentation,
            Flip3DPolicy,
            ExtendedFrameBounds,
            HasIconicBitmap,
            DisallowPeek,
            ExcludedFromPeek,
            Cloak,
            Cloaked,
            FreezeRepresentation
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true)]
        static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        // This static method is required because Win32 does not support
        // GetWindowLongPtr directly.
        // http://pinvoke.net/default.aspx/user32/GetWindowLong.html
        static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLongPtr32(hWnd, nIndex);
        }

        [DllImport("dwmapi.dll")]
        static extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out bool pvAttribute, int cbAttribute);

        public static bool IsWindowValidForCapture(IntPtr hwnd)
        {
            if (hwnd.ToInt64() == 0)
            {
                return false;
            }

            if (hwnd == GetShellWindow())
            {
                return false;
            }

            if (!IsWindowVisible(hwnd))
            {
                return false;
            }

            if (GetAncestor(hwnd, GetAncestorFlags.GetRoot) != hwnd)
            {
                return false;
            }

            var style = (WindowStyles)(uint)GetWindowLongPtr(hwnd, (int)GWL.GWL_STYLE).ToInt64();
            if (style.HasFlag(WindowStyles.WS_DISABLED))
            {
                return false;
            }

            var hrTemp = DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.Cloaked, out bool cloaked, Marshal.SizeOf<bool>());
            if (hrTemp == 0 && cloaked)
            {
                return false;
            }

            return true;
        }
    }

    public static class Win32Processes
    {
        /// <summary>
        /// Find out what process(es) have a lock on the specified file.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <returns>Processes locking the file</returns>
        /// <remarks>See also:
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa373661(v=vs.85).aspx
        /// http://wyupdate.googlecode.com/svn-history/r401/trunk/frmFilesInUse.cs (no copyright in code at time of viewing)
        /// </remarks>
        public static List<Process> GetProcessesLockingFile(string path)
        {
            uint handle;
            string key = Guid.NewGuid().ToString();
            int res = RmStartSession(out handle, 0, key);

            if (res != 0) throw new Exception("Could not begin restart session.  Unable to determine file locker.");

            try
            {
                const int MORE_DATA = 234;
                uint pnProcInfoNeeded, pnProcInfo = 0, lpdwRebootReasons = RmRebootReasonNone;

                string[] resources = { path }; // Just checking on one resource.

                res = RmRegisterResources(handle, (uint)resources.Length, resources, 0, null, 0, null);

                if (res != 0) throw new Exception("Could not register resource.");

                //Note: there's a race condition here -- the first call to RmGetList() returns
                //      the total number of process. However, when we call RmGetList() again to get
                //      the actual processes this number may have increased.
                res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

                if (res == MORE_DATA)
                {
                    return EnumerateProcesses(pnProcInfoNeeded, handle, lpdwRebootReasons);
                }
                else if (res != 0) throw new Exception("Could not list processes locking resource. Failed to get size of result.");
            }
            finally
            {
                RmEndSession(handle);
            }

            return new List<Process>();
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }

        const int RmRebootReasonNone = 0;
        const int CCH_RM_MAX_APP_NAME = 255;
        const int CCH_RM_MAX_SVC_NAME = 63;

        public enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)] public string strAppName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)] public string strServiceShortName;

            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)] public bool bRestartable;
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        static extern int RmRegisterResources(uint pSessionHandle, uint nFiles, string[] rgsFilenames,
            uint nApplications, [In] RM_UNIQUE_PROCESS[] rgApplications, uint nServices,
            string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
        static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll")]
        static extern int RmEndSession(uint pSessionHandle);

        [DllImport("rstrtmgr.dll")]
        static extern int RmGetList(uint dwSessionHandle, out uint pnProcInfoNeeded,
            ref uint pnProcInfo, [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
            ref uint lpdwRebootReasons);

        private static List<Process> EnumerateProcesses(uint pnProcInfoNeeded, uint handle, uint lpdwRebootReasons)
        {
            var processes = new List<Process>(10);
            // Create an array to store the process results
            var processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
            var pnProcInfo = pnProcInfoNeeded;

            // Get the list
            var res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);

            if (res != 0) throw new Exception("Could not list processes locking resource.");
            for (int i = 0; i < pnProcInfo; i++)
            {
                try
                {
                    processes.Add(Process.GetProcessById(processInfo[i].Process.dwProcessId));
                }
                catch (ArgumentException) { } // catch the error -- in case the process is no longer running
            }
            return processes;
        }
    }

    public static class NativeMethod
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }
    }
}
