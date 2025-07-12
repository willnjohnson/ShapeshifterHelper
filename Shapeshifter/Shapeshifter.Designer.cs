namespace Shapeshifter
{
    partial class ShapeShifter
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
            this.textBoxStepsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.textBoxInput = new System.Windows.Forms.TextBox();
            this.labelPaste = new System.Windows.Forms.Label();
            this.btnStartStop = new System.Windows.Forms.Button();
            this.labelResult = new System.Windows.Forms.Label();
            this.labelWaiting = new System.Windows.Forms.Label();
            this.labelTitle = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // textBoxStepsPanel
            // 
            this.textBoxStepsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxStepsPanel.AutoScroll = true;
            this.textBoxStepsPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxStepsPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.textBoxStepsPanel.ForeColor = System.Drawing.Color.White;
            this.textBoxStepsPanel.Location = new System.Drawing.Point(16, 413);
            this.textBoxStepsPanel.Name = "textBoxStepsPanel";
            this.textBoxStepsPanel.Size = new System.Drawing.Size(856, 256);
            this.textBoxStepsPanel.TabIndex = 5;
            this.textBoxStepsPanel.WrapContents = false;
            // 
            // textBoxInput
            // 
            this.textBoxInput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxInput.BackColor = System.Drawing.Color.DimGray;
            this.textBoxInput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxInput.ForeColor = System.Drawing.Color.White;
            this.textBoxInput.Location = new System.Drawing.Point(16, 81);
            this.textBoxInput.MaxLength = 200000;
            this.textBoxInput.Multiline = true;
            this.textBoxInput.Name = "textBoxInput";
            this.textBoxInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxInput.Size = new System.Drawing.Size(856, 252);
            this.textBoxInput.TabIndex = 0;
            this.textBoxInput.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // labelPaste
            // 
            this.labelPaste.AutoSize = true;
            this.labelPaste.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.labelPaste.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.labelPaste.Location = new System.Drawing.Point(12, 58);
            this.labelPaste.Name = "labelPaste";
            this.labelPaste.Size = new System.Drawing.Size(238, 20);
            this.labelPaste.TabIndex = 1;
            this.labelPaste.Text = "Paste ShapeShifter HTML code:";
            this.labelPaste.Click += new System.EventHandler(this.label1_Click);
            // 
            // btnStartStop
            // 
            this.btnStartStop.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnStartStop.BackColor = System.Drawing.Color.DimGray;
            this.btnStartStop.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnStartStop.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.btnStartStop.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.btnStartStop.Location = new System.Drawing.Point(16, 339);
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Size = new System.Drawing.Size(109, 48);
            this.btnStartStop.TabIndex = 2;
            this.btnStartStop.Text = "Start";
            this.btnStartStop.UseVisualStyleBackColor = false;
            this.btnStartStop.Click += new System.EventHandler(this.startStopButton_Click);
            // 
            // labelResult
            // 
            this.labelResult.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelResult.AutoSize = true;
            this.labelResult.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.labelResult.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.labelResult.Location = new System.Drawing.Point(12, 390);
            this.labelResult.Name = "labelResult";
            this.labelResult.Size = new System.Drawing.Size(59, 20);
            this.labelResult.TabIndex = 4;
            this.labelResult.Text = "Result:";
            this.labelResult.Click += new System.EventHandler(this.label1_Click_1);
            // 
            // labelWaiting
            // 
            this.labelWaiting.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelWaiting.AutoSize = true;
            this.labelWaiting.ForeColor = System.Drawing.Color.Transparent;
            this.labelWaiting.Location = new System.Drawing.Point(131, 358);
            this.labelWaiting.Name = "labelWaiting";
            this.labelWaiting.Size = new System.Drawing.Size(71, 13);
            this.labelWaiting.TabIndex = 6;
            this.labelWaiting.Text = "Calculating ...";
            this.labelWaiting.Visible = false;
            this.labelWaiting.Click += new System.EventHandler(this.label1_Click_2);
            // 
            // labelTitle
            // 
            this.labelTitle.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F);
            this.labelTitle.ForeColor = System.Drawing.Color.White;
            this.labelTitle.Location = new System.Drawing.Point(321, 15);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(258, 31);
            this.labelTitle.TabIndex = 7;
            this.labelTitle.Text = "ShapeShifter Helper";
            this.labelTitle.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // ShapeShifter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(884, 681);
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.labelWaiting);
            this.Controls.Add(this.textBoxStepsPanel);
            this.Controls.Add(this.labelResult);
            this.Controls.Add(this.btnStartStop);
            this.Controls.Add(this.labelPaste);
            this.Controls.Add(this.textBoxInput);
            this.Name = "ShapeShifter";
            this.ShowIcon = false;
            this.Text = "ShapeShifter Helper (A* Heuristic)";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxInput;
        private System.Windows.Forms.Label labelPaste;
        private System.Windows.Forms.Button btnStartStop;
        private System.Windows.Forms.Label labelResult;
        private System.Windows.Forms.Label labelWaiting;
        private System.Windows.Forms.FlowLayoutPanel textBoxStepsPanel;
        private System.Windows.Forms.Label labelTitle;
    }
}

