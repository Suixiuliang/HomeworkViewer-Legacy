#nullable disable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace HomeworkViewer
{
    static class Program
    {
        private static Mutex mutex = null;

        [STAThread]
        static void Main()
        {
            const string appName = "HomeworkViewer";
            bool createdNew;

            mutex = new Mutex(true, appName, out createdNew);
            if (!createdNew)
            {
                ActivateExistingInstance();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new HomeworkViewer());

            mutex.ReleaseMutex();
        }

        private static void ActivateExistingInstance()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
            foreach (Process p in processes)
            {
                if (p.Id != currentProcess.Id)
                {
                    p.WaitForInputIdle(1000);
                    IntPtr hWnd = p.MainWindowHandle;
                    if (hWnd != IntPtr.Zero)
                    {
                        ShowWindow(hWnd, SW_RESTORE);
                        SetForegroundWindow(hWnd);
                    }
                    break;
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int SW_RESTORE = 9;
    }
}