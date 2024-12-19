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
        private List<(string Title, int ProcessId)> savedWindowTitles = new List<(string, int)>();
        
        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            RefreshWindowList();
            timer.Start();
        }

        private void LoadSettings()
        {
            savedWindowTitles = SettingsManager.LoadHiddenWindows();
        }

        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            SettingsManager.SaveHiddenWindows(savedWindowTitles);
            lblStatus.Text = "Settings saved.";
        }

        private void btnLoadSettings_Click(object sender, EventArgs e)
        {
            savedWindowTitles = SettingsManager.LoadHiddenWindows();
            lblStatus.Text = "Settings loaded.";
            ApplyCurrentSettings();
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
            // We can choose to automatically refresh windows or only if needed.
            // For now, let's refresh.
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
                // Check if currently hidden or not
                bool shouldBeChecked = savedWindowTitles.Any(sw => sw.ProcessId == w.ProcessId || sw.Title == w.Title);
                // Attempt to read current display affinity:
                bool currentlyHidden = WindowHandler.GetWindowDisplayAffinity(w.Handle) > 0;
                // If saved says it should be hidden but not currently, fix it:
                if (shouldBeChecked && !currentlyHidden)
                {
                    WindowHandler.SetWindowDisplayAffinity(w.Handle, 0x11);
                }
                item.Checked = WindowHandler.GetWindowDisplayAffinity(w.Handle) > 0;
                windowListView.Items.Add(item);
            }

            windowListView.EndUpdate();
            UpdateStatus();
        }

        private void ApplyCurrentSettings()
        {
            // Re-apply saved settings to current windows
            foreach (var w in currentWindows)
            {
                bool shouldHide = savedWindowTitles.Any(sw => sw.ProcessId == w.ProcessId || sw.Title == w.Title);
                WindowHandler.SetWindowDisplayAffinity(w.Handle, shouldHide ? 0x11 : 0x0);
            }
            ApplyFilter();
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
                        // Add or update saved titles
                        if (!savedWindowTitles.Any(x => x.ProcessId == w.ProcessId))
                            savedWindowTitles.Add((w.Title, w.ProcessId));
                    }
                    else
                    {
                        savedWindowTitles.RemoveAll(x => x.ProcessId == w.ProcessId || x.Title == w.Title);
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
            int hiddenCount = savedWindowTitles.Count;
            lblStatus.Text = $"Total Windows: {currentWindows.Count}, Hidden: {hiddenCount}";
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
    }
}
