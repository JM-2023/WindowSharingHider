using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowSharingHider
{
    public partial class MainWindow : Form
    {
        private List<WindowInfo> currentWindows = new List<WindowInfo>();

        // Track which PIDs/titles are hidden
        private HashSet<int> hiddenPIDs = new HashSet<int>();
        private HashSet<string> hiddenTitles = new HashSet<string>();

        // Lock state to prevent manual toggling in the UI
        private bool isLocked = false;

        // Prevent re-entrant ItemChecked calls
        private bool isInItemCheckedHandler = false;

        public MainWindow()
        {
            InitializeComponent();

            // e.g. 2 seconds for quicker detection.
            timer.Interval = 2000;
            timer.Start();

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
            try
            {
                RefreshWindowList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Timer refresh error:\n" + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Enumerate visible windows. If a window’s PID or Title is in our hidden sets, 
        /// immediately hide it. Also add that PID and Title to sets if it was hidden automatically.
        /// Then update the ListView.
        /// </summary>
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
                    existing = new WindowInfo
                    {
                        Title = title,
                        Handle = handle,
                        ProcessId = pid
                    };
                }
                else
                {
                    // Update title if changed
                    existing.Title = title;
                }

                existing.StillExists = true;
                updatedList.Add(existing);

                // (A) Auto-hide logic (whether locked or not)
                bool isInHiddenSet = hiddenPIDs.Contains(pid) || hiddenTitles.Contains(title);
                if (isInHiddenSet)
                {
                    try
                    {
                        // Check if already hidden before calling again
                        int currentAffinity = WindowHandler.GetWindowDisplayAffinity(handle);
                        if (currentAffinity != 0x11)
                        {
                            WindowHandler.SetWindowDisplayAffinity(handle, 0x11);
                        }

                        if (currentAffinity == 0x11)
                        {
                            WindowHandler.SetWindowDisplayAffinity(handle, 0x0);
                            WindowHandler.SetWindowDisplayAffinity(handle, 0x11);
                        }

                        // (B) Also add this PID and title to the sets,
                        //     so next time the same PID or changed title reappears, we hide it.
                        hiddenPIDs.Add(pid);
                        hiddenTitles.Add(title);
                    }
                    catch
                    {
                        // Optionally ignore or log
                    }
                }
            }

            currentWindows = updatedList;
            ApplyFilter();
        }

        /// <summary>
        /// Rebuild the ListView based on the current filter and hidden sets.
        /// </summary>
        private void ApplyFilter()
        {
            // Avoid re-entrancy
            windowListView.ItemChecked -= windowListView_ItemChecked;

            windowListView.BeginUpdate();
            windowListView.Items.Clear();

            string filter = txtFilter.Text?.ToLowerInvariant() ?? "";
            var filtered = string.IsNullOrWhiteSpace(filter)
                ? currentWindows
                : currentWindows.Where(w => w.Title.ToLowerInvariant().Contains(filter)).ToList();

            foreach (var w in filtered)
            {
                var item = new ListViewItem(w.Title);
                item.SubItems.Add(w.ProcessId.ToString());
                item.Tag = w;

                // If the sets contain either the PID or the title, show as checked
                bool shouldBeHidden = hiddenPIDs.Contains(w.ProcessId) || hiddenTitles.Contains(w.Title);
                item.Checked = shouldBeHidden;

                windowListView.Items.Add(item);
            }

            windowListView.EndUpdate();
            windowListView.ItemChecked += windowListView_ItemChecked;

            UpdateStatus();
        }

        /// <summary>
        /// Fires when the user manually toggles the checkbox (only if unlocked).
        /// </summary>
        private void windowListView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (isInItemCheckedHandler) return;
            isInItemCheckedHandler = true;

            try
            {
                if (isLocked)
                {
                    // Revert user’s attempt if locked
                    e.Item.Checked = !e.Item.Checked;
                    return;
                }

                if (e.Item.Tag is WindowInfo w)
                {
                    bool target = e.Item.Checked; // user wants hidden if checked
                    try
                    {
                        WindowHandler.SetWindowDisplayAffinity(w.Handle, target ? 0x11 : 0x0);

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
                        MessageBox.Show("Error changing window state: " + ex.Message,
                                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        e.Item.Checked = !target;
                    }
                }
            }
            finally
            {
                isInItemCheckedHandler = false;
            }
        }

        private void UpdateStatus()
        {
            int totalWindows = currentWindows.Count;
            int hiddenCount = hiddenPIDs.Count + hiddenTitles.Count; 

            lblStatus.Text = $"Total Windows: {totalWindows}, Hidden: {hiddenCount}";
        }

        private void btnShowAll_Click(object sender, EventArgs e)
        {
            hiddenPIDs.Clear();
            hiddenTitles.Clear();

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

            ApplyFilter();
        }

        private void btnLockToggle_Click(object sender, EventArgs e)
        {
            isLocked = !isLocked;
            btnLockToggle.Text = isLocked ? "Unlock" : "Lock";

            // Prevent user from manually toggling if locked
            windowListView.Enabled = !isLocked;

            // Optional: Keep or stop the timer. If you want to keep auto-hiding new windows, keep it running.
            // if (isLocked) timer.Stop(); else timer.Start();
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
    }
}
