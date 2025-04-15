using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace RTXLauncher
{
    public class CustomWarningDialog : Form
    {
        private Label messageLabel;
        private LinkLabel linkLabel;
        private Button yesButton;
        private Button noButton;

        public CustomWarningDialog(string message, string title, string linkText, string linkUrl)
        {
            this.Text = title;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Width = 730;
            this.Icon = SystemIcons.Warning;

            messageLabel = new Label
            {
                Text = message,
                Location = new System.Drawing.Point(20, 20),
                Width = 410,
                Height = 80,
                AutoSize = true
            };

            linkLabel = new LinkLabel
            {
                Text = linkText,
                AutoSize = true,
                Location = new System.Drawing.Point(20, messageLabel.Bottom + 10)
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

            yesButton = new Button
            {
                Text = "Yes",
                DialogResult = DialogResult.Yes,
                Location = new System.Drawing.Point(this.Width - 170, linkLabel.Bottom + 20),
                Width = 75
            };

            noButton = new Button
            {
                Text = "No",
                DialogResult = DialogResult.No,
                Location = new System.Drawing.Point(this.Width - 90, linkLabel.Bottom + 20),
                Width = 75
            };

            this.Controls.Add(messageLabel);
            this.Controls.Add(linkLabel);
            this.Controls.Add(yesButton);
            this.Controls.Add(noButton);
            this.Height = noButton.Bottom + 50;
            this.AcceptButton = yesButton;
            this.CancelButton = noButton;
        }
    }
}