namespace SimpleBlockchain.UI
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.ss_status_bar = new System.Windows.Forms.StatusStrip();
            this.tss_lbl_height = new System.Windows.Forms.ToolStripStatusLabel();
            this.tss_lbl_height_value = new System.Windows.Forms.ToolStripStatusLabel();
            this.tss_lbl_connected = new System.Windows.Forms.ToolStripStatusLabel();
            this.tss_lbl_connected_value = new System.Windows.Forms.ToolStripStatusLabel();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.ss_status_bar.SuspendLayout();
            this.SuspendLayout();
            // 
            // ss_status_bar
            // 
            this.ss_status_bar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(13)))), ((int)(((byte)(13)))));
            this.ss_status_bar.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.ss_status_bar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tss_lbl_height,
            this.tss_lbl_height_value,
            this.tss_lbl_connected,
            this.tss_lbl_connected_value});
            this.ss_status_bar.Location = new System.Drawing.Point(0, 398);
            this.ss_status_bar.Name = "ss_status_bar";
            this.ss_status_bar.Size = new System.Drawing.Size(636, 25);
            this.ss_status_bar.TabIndex = 0;
            this.ss_status_bar.Text = "ss_status_bar";
            // 
            // tss_lbl_height
            // 
            this.tss_lbl_height.ForeColor = System.Drawing.Color.White;
            this.tss_lbl_height.Name = "tss_lbl_height";
            this.tss_lbl_height.Size = new System.Drawing.Size(57, 20);
            this.tss_lbl_height.Text = "Height:";
            // 
            // tss_lbl_height_value
            // 
            this.tss_lbl_height_value.ForeColor = System.Drawing.Color.White;
            this.tss_lbl_height_value.Name = "tss_lbl_height_value";
            this.tss_lbl_height_value.Size = new System.Drawing.Size(45, 20);
            this.tss_lbl_height_value.Text = "0/0/0";
            // 
            // tss_lbl_connected
            // 
            this.tss_lbl_connected.ForeColor = System.Drawing.Color.White;
            this.tss_lbl_connected.Name = "tss_lbl_connected";
            this.tss_lbl_connected.Size = new System.Drawing.Size(83, 20);
            this.tss_lbl_connected.Text = "Connected:";
            // 
            // tss_lbl_connected_value
            // 
            this.tss_lbl_connected_value.ForeColor = System.Drawing.Color.White;
            this.tss_lbl_connected_value.Name = "tss_lbl_connected_value";
            this.tss_lbl_connected_value.Size = new System.Drawing.Size(17, 20);
            this.tss_lbl_connected_value.Text = "0";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(13)))), ((int)(((byte)(13)))));
            this.ClientSize = new System.Drawing.Size(636, 423);
            this.Controls.Add(this.ss_status_bar);
            this.Name = "MainForm";
            this.Text = "MainForm";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ss_status_bar.ResumeLayout(false);
            this.ss_status_bar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip ss_status_bar;
        private System.Windows.Forms.ToolStripStatusLabel tss_lbl_height;
        private System.Windows.Forms.ToolStripStatusLabel tss_lbl_height_value;
        private System.Windows.Forms.ToolStripStatusLabel tss_lbl_connected;
        private System.Windows.Forms.ToolStripStatusLabel tss_lbl_connected_value;
        private System.Windows.Forms.Timer timer1;
    }
}