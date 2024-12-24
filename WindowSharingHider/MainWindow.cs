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

            // (1) If you still want a timer, reduce refresh frequency or consider removing it.
            // Example: 10 seconds instead of 2 seconds
            timer.Interval = 10000; // was 2000
            timer.Start();

            // Or remove timer if you want manual refresh only:
            // timer.Stop();
            // timer.Dispose();

            RefreshWindowList();
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
                    existing.Title = title; // update title if changed
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

            string filter = txtFilter.Text?.ToLowerInvariant() ?? "";
            var filtered = string.IsNullOrWhiteSpace(filter)
                ? currentWindows
                : currentWindows.Where(w => w.Title.ToLowerInvariant().Contains(filter)).ToList();

            // (2) Just update the ListView check state based on our sets,
            // but DO NOT re-call SetWindowDisplayAffinity in here.
            foreach (var w in filtered)
            {
                var item = new ListViewItem(w.Title);
                item.SubItems.Add(w.ProcessId.ToString());
                item.Tag = w;

                // Should this window be hidden?
                bool shouldBeHidden = hiddenPIDs.Contains(w.ProcessId) || hiddenTitles.Contains(w.Title);

                // Set the checkbox state, but do not call SetWindowDisplayAffinity here.
                item.Checked = shouldBeHidden;

                windowListView.Items.Add(item);
            }

            windowListView.EndUpdate();

            UpdateStatus();
        }

        private void windowListView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            // (3) Only call SetWindowDisplayAffinity here—when the user explicitly checks/unchecks.
            if (e.Item.Tag is WindowInfo w)
            {
                bool target = e.Item.Checked; // user wants hidden if checked

                try
                {
                    // (A) Set the new display affinity once
                    WindowHandler.SetWindowDisplayAffinity(w.Handle, target ? 0x11 : 0x0);

                    // (B) Update our tracking sets
                    if (target)
                    {
                        hiddenPIDs.Add(w.ProcessId);
                        hiddenTitles.Add(w.Title);
                    }
                    else
                    {
                        hiddenPIDs.Remove(w.ProcessId);
                        hiddenTitles.Remove(w.Title);
                    }

                    UpdateStatus();
                }
                catch (Exception ex)
                {
                    // If something goes wrong, revert the checkbox to old state
                    MessageBox.Show("Error changing window state: " + ex.Message,
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    e.Item.Checked = !target;
                }
            }
        }

        private void UpdateStatus()
        {
            int totalWindows = currentWindows.Count;
            // "Hidden" = the number of unique PIDs plus unique titles. (This is simplistic.)
            // Or you could do something else, but we'll keep it short:
            int hiddenCount = hiddenPIDs.Count + hiddenTitles.Count;

            lblStatus.Text = $"Total Windows: {totalWindows}, Hidden (by PID/Title): {hiddenCount}";
        }

        private void btnShowAll_Click(object sender, EventArgs e)
        {
            // (4) "Show All" => remove everything from hidden sets, then forcibly show
            hiddenPIDs.Clear();
            hiddenTitles.Clear();

            // If you want to forcibly set affinity to 0 for all,
            // do it once here, not repeatedly in a loop:
            foreach (var w in currentWindows)
            {
                try
                {
                    WindowHandler.SetWindowDisplayAffinity(w.Handle, 0x0);
                }
                catch
                {
                    // ignore
                }
            }

            // Rebuild the UI now that everything is "shown"
            ApplyFilter();
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
    }
}
