using System.Diagnostics;

namespace RTXLauncher
{
	public class CustomWarningDialog : Form
	{
		private Label messageLabel;
		private LinkLabel linkLabel;
		private Button yesButton;
		private Button noButton;
		private PictureBox warningIcon;
		private TableLayoutPanel buttonPanel;

		public CustomWarningDialog(string message, string title, string linkText, string linkUrl)
		{
			// Set up DPI awareness
			this.AutoScaleMode = AutoScaleMode.Dpi;
			this.Font = SystemFonts.MessageBoxFont;

			this.Text = title;
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.StartPosition = FormStartPosition.CenterParent;
			this.SizeGripStyle = SizeGripStyle.Hide;
			this.Padding = new Padding(6);

			// Create layout panel
			var mainPanel = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 2,
				RowCount = 4,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink
			};

			mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

			mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Icon & Message
			mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Link
			mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 15));  // Spacing
			mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Buttons

			// Add warning icon
			warningIcon = new PictureBox
			{
				Image = SystemIcons.Warning.ToBitmap(),
				SizeMode = PictureBoxSizeMode.AutoSize,
				Margin = new Padding(0, 0, 6, 0),
			};
			mainPanel.Controls.Add(warningIcon, 0, 0);
			mainPanel.SetRowSpan(warningIcon, 2);

			// Message content
			messageLabel = new Label
			{
				Text = message,
				AutoSize = true,
				MaximumSize = new Size(380, 0),
				MinimumSize = new Size(250, 0)
			};
			mainPanel.Controls.Add(messageLabel, 1, 0);

			// Link label
			linkLabel = new LinkLabel
			{
				Text = linkText,
				AutoSize = true,
				Margin = new Padding(3, 6, 3, 6),
				FlatStyle = FlatStyle.System
			};
			linkLabel.LinkClicked += (s, e) =>
			{
				try
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = linkUrl,
						UseShellExecute = true
					});
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Error opening link: {ex.Message}");
				}
			};
			mainPanel.Controls.Add(linkLabel, 1, 1);

			// Button panel - right aligned
			buttonPanel = new TableLayoutPanel
			{
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				ColumnCount = 2,
				RowCount = 1,
				Anchor = AnchorStyles.Right
			};
			buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

			// Buttons
			yesButton = new Button
			{
				Text = "Yes",
				DialogResult = DialogResult.Yes,
				AutoSize = true,
				MinimumSize = new Size(76, 0),
				Margin = new Padding(6, 0, 0, 0),
				FlatStyle = FlatStyle.System
			};

			noButton = new Button
			{
				Text = "No",
				DialogResult = DialogResult.No,
				AutoSize = true,
				MinimumSize = new Size(76, 0),
				Margin = new Padding(6, 0, 0, 0),
				FlatStyle = FlatStyle.System
			};

			buttonPanel.Controls.Add(yesButton, 0, 0);
			buttonPanel.Controls.Add(noButton, 1, 0);
			mainPanel.Controls.Add(buttonPanel, 1, 3);

			// Add the main panel to the form
			this.Controls.Add(mainPanel);

			// Set the size of the form based on the panel
			this.ClientSize = new Size(450, 200);

			this.AcceptButton = yesButton;
			this.CancelButton = noButton;

			// Handle form loaded event to adjust the size
			this.Load += CustomWarningDialog_Load;
		}

		private void CustomWarningDialog_Load(object sender, EventArgs e)
		{
			// Get current DPI scale factor
			float dpiScaleFactor;
			using (Graphics g = this.CreateGraphics())
			{
				dpiScaleFactor = g.DpiX / 96f;
			}

			// Base sizes adjusted for 100% scaling
			int baseWidth = 450;
			int baseHeight = 220;

			// Calculate actual content width needed
			int contentWidth = messageLabel.Width + warningIcon.Width + (int)(50 * dpiScaleFactor) - 12;

			// Take the larger of the base width or content width
			int width = Math.Max(contentWidth, baseWidth);

			// Calculate height based on content
			int contentHeight = messageLabel.Height + linkLabel.Height + buttonPanel.Height + (int)(100 * dpiScaleFactor) - 18;
			int height = Math.Max(contentHeight, baseHeight);

			// Set the form size
			this.MinimumSize = new Size(width, height);
			this.Size = new Size(width, height);

			// Force a layout pass to position everything correctly
			this.PerformLayout();
		}
	}
}