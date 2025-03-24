namespace RTXLauncher
{
	partial class UpdateForm
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
			infoLabel = new Label();
			selectAllCheckBox = new CheckBox();
			updateTreeView = new TreeView();
			updateButton = new Button();
			cancelButton = new Button();
			checkBox2 = new CheckBox();
			SuspendLayout();
			// 
			// infoLabel
			// 
			infoLabel.AutoSize = true;
			infoLabel.FlatStyle = FlatStyle.System;
			infoLabel.Location = new Point(12, 9);
			infoLabel.Name = "infoLabel";
			infoLabel.Size = new Size(546, 15);
			infoLabel.TabIndex = 0;
			infoLabel.Text = "The following files need to be updated. Select the files you want to update from the vanilla installation.";
			// 
			// selectAllCheckBox
			// 
			selectAllCheckBox.AutoSize = true;
			selectAllCheckBox.FlatStyle = FlatStyle.System;
			selectAllCheckBox.Location = new Point(12, 27);
			selectAllCheckBox.Name = "selectAllCheckBox";
			selectAllCheckBox.Size = new Size(80, 20);
			selectAllCheckBox.TabIndex = 1;
			selectAllCheckBox.Text = "Select All";
			selectAllCheckBox.UseVisualStyleBackColor = true;
			// 
			// updateTreeView
			// 
			updateTreeView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			updateTreeView.CheckBoxes = true;
			updateTreeView.Location = new Point(12, 53);
			updateTreeView.Name = "updateTreeView";
			updateTreeView.Size = new Size(660, 287);
			updateTreeView.TabIndex = 2;
			updateTreeView.AfterCheck += UpdateTreeView_AfterCheck;
			// 
			// updateButton
			// 
			updateButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			updateButton.DialogResult = DialogResult.OK;
			updateButton.FlatStyle = FlatStyle.System;
			updateButton.Location = new Point(516, 526);
			updateButton.Name = "updateButton";
			updateButton.Size = new Size(75, 23);
			updateButton.TabIndex = 3;
			updateButton.Text = "Update";
			updateButton.UseVisualStyleBackColor = true;
			// 
			// cancelButton
			// 
			cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			cancelButton.DialogResult = DialogResult.Cancel;
			cancelButton.FlatStyle = FlatStyle.System;
			cancelButton.Location = new Point(597, 526);
			cancelButton.Name = "cancelButton";
			cancelButton.Size = new Size(75, 23);
			cancelButton.TabIndex = 4;
			cancelButton.Text = "Cancel";
			cancelButton.UseVisualStyleBackColor = true;
			// 
			// checkBox2
			// 
			checkBox2.AutoSize = true;
			checkBox2.FlatStyle = FlatStyle.System;
			checkBox2.Location = new Point(12, 526);
			checkBox2.Name = "checkBox2";
			checkBox2.Size = new Size(196, 20);
			checkBox2.TabIndex = 5;
			checkBox2.Text = "Reapply patches after updating";
			checkBox2.UseVisualStyleBackColor = true;
			// 
			// UpdateForm
			// 
			AcceptButton = updateButton;
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			CancelButton = cancelButton;
			ClientSize = new Size(684, 561);
			Controls.Add(checkBox2);
			Controls.Add(cancelButton);
			Controls.Add(updateButton);
			Controls.Add(updateTreeView);
			Controls.Add(selectAllCheckBox);
			Controls.Add(infoLabel);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "UpdateForm";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "Update RTX Installation";
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Label infoLabel;
		private CheckBox selectAllCheckBox;
		private TreeView updateTreeView;
		private Button updateButton;
		private Button cancelButton;
		private CheckBox checkBox2;
	}
}