using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;

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
            this.Icon = SystemIcons.Warning;
            this.SizeGripStyle = SizeGripStyle.Hide;
            this.Padding = new Padding(20);
            
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
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));  // Spacing
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Buttons

            // Add warning icon
            warningIcon = new PictureBox
            {
                Image = SystemIcons.Warning.ToBitmap(),
                SizeMode = PictureBoxSizeMode.AutoSize,
                Margin = new Padding(0, 0, 10, 0)
            };
            mainPanel.Controls.Add(warningIcon, 0, 0);
            mainPanel.SetRowSpan(warningIcon, 2);

            // Message content
            messageLabel = new Label
            {
                Text = message,
                AutoSize = true,
                Margin = new Padding(3),
                MaximumSize = new Size(450, 0),  // Allow vertical auto-sizing with max width
                MinimumSize = new Size(300, 0)
            };
            mainPanel.Controls.Add(messageLabel, 1, 0);
            
            // Link label
            linkLabel = new LinkLabel
            {
                Text = linkText,
                AutoSize = true,
                Margin = new Padding(3, 10, 3, 10)
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
            buttonPanel.Margin = new Padding(3, 10, 3, 3);

            // Buttons
            yesButton = new Button
            {
                Text = "Yes",
                DialogResult = DialogResult.Yes,
                AutoSize = true,
                Padding = new Padding(10, 5, 10, 5),
                MinimumSize = new Size(80, 0),
                Margin = new Padding(3)
            };
            
            noButton = new Button
            {
                Text = "No",
                DialogResult = DialogResult.No,
                AutoSize = true,
                Padding = new Padding(10, 5, 10, 5),
                MinimumSize = new Size(80, 0),
                Margin = new Padding(3)
            };

            buttonPanel.Controls.Add(yesButton, 0, 0);
            buttonPanel.Controls.Add(noButton, 1, 0);
            mainPanel.Controls.Add(buttonPanel, 1, 3);
            
            // Add the main panel to the form
            this.Controls.Add(mainPanel);
            
            // Set the size of the form based on the panel
            this.ClientSize = new Size(530, 330);  // Increased initial height
            
            this.AcceptButton = yesButton;
            this.CancelButton = noButton;
            
            // Handle form loaded event to adjust the size
            this.Load += CustomWarningDialog_Load;
        }

        private void CustomWarningDialog_Load(object sender, EventArgs e)
        {
            // Add some extra padding to ensure everything fits
            int width = Math.Max(messageLabel.Width + warningIcon.Width + 100, 530);
            int height = Math.Max(messageLabel.Height + linkLabel.Height + buttonPanel.Height + 160, 300); // Increased padding
            
            // Ensure the form is wide enough
            this.MinimumSize = new Size(width, height);
            this.Size = new Size(width, height);
            
            // Force a layout pass to position everything correctly
            this.PerformLayout();
        }
    }
}