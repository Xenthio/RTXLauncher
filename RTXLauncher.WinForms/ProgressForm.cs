using RTXLauncher.WinForms.Controls;

namespace RTXLauncher.WinForms
{
	public partial class ProgressForm : Form
	{
		private LogReflectionControl reflectionControl;
		public ProgressForm()
		{
			InitializeComponent();
			this.FormClosing += (s, e) =>
			{
				if (progressBar.Value < 100)
				{
					var result = MessageBox.Show(
						"Installation is still in progress. Are you sure you want to cancel?",
						"Cancel Installation",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Warning
					);
					if (result == DialogResult.No)
					{
						e.Cancel = true;
					}
				}
			};
			//CreateReflection()
		}

		private void CreateReflection()
		{
			// Create and set up the reflection control
			reflectionControl = new LogReflectionControl();
			reflectionControl.Location = new Point(logTextBox.Left, logTextBox.Bottom + 1);
			reflectionControl.Size = new Size(logTextBox.Width, logTextBox.Height);
			reflectionControl.Anchor = logTextBox.Anchor; // Match the anchoring
			reflectionControl.SourceTextBox = logTextBox;

			// Add the reflection control to the form
			this.Controls.Add(reflectionControl);

			// Make sure the reflection control is behind everything else
			reflectionControl.SendToBack();
		}

		// Method to update the progress bar and log
		public void UpdateProgress(string message, int progress)
		{
			if (this.InvokeRequired)
			{
				this.Invoke(new Action<string, int>(UpdateProgress), message, progress);
				return;
			}

			// Update progress bar
			progressBar.Value = Math.Max(0, Math.Min(100, progress));

			// Update status if it's the last message
			if (progress >= 100)
			{
				statusLabel.Text = "Installation Complete!";
				closeButton.Visible = true;
				closeButton.Focus();
				//statusLabel.ForeColor = Color.Green;
			}

			// Add message to log
			logTextBox.AppendText($"{message}\n");
			logTextBox.ScrollToCaret();
			if (reflectionControl != null) reflectionControl.UpdateReflection(true);
		}
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			// Make sure the reflection control is still in the right position
			if (reflectionControl != null && logTextBox != null)
			{
				reflectionControl.Location = new Point(logTextBox.Left, logTextBox.Bottom + 1);
				reflectionControl.Size = new Size(logTextBox.Width, logTextBox.Height);
				reflectionControl.UpdateReflection(true);
			}
		}
		private void closeButton_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void DisposeTimers(bool disposing)
		{
		}
	}
}
