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
            this.stepsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.inputTextBox = new System.Windows.Forms.TextBox();
            this.headerPaste = new System.Windows.Forms.Label();
            this.startStopButton = new System.Windows.Forms.Button();
            this.headerResult = new System.Windows.Forms.Label();
            this.waitingLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // stepsPanel
            // 
            this.stepsPanel.AutoScroll = true;
            this.stepsPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.stepsPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.stepsPanel.ForeColor = System.Drawing.Color.White;
            this.stepsPanel.Location = new System.Drawing.Point(16, 493);
            this.stepsPanel.Name = "stepsPanel";
            this.stepsPanel.Size = new System.Drawing.Size(772, 233);
            this.stepsPanel.TabIndex = 5;
            this.stepsPanel.WrapContents = false;
            // 
            // inputTextBox
            // 
            this.inputTextBox.BackColor = System.Drawing.Color.DimGray;
            this.inputTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.inputTextBox.ForeColor = System.Drawing.Color.White;
            this.inputTextBox.Location = new System.Drawing.Point(16, 51);
            this.inputTextBox.Multiline = true;
            this.inputTextBox.Name = "inputTextBox";
            this.inputTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.inputTextBox.Size = new System.Drawing.Size(772, 328);
            this.inputTextBox.TabIndex = 0;
            this.inputTextBox.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // headerPaste
            // 
            this.headerPaste.AutoSize = true;
            this.headerPaste.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.headerPaste.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.headerPaste.Location = new System.Drawing.Point(12, 28);
            this.headerPaste.Name = "headerPaste";
            this.headerPaste.Size = new System.Drawing.Size(238, 20);
            this.headerPaste.TabIndex = 1;
            this.headerPaste.Text = "Paste ShapeShifter HTML code:";
            this.headerPaste.Click += new System.EventHandler(this.label1_Click);
            // 
            // startStopButton
            // 
            this.startStopButton.BackColor = System.Drawing.Color.DimGray;
            this.startStopButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.startStopButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.startStopButton.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.startStopButton.Location = new System.Drawing.Point(16, 385);
            this.startStopButton.Name = "startStopButton";
            this.startStopButton.Size = new System.Drawing.Size(109, 48);
            this.startStopButton.TabIndex = 2;
            this.startStopButton.Text = "Start";
            this.startStopButton.UseVisualStyleBackColor = false;
            this.startStopButton.Click += new System.EventHandler(this.startStopButton_Click);
            // 
            // headerResult
            // 
            this.headerResult.AutoSize = true;
            this.headerResult.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.headerResult.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.headerResult.Location = new System.Drawing.Point(12, 470);
            this.headerResult.Name = "headerResult";
            this.headerResult.Size = new System.Drawing.Size(59, 20);
            this.headerResult.TabIndex = 4;
            this.headerResult.Text = "Result:";
            this.headerResult.Click += new System.EventHandler(this.label1_Click_1);
            // 
            // waitingLabel
            // 
            this.waitingLabel.AutoSize = true;
            this.waitingLabel.ForeColor = System.Drawing.Color.Transparent;
            this.waitingLabel.Location = new System.Drawing.Point(131, 404);
            this.waitingLabel.Name = "waitingLabel";
            this.waitingLabel.Size = new System.Drawing.Size(71, 13);
            this.waitingLabel.TabIndex = 6;
            this.waitingLabel.Text = "Calculating ...";
            this.waitingLabel.Visible = false;
            this.waitingLabel.Click += new System.EventHandler(this.label1_Click_2);
            // 
            // ShapeShifter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(800, 738);
            this.Controls.Add(this.waitingLabel);
            this.Controls.Add(this.stepsPanel);
            this.Controls.Add(this.headerResult);
            this.Controls.Add(this.startStopButton);
            this.Controls.Add(this.headerPaste);
            this.Controls.Add(this.inputTextBox);
            this.Name = "ShapeShifter";
            this.ShowIcon = false;
            this.Text = "ShapeShifter Solver";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox inputTextBox;
        private System.Windows.Forms.Label headerPaste;
        private System.Windows.Forms.Button startStopButton;
        private System.Windows.Forms.Label headerResult;
        private System.Windows.Forms.Label waitingLabel;
        private System.Windows.Forms.FlowLayoutPanel stepsPanel;
    }
}

