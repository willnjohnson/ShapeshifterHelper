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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShapeShifter));
            this.textBoxInput = new System.Windows.Forms.RichTextBox();
            this.btnStartStop = new System.Windows.Forms.Button();
            this.textBoxStepsPanel = new Shapeshifter.DoubleBufferedFlowLayoutPanel();
            this.labelWaiting = new Shapeshifter.RoundedLabel();
            this.labelResult = new Shapeshifter.RoundedLabel();
            this.labelPaste = new Shapeshifter.RoundedLabel();
            this.pboxTitle = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pboxTitle)).BeginInit();
            this.SuspendLayout();
            // 
            // textBoxInput
            // 
            this.textBoxInput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxInput.BackColor = System.Drawing.Color.SaddleBrown;
            this.textBoxInput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxInput.ForeColor = System.Drawing.Color.BlanchedAlmond;
            this.textBoxInput.Location = new System.Drawing.Point(16, 81);
            this.textBoxInput.MaxLength = 200000;
            this.textBoxInput.Name = "textBoxInput";
            this.textBoxInput.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.textBoxInput.Size = new System.Drawing.Size(856, 252);
            this.textBoxInput.TabIndex = 0;
            this.textBoxInput.Text = "";
            this.textBoxInput.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // btnStartStop
            // 
            this.btnStartStop.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnStartStop.BackColor = System.Drawing.Color.SaddleBrown;
            this.btnStartStop.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.btnStartStop.ForeColor = System.Drawing.Color.BlanchedAlmond;
            this.btnStartStop.Location = new System.Drawing.Point(16, 339);
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Size = new System.Drawing.Size(109, 48);
            this.btnStartStop.TabIndex = 2;
            this.btnStartStop.Text = "Start";
            this.btnStartStop.UseVisualStyleBackColor = false;
            this.btnStartStop.Click += new System.EventHandler(this.startStopButton_Click);
            // 
            // textBoxStepsPanel
            // 
            this.textBoxStepsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxStepsPanel.AutoScroll = true;
            this.textBoxStepsPanel.BackColor = System.Drawing.Color.Transparent;
            this.textBoxStepsPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.textBoxStepsPanel.ForeColor = System.Drawing.Color.White;
            this.textBoxStepsPanel.Location = new System.Drawing.Point(16, 428);
            this.textBoxStepsPanel.Name = "textBoxStepsPanel";
            this.textBoxStepsPanel.Size = new System.Drawing.Size(856, 241);
            this.textBoxStepsPanel.TabIndex = 13;
            this.textBoxStepsPanel.WrapContents = false;
            // 
            // labelWaiting
            // 
            this.labelWaiting.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelWaiting.AutoSize = true;
            this.labelWaiting.BackColor = System.Drawing.Color.Transparent;
            this.labelWaiting.CornerRadius = 10;
            this.labelWaiting.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.labelWaiting.ForeColor = System.Drawing.Color.BlanchedAlmond;
            this.labelWaiting.Location = new System.Drawing.Point(131, 358);
            this.labelWaiting.Name = "labelWaiting";
            this.labelWaiting.Size = new System.Drawing.Size(68, 13);
            this.labelWaiting.TabIndex = 11;
            this.labelWaiting.Text = "Calculating...";
            this.labelWaiting.Visible = false;
            // 
            // labelResult
            // 
            this.labelResult.AutoSize = true;
            this.labelResult.BackColor = System.Drawing.Color.Transparent;
            this.labelResult.CornerRadius = 10;
            this.labelResult.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.labelResult.ForeColor = System.Drawing.Color.BlanchedAlmond;
            this.labelResult.Location = new System.Drawing.Point(17, 400);
            this.labelResult.Name = "labelResult";
            this.labelResult.Size = new System.Drawing.Size(59, 20);
            this.labelResult.TabIndex = 10;
            this.labelResult.Text = "Result:";
            this.labelResult.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // labelPaste
            // 
            this.labelPaste.AutoSize = true;
            this.labelPaste.BackColor = System.Drawing.Color.Transparent;
            this.labelPaste.CornerRadius = 10;
            this.labelPaste.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.labelPaste.ForeColor = System.Drawing.Color.BlanchedAlmond;
            this.labelPaste.Location = new System.Drawing.Point(17, 53);
            this.labelPaste.Name = "labelPaste";
            this.labelPaste.Size = new System.Drawing.Size(238, 20);
            this.labelPaste.TabIndex = 8;
            this.labelPaste.Text = "Paste ShapeShifter HTML code:";
            this.labelPaste.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // pboxTitle
            // 
            this.pboxTitle.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.pboxTitle.BackColor = System.Drawing.Color.Transparent;
            this.pboxTitle.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pboxTitle.BackgroundImage")));
            this.pboxTitle.Location = new System.Drawing.Point(298, 11);
            this.pboxTitle.Name = "pboxTitle";
            this.pboxTitle.Size = new System.Drawing.Size(325, 59);
            this.pboxTitle.TabIndex = 14;
            this.pboxTitle.TabStop = false;
            // 
            // ShapeShifter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.Black;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.ClientSize = new System.Drawing.Size(884, 681);
            this.Controls.Add(this.pboxTitle);
            this.Controls.Add(this.textBoxStepsPanel);
            this.Controls.Add(this.labelWaiting);
            this.Controls.Add(this.labelResult);
            this.Controls.Add(this.labelPaste);
            this.Controls.Add(this.btnStartStop);
            this.Controls.Add(this.textBoxInput);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ShapeShifter";
            this.ShowIcon = false;
            this.Text = "ShapeShifter Helper (A* Heuristic)";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pboxTitle)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox textBoxInput;
        private System.Windows.Forms.Button btnStartStop;
        private RoundedLabel labelPaste;
        private RoundedLabel labelResult;
        private RoundedLabel labelWaiting;
        private DoubleBufferedFlowLayoutPanel textBoxStepsPanel;
        private System.Windows.Forms.PictureBox pboxTitle;
    }
}

