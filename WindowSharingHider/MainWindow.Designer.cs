namespace WindowSharingHider
{
    partial class MainWindow
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListView windowListView;
        private System.Windows.Forms.ColumnHeader colTitle;
        private System.Windows.Forms.ColumnHeader colPID;
        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton btnRefresh;
        private System.Windows.Forms.ToolStripButton btnSaveSettings;
        private System.Windows.Forms.ToolStripButton btnLoadSettings;
        private System.Windows.Forms.ToolStripTextBox txtFilter;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.windowListView = new System.Windows.Forms.ListView();
            this.colTitle = new System.Windows.Forms.ColumnHeader();
            this.colPID = new System.Windows.Forms.ColumnHeader();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.btnRefresh = new System.Windows.Forms.ToolStripButton();
            this.btnSaveSettings = new System.Windows.Forms.ToolStripButton();
            this.btnLoadSettings = new System.Windows.Forms.ToolStripButton();
            this.txtFilter = new System.Windows.Forms.ToolStripTextBox();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();

            // 
            // windowListView
            // 
            this.windowListView.CheckBoxes = true;
            this.windowListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colTitle,
            this.colPID});
            this.windowListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.windowListView.FullRowSelect = true;
            this.windowListView.HideSelection = false;
            this.windowListView.Location = new System.Drawing.Point(0, 25);
            this.windowListView.Name = "windowListView";
            this.windowListView.Size = new System.Drawing.Size(500, 300);
            this.windowListView.TabIndex = 0;
            this.windowListView.UseCompatibleStateImageBehavior = false;
            this.windowListView.View = System.Windows.Forms.View.Details;
            this.windowListView.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.windowListView_ItemChecked);

            // 
            // colTitle
            // 
            this.colTitle.Text = "Window Title";
            this.colTitle.Width = 350;
            // 
            // colPID
            // 
            this.colPID.Text = "PID";
            this.colPID.Width = 80;

            // 
            // timer
            // 
            this.timer.Interval = 2000; // less frequent
            this.timer.Tick += new System.EventHandler(this.timer_Tick);

            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnRefresh,
            this.btnSaveSettings,
            this.btnLoadSettings,
            this.txtFilter});
            this.toolStrip.Location = new System.Drawing.Point(0,0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(500, 25);
            this.toolStrip.TabIndex = 1;

            // 
            // btnRefresh
            // 
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

            // 
            // btnSaveSettings
            // 
            this.btnSaveSettings.Text = "Save";
            this.btnSaveSettings.Click += new System.EventHandler(this.btnSaveSettings_Click);

            // 
            // btnLoadSettings
            // 
            this.btnLoadSettings.Text = "Load";
            this.btnLoadSettings.Click += new System.EventHandler(this.btnLoadSettings_Click);

            // 
            // txtFilter
            // 
            this.txtFilter.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.Size = new System.Drawing.Size(150, 25);
            this.txtFilter.ToolTipText = "Filter by title...";
            this.txtFilter.TextChanged += new System.EventHandler(this.txtFilter_TextChanged);

            // 
            // statusStrip
            // 
            this.statusStrip.Items.Add(this.lblStatus);
            this.statusStrip.Location = new System.Drawing.Point(0, 325);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(500, 22);

            // 
            // lblStatus
            // 
            this.lblStatus.Text = "Ready";

            // 
            // MainWindow
            // 
            this.ClientSize = new System.Drawing.Size(500, 347);
            this.Controls.Add(this.windowListView);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.statusStrip);
            this.Name = "MainWindow";
            this.Text = "Window Sharing Hider";
        }
    }
}
