namespace RTXLauncher
{
	public partial class ProgressForm : Form
	{
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
				//statusLabel.ForeColor = Color.Green;
			}

			// Add message to log
			logTextBox.AppendText($"{message}\n");
			logTextBox.ScrollToCaret();
		}

		private void closeButton_Click(object sender, EventArgs e)
		{
			this.Close();
		}
	}
}
