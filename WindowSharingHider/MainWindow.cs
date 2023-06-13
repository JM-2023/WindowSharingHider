using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowSharingHider
{
    public partial class MainWindow : Form
    {
        public class WindowInfo
        {
            public String Title { get; set; }
            public IntPtr Handle { get; set; }
            public int ProcessId { get; set; }
            public Boolean stillExists = false;
            public override string ToString()
            {
                return Title;
            }
        }

        private List<(string Title, int ProcessId)> savedWindowTitles = new List<(string, int)>();

        public MainWindow()
        {
            InitializeComponent();
            var timer = new Timer();
            timer.Interval = 500;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        Boolean flagToPreserveSettings = false;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (WindowInfo window in windowListCheckBox.Items) window.stillExists = false;
            var currWindows = WindowHandler.GetVisibleWindows();
            foreach (var window in currWindows)
            {
                var existingWindow = windowListCheckBox.Items.Cast<WindowInfo>().FirstOrDefault(i => i.Handle == window.Key);

                if (existingWindow != null)
                {
                    existingWindow.stillExists = true;
                    existingWindow.Title = window.Value;
                }
                else
                {
                    GetWindowThreadProcessId(window.Key, out int processId);
                    var newWindow = new WindowInfo { Title = window.Value, Handle = window.Key, ProcessId = processId, stillExists = true };
                    windowListCheckBox.Items.Add(newWindow);
                    // Set the status of the new window based on the savedWindowTitles list
                    // Check if the current window (either by title or process ID) exists in the savedWindowTitles list
                    if (savedWindowTitles.Any(item => item.Title == window.Value || item.ProcessId == processId))
                    {
                        // Get the index of the new window in the windowListCheckBox
                        var index = windowListCheckBox.Items.IndexOf(newWindow);
                        // Set the status of the new window in the windowListCheckBox to checked (true)
                        windowListCheckBox.SetItemChecked(index, true);

                        // Cancel and reapply the SetWindowDisplayAffinity to prevent black windows error
                        WindowHandler.SetWindowDisplayAffinity(newWindow.Handle, 0x0); // Cancel
                        WindowHandler.SetWindowDisplayAffinity(newWindow.Handle, 0x11); // Reapply
                    }
                }
            }
            foreach (var window in windowListCheckBox.Items.Cast<WindowInfo>().ToArray()) if (window.stillExists == false) windowListCheckBox.Items.Remove(window);
            foreach (var window in windowListCheckBox.Items.Cast<WindowInfo>().ToArray())
            {
                var status = WindowHandler.GetWindowDisplayAffinity(window.Handle) > 0;
                var target = windowListCheckBox.GetItemChecked(windowListCheckBox.Items.IndexOf(window));
                if (target != status && flagToPreserveSettings)
                {
                    WindowHandler.SetWindowDisplayAffinity(window.Handle, target ? 0x11 : 0x0);
                    status = WindowHandler.GetWindowDisplayAffinity(window.Handle) > 0;

                    if (status)
                    {
                        if (!savedWindowTitles.Contains((window.Title, window.ProcessId)))
                        {
                            savedWindowTitles.Add((window.Title, window.ProcessId));
                        }
                    }
                    else
                    {
                        savedWindowTitles.Remove((window.Title, window.ProcessId));
                    }
                }
                windowListCheckBox.SetItemChecked(windowListCheckBox.Items.IndexOf(window), status);
            }
            flagToPreserveSettings = true;
        }
    }
}