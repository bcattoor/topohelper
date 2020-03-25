using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

// DON'T FORGET to update NuGet package "WixSharp". NuGet console:
// Update-Package WixSharp NuGet Manager UI: updates tab

namespace TopoHelper.WixSharpSetup
{
    public static class NotepadHelper
    {
        #region Methods

        /// <summary>
        /// </summary>
        /// <param name="message"> Message to display in notepad. </param>
        /// <param name="title">   Title the window needs. </param>
        /// <exception cref="InvalidOperationException">
        /// The process does not have a graphical interface.-or-An unknown error
        /// occurred. The process failed to enter an idle state.-or-The process
        /// has already exited. -or-No process is associated with this
        /// <see cref="T:System.Diagnostics.Process" /> object.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// You are trying to access the
        /// <see cref="P:System.Diagnostics.Process.MainWindowHandle" />
        /// property for a process that is running on a remote computer. This
        /// property is available only for processes that are running on the
        /// local computer.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        /// The platform is Windows 98 or Windows Millennium Edition (Windows
        /// Me); set
        /// <see cref="P:System.Diagnostics.ProcessStartInfo.UseShellExecute" />
        /// to false to access this property on Windows 98 and Windows Me.
        /// </exception>
        public static void ShowMessage(string message = null, string title = null)
        {
            var notepad = Process.Start(new ProcessStartInfo("notepad.exe"));
            if (notepad == null) return;
            notepad.WaitForInputIdle();

            if (!string.IsNullOrEmpty(title))
                SetWindowText(notepad.MainWindowHandle, title);

            if (string.IsNullOrEmpty(message)) return;
            var child = FindWindowEx(notepad.MainWindowHandle, new IntPtr(0), "Edit", null);
            SendMessage(child, 0x000C, 0, message);
        }

        [DllImport("user32.dll", EntryPoint = "FindWindowEx", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
            string lpszWindow);

        [DllImport("User32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Unicode)]
        private static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, string lParam);

        [DllImport("user32.dll", EntryPoint = "SetWindowText", CharSet = CharSet.Unicode)]
        private static extern int SetWindowText(IntPtr hWnd, string text);

        #endregion Methods
    }
}