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
        private List<WindowInfo> currentWindows = new List<WindowInfo>();
        
        // Use both sets to track hidden states:
        private HashSet<int> hiddenPIDs = new HashSet<int>();
        private HashSet<string> hiddenTitles = new HashSet<string>();

        public MainWindow()
        {
            InitializeComponent();
            RefreshWindowList();
            timer.Start();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshWindowList();
        }

        private void txtFilter_TextChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            RefreshWindowList();
        }

        private void RefreshWindowList()
        {
            var visibleWindows = WindowHandler.GetVisibleWindows();
            var updatedList = new List<WindowInfo>();

            foreach (var kvp in visibleWindows)
            {
                var handle = kvp.Key;
                var title = kvp.Value;
                GetWindowThreadProcessId(handle, out int pid);

                var existing = currentWindows.FirstOrDefault(w => w.Handle == handle);
                if (existing == null)
                {
                    existing = new WindowInfo { Title = title, Handle = handle, ProcessId = pid };
                }
                else
                {
                    existing.Title = title;
                }
                existing.StillExists = true;
                updatedList.Add(existing);
            }

            currentWindows = updatedList;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            windowListView.BeginUpdate();
            windowListView.Items.Clear();

            string filter = txtFilter.Text.ToLowerInvariant();
            var filtered = string.IsNullOrWhiteSpace(filter)
                ? currentWindows
                : currentWindows.Where(w => w.Title.ToLowerInvariant().Contains(filter)).ToList();

            foreach (var w in filtered)
            {
                var item = new ListViewItem(w.Title);
                item.SubItems.Add(w.ProcessId.ToString());
                item.Tag = w;

                // Determine if this window should be hidden
                bool shouldBeHidden = hiddenPIDs.Contains(w.ProcessId) || hiddenTitles.Contains(w.Title);
                bool currentlyHidden = WindowHandler.GetWindowDisplayAffinity(w.Handle) > 0;

                // If it should be hidden but isn't, hide it
                if (shouldBeHidden && !currentlyHidden)
                {
                    WindowHandler.SetWindowDisplayAffinity(w.Handle, 0x11);
                }
                // If it should not be hidden but is, show it
                else if (!shouldBeHidden && currentlyHidden)
                {
                    WindowHandler.SetWindowDisplayAffinity(w.Handle, 0x0);
                }

                item.Checked = shouldBeHidden;
                windowListView.Items.Add(item);
            }

            windowListView.EndUpdate();
            UpdateStatus();
        }

        private void windowListView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (e.Item.Tag is WindowInfo w)
            {
                bool target = e.Item.Checked;
                try
                {
                    WindowHandler.SetWindowDisplayAffinity(w.Handle, target ? 0x11 : 0x0);
                    if (target)
                    {
                        // Add both PID and Title to hidden sets
                        hiddenPIDs.Add(w.ProcessId);
                        hiddenTitles.Add(w.Title);
                    }
                    else
                    {
                        // Remove PID and Title from hidden sets
                        hiddenPIDs.Remove(w.ProcessId);
                        hiddenTitles.Remove(w.Title);
                    }
                    UpdateStatus();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error changing window state: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    e.Item.Checked = !target; // revert
                }
            }
        }

        private void UpdateStatus()
        {
            int hiddenCount = hiddenPIDs.Count + hiddenTitles.Count;
            lblStatus.Text = $"Total Windows: {currentWindows.Count}, Hidden (by PID or Title): {hiddenCount}";
        }

        private void btnShowAll_Click(object sender, EventArgs e)
        {
            // Cancel all hiding
            hiddenPIDs.Clear();
            hiddenTitles.Clear();

            // Show all windows
            foreach (var w in currentWindows)
            {
                WindowHandler.SetWindowDisplayAffinity(w.Handle, 0x0);
            }
            ApplyFilter();
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
    }
}
