namespace RTXLauncher
{
	partial class ProgressForm
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
			DisposeTimers(disposing);
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProgressForm));
			progressBar = new ProgressBar();
			statusLabel = new Label();
			logTextBox = new RichTextBox();
			closeButton = new Button();
			SuspendLayout();
			// 
			// progressBar
			// 
			progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			progressBar.Location = new Point(12, 32);
			progressBar.Name = "progressBar";
			progressBar.Size = new Size(510, 23);
			progressBar.TabIndex = 0;
			// 
			// statusLabel
			// 
			statusLabel.AutoSize = true;
			statusLabel.Location = new Point(12, 9);
			statusLabel.Name = "statusLabel";
			statusLabel.Size = new Size(129, 15);
			statusLabel.TabIndex = 1;
			statusLabel.Text = "Creating a new install...";
			// 
			// logTextBox
			// 
			logTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			logTextBox.BackColor = Color.Black;
			logTextBox.Font = new Font("Consolas", 9F);
			logTextBox.ForeColor = Color.LightGreen;
			logTextBox.Location = new Point(12, 61);
			logTextBox.Name = "logTextBox";
			logTextBox.ReadOnly = true;
			logTextBox.Size = new Size(510, 328);
			logTextBox.TabIndex = 2;
			logTextBox.Text = "";
			// 
			// closeButton
			// 
			closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			closeButton.Location = new Point(447, 5);
			closeButton.Name = "closeButton";
			closeButton.Size = new Size(75, 23);
			closeButton.TabIndex = 3;
			closeButton.Text = "Close";
			closeButton.UseVisualStyleBackColor = true;
			closeButton.Visible = false;
			closeButton.Click += closeButton_Click;
			// 
			// ProgressForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(534, 401);
			Controls.Add(closeButton);
			Controls.Add(logTextBox);
			Controls.Add(statusLabel);
			Controls.Add(progressBar);
			Icon = (Icon)resources.GetObject("$this.Icon");
			MaximizeBox = false;
			Name = "ProgressForm";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "RTX Installation Progress";
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private ProgressBar progressBar;
		private Label statusLabel;
		private RichTextBox logTextBox;
		private Button closeButton;
	}
}