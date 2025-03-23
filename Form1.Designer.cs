namespace RTXLauncher
{
	partial class Form1
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
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
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			settingsDataBindingSource = new BindingSource(components);
			LaunchGameButton = new Button();
			CloseButton = new Button();
			tabControl1 = new TabControl();
			SettingsPage = new TabPage();
			groupBox1 = new GroupBox();
			CustomWidthBox = new NumericUpDown();
			CustomHeightBox = new NumericUpDown();
			label2 = new Label();
			label1 = new Label();
			checkBox1 = new CheckBox();
			WidthHeightComboBox = new ComboBox();
			groupBox2 = new GroupBox();
			checkBox6 = new CheckBox();
			checkBox2 = new CheckBox();
			MountingPage = new TabPage();
			groupBox6 = new GroupBox();
			MountP2RTXCheckBox = new GameMountCheckbox();
			MountPortalPreludeRTXCheckBox = new GameMountCheckbox();
			MountPortalRTXCheckbox = new GameMountCheckbox();
			MountHL2RTXCheckbox = new GameMountCheckbox();
			AdvancedPage = new TabPage();
			groupBox5 = new GroupBox();
			label3 = new Label();
			numericUpDown1 = new NumericUpDown();
			checkBox3 = new CheckBox();
			groupBox4 = new GroupBox();
			checkBox5 = new CheckBox();
			checkBox4 = new CheckBox();
			groupBox3 = new GroupBox();
			textBox1 = new TextBox();
			((System.ComponentModel.ISupportInitialize)settingsDataBindingSource).BeginInit();
			tabControl1.SuspendLayout();
			SettingsPage.SuspendLayout();
			groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)CustomWidthBox).BeginInit();
			((System.ComponentModel.ISupportInitialize)CustomHeightBox).BeginInit();
			groupBox2.SuspendLayout();
			MountingPage.SuspendLayout();
			groupBox6.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)MountP2RTXCheckBox).BeginInit();
			((System.ComponentModel.ISupportInitialize)MountPortalPreludeRTXCheckBox).BeginInit();
			((System.ComponentModel.ISupportInitialize)MountPortalRTXCheckbox).BeginInit();
			((System.ComponentModel.ISupportInitialize)MountHL2RTXCheckbox).BeginInit();
			AdvancedPage.SuspendLayout();
			groupBox5.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)numericUpDown1).BeginInit();
			groupBox4.SuspendLayout();
			groupBox3.SuspendLayout();
			SuspendLayout();
			// 
			// settingsDataBindingSource
			// 
			settingsDataBindingSource.DataSource = typeof(SettingsData);
			// 
			// LaunchGameButton
			// 
			LaunchGameButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			LaunchGameButton.DialogResult = DialogResult.OK;
			LaunchGameButton.FlatStyle = FlatStyle.System;
			LaunchGameButton.Location = new Point(140, 332);
			LaunchGameButton.Name = "LaunchGameButton";
			LaunchGameButton.Size = new Size(93, 23);
			LaunchGameButton.TabIndex = 0;
			LaunchGameButton.Text = "Launch Game";
			LaunchGameButton.UseVisualStyleBackColor = true;
			LaunchGameButton.Click += LaunchGameButton_Click;
			// 
			// CloseButton
			// 
			CloseButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			CloseButton.DialogResult = DialogResult.Cancel;
			CloseButton.Location = new Point(239, 332);
			CloseButton.Name = "CloseButton";
			CloseButton.Size = new Size(75, 23);
			CloseButton.TabIndex = 1;
			CloseButton.Text = "Close";
			CloseButton.UseVisualStyleBackColor = true;
			CloseButton.Click += CloseButton_Click;
			// 
			// tabControl1
			// 
			tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			tabControl1.Controls.Add(SettingsPage);
			tabControl1.Controls.Add(MountingPage);
			tabControl1.Controls.Add(AdvancedPage);
			tabControl1.Location = new Point(6, 6);
			tabControl1.Name = "tabControl1";
			tabControl1.SelectedIndex = 0;
			tabControl1.Size = new Size(312, 320);
			tabControl1.TabIndex = 10;
			// 
			// SettingsPage
			// 
			SettingsPage.BackColor = SystemColors.Window;
			SettingsPage.Controls.Add(groupBox1);
			SettingsPage.Controls.Add(groupBox2);
			SettingsPage.Location = new Point(4, 24);
			SettingsPage.Name = "SettingsPage";
			SettingsPage.Padding = new Padding(3);
			SettingsPage.Size = new Size(304, 292);
			SettingsPage.TabIndex = 0;
			SettingsPage.Text = "Settings";
			// 
			// groupBox1
			// 
			groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			groupBox1.Controls.Add(CustomWidthBox);
			groupBox1.Controls.Add(CustomHeightBox);
			groupBox1.Controls.Add(label2);
			groupBox1.Controls.Add(label1);
			groupBox1.Controls.Add(checkBox1);
			groupBox1.Controls.Add(WidthHeightComboBox);
			groupBox1.FlatStyle = FlatStyle.System;
			groupBox1.Location = new Point(6, 6);
			groupBox1.Name = "groupBox1";
			groupBox1.Size = new Size(292, 123);
			groupBox1.TabIndex = 2;
			groupBox1.TabStop = false;
			groupBox1.Text = "Resolution";
			groupBox1.Enter += groupBox1_Enter;
			// 
			// CustomWidthBox
			// 
			CustomWidthBox.DataBindings.Add(new Binding("Value", settingsDataBindingSource, "Width", true, DataSourceUpdateMode.OnPropertyChanged));
			CustomWidthBox.DataBindings.Add(new Binding("Enabled", settingsDataBindingSource, "UseCustomResolution", true, DataSourceUpdateMode.OnPropertyChanged));
			CustomWidthBox.Location = new Point(6, 91);
			CustomWidthBox.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
			CustomWidthBox.Name = "CustomWidthBox";
			CustomWidthBox.Size = new Size(47, 23);
			CustomWidthBox.TabIndex = 5;
			CustomWidthBox.Value = new decimal(new int[] { 1920, 0, 0, 0 });
			// 
			// CustomHeightBox
			// 
			CustomHeightBox.DataBindings.Add(new Binding("Value", settingsDataBindingSource, "Height", true, DataSourceUpdateMode.OnPropertyChanged));
			CustomHeightBox.DataBindings.Add(new Binding("Enabled", settingsDataBindingSource, "UseCustomResolution", true, DataSourceUpdateMode.OnPropertyChanged));
			CustomHeightBox.Location = new Point(72, 91);
			CustomHeightBox.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
			CustomHeightBox.Name = "CustomHeightBox";
			CustomHeightBox.Size = new Size(47, 23);
			CustomHeightBox.TabIndex = 6;
			CustomHeightBox.Value = new decimal(new int[] { 1080, 0, 0, 0 });
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.DataBindings.Add(new Binding("Enabled", settingsDataBindingSource, "UseCustomResolution", true, DataSourceUpdateMode.OnPropertyChanged));
			label2.FlatStyle = FlatStyle.System;
			label2.Location = new Point(6, 73);
			label2.Name = "label2";
			label2.Size = new Size(108, 15);
			label2.TabIndex = 5;
			label2.Text = "Custom Resolution";
			label2.Click += label2_Click;
			// 
			// label1
			// 
			label1.Font = new Font("Segoe UI Symbol", 9F);
			label1.Location = new Point(56, 92);
			label1.Margin = new Padding(0);
			label1.Name = "label1";
			label1.Size = new Size(13, 18);
			label1.TabIndex = 4;
			label1.Text = "×";
			label1.TextAlign = ContentAlignment.MiddleLeft;
			label1.Click += label1_Click;
			// 
			// checkBox1
			// 
			checkBox1.AutoSize = true;
			checkBox1.DataBindings.Add(new Binding("Checked", settingsDataBindingSource, "UseCustomResolution", true, DataSourceUpdateMode.OnPropertyChanged));
			checkBox1.FlatStyle = FlatStyle.System;
			checkBox1.Location = new Point(6, 51);
			checkBox1.Name = "checkBox1";
			checkBox1.Size = new Size(155, 20);
			checkBox1.TabIndex = 4;
			checkBox1.Text = "Use Custom Resolution";
			checkBox1.UseVisualStyleBackColor = true;
			checkBox1.CheckStateChanged += checkBox1_CheckedChanged;
			// 
			// WidthHeightComboBox
			// 
			WidthHeightComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			WidthHeightComboBox.FlatStyle = FlatStyle.System;
			WidthHeightComboBox.FormattingEnabled = true;
			WidthHeightComboBox.Items.AddRange(new object[] { "1920x1080", "2560x1440", "3440x1440", "3480x2160", "1600x900", "1366x768", "1280x720", "1920x1200" });
			WidthHeightComboBox.Location = new Point(6, 22);
			WidthHeightComboBox.Name = "WidthHeightComboBox";
			WidthHeightComboBox.Size = new Size(280, 23);
			WidthHeightComboBox.TabIndex = 3;
			WidthHeightComboBox.Text = "1920x1080";
			WidthHeightComboBox.SelectedIndexChanged += WidthHeightComboBox_SelectedIndexChanged;
			WidthHeightComboBox.TextChanged += WidthHeightComboBox_TextUpdate;
			// 
			// groupBox2
			// 
			groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			groupBox2.Controls.Add(checkBox6);
			groupBox2.Controls.Add(checkBox2);
			groupBox2.Location = new Point(6, 135);
			groupBox2.Name = "groupBox2";
			groupBox2.Size = new Size(292, 95);
			groupBox2.TabIndex = 7;
			groupBox2.TabStop = false;
			groupBox2.Text = "Garry's Mod";
			groupBox2.Enter += groupBox2_Enter;
			// 
			// checkBox6
			// 
			checkBox6.AutoSize = true;
			checkBox6.DataBindings.Add(new Binding("Checked", settingsDataBindingSource, "DisableChromium", true, DataSourceUpdateMode.OnPropertyChanged));
			checkBox6.FlatStyle = FlatStyle.System;
			checkBox6.Location = new Point(6, 48);
			checkBox6.Name = "checkBox6";
			checkBox6.Size = new Size(131, 20);
			checkBox6.TabIndex = 2;
			checkBox6.Text = "Disable Chromium";
			checkBox6.UseVisualStyleBackColor = true;
			// 
			// checkBox2
			// 
			checkBox2.AutoSize = true;
			checkBox2.Checked = true;
			checkBox2.CheckState = CheckState.Checked;
			checkBox2.DataBindings.Add(new Binding("Checked", settingsDataBindingSource, "LoadWorkshopAddons", true, DataSourceUpdateMode.OnPropertyChanged));
			checkBox2.FlatStyle = FlatStyle.System;
			checkBox2.Location = new Point(6, 22);
			checkBox2.Name = "checkBox2";
			checkBox2.Size = new Size(159, 20);
			checkBox2.TabIndex = 1;
			checkBox2.Text = "Load Workshop Addons";
			checkBox2.UseVisualStyleBackColor = true;
			// 
			// MountingPage
			// 
			MountingPage.BackColor = SystemColors.Window;
			MountingPage.Controls.Add(groupBox6);
			MountingPage.Location = new Point(4, 24);
			MountingPage.Name = "MountingPage";
			MountingPage.Size = new Size(304, 292);
			MountingPage.TabIndex = 2;
			MountingPage.Text = "Content Mounting";
			// 
			// groupBox6
			// 
			groupBox6.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			groupBox6.Controls.Add(MountP2RTXCheckBox);
			groupBox6.Controls.Add(MountPortalPreludeRTXCheckBox);
			groupBox6.Controls.Add(MountPortalRTXCheckbox);
			groupBox6.Controls.Add(MountHL2RTXCheckbox);
			groupBox6.FlatStyle = FlatStyle.System;
			groupBox6.Location = new Point(6, 6);
			groupBox6.Name = "groupBox6";
			groupBox6.Size = new Size(292, 245);
			groupBox6.TabIndex = 1;
			groupBox6.TabStop = false;
			groupBox6.Text = "Mounted Remix Games";
			// 
			// MountP2RTXCheckBox
			// 
			MountP2RTXCheckBox.AutoSize = true;
			MountP2RTXCheckBox.Enabled = false;
			MountP2RTXCheckBox.FlatStyle = FlatStyle.System;
			MountP2RTXCheckBox.GameFolder = "portal2";
			MountP2RTXCheckBox.InstallFolder = "Portal 2 With RTX";
			MountP2RTXCheckBox.Location = new Point(6, 100);
			MountP2RTXCheckBox.Name = "MountP2RTXCheckBox";
			MountP2RTXCheckBox.RemixModFolder = "portal2rtx";
			MountP2RTXCheckBox.Size = new Size(120, 20);
			MountP2RTXCheckBox.TabIndex = 7;
			MountP2RTXCheckBox.Text = "Portal 2 with RTX";
			MountP2RTXCheckBox.UseVisualStyleBackColor = true;
			// 
			// MountPortalPreludeRTXCheckBox
			// 
			MountPortalPreludeRTXCheckBox.AutoSize = true;
			MountPortalPreludeRTXCheckBox.Enabled = false;
			MountPortalPreludeRTXCheckBox.FlatStyle = FlatStyle.System;
			MountPortalPreludeRTXCheckBox.GameFolder = "prelude_rtx";
			MountPortalPreludeRTXCheckBox.InstallFolder = "Portal Prelude RTX";
			MountPortalPreludeRTXCheckBox.Location = new Point(6, 74);
			MountPortalPreludeRTXCheckBox.Name = "MountPortalPreludeRTXCheckBox";
			MountPortalPreludeRTXCheckBox.RemixModFolder = "gameReadyAssets";
			MountPortalPreludeRTXCheckBox.Size = new Size(131, 20);
			MountPortalPreludeRTXCheckBox.TabIndex = 6;
			MountPortalPreludeRTXCheckBox.Text = "Portal: Prelude RTX";
			MountPortalPreludeRTXCheckBox.UseVisualStyleBackColor = true;
			// 
			// MountPortalRTXCheckbox
			// 
			MountPortalRTXCheckbox.AutoSize = true;
			MountPortalRTXCheckbox.Enabled = false;
			MountPortalRTXCheckbox.FlatStyle = FlatStyle.System;
			MountPortalRTXCheckbox.GameFolder = "portal_rtx";
			MountPortalRTXCheckbox.InstallFolder = "PortalRTX";
			MountPortalRTXCheckbox.Location = new Point(6, 48);
			MountPortalRTXCheckbox.Name = "MountPortalRTXCheckbox";
			MountPortalRTXCheckbox.RemixModFolder = "gameReadyAssets";
			MountPortalRTXCheckbox.Size = new Size(111, 20);
			MountPortalRTXCheckbox.TabIndex = 5;
			MountPortalRTXCheckbox.Text = "Portal with RTX";
			MountPortalRTXCheckbox.UseVisualStyleBackColor = true;
			// 
			// MountHL2RTXCheckbox
			// 
			MountHL2RTXCheckbox.AutoSize = true;
			MountHL2RTXCheckbox.Enabled = false;
			MountHL2RTXCheckbox.FlatStyle = FlatStyle.System;
			MountHL2RTXCheckbox.GameFolder = "hl2rtx";
			MountHL2RTXCheckbox.InstallFolder = "Half-Life 2 RTX";
			MountHL2RTXCheckbox.Location = new Point(6, 22);
			MountHL2RTXCheckbox.Name = "MountHL2RTXCheckbox";
			MountHL2RTXCheckbox.RemixModFolder = "hl2rtx";
			MountHL2RTXCheckbox.Size = new Size(112, 20);
			MountHL2RTXCheckbox.TabIndex = 4;
			MountHL2RTXCheckbox.Text = "Half-Life 2: RTX";
			MountHL2RTXCheckbox.UseVisualStyleBackColor = true;
			// 
			// AdvancedPage
			// 
			AdvancedPage.BackColor = SystemColors.Window;
			AdvancedPage.Controls.Add(groupBox5);
			AdvancedPage.Controls.Add(groupBox4);
			AdvancedPage.Controls.Add(groupBox3);
			AdvancedPage.Location = new Point(4, 24);
			AdvancedPage.Name = "AdvancedPage";
			AdvancedPage.Padding = new Padding(3);
			AdvancedPage.Size = new Size(304, 292);
			AdvancedPage.TabIndex = 1;
			AdvancedPage.Text = "Advanced";
			// 
			// groupBox5
			// 
			groupBox5.BackColor = SystemColors.Window;
			groupBox5.Controls.Add(label3);
			groupBox5.Controls.Add(numericUpDown1);
			groupBox5.Controls.Add(checkBox3);
			groupBox5.FlatStyle = FlatStyle.System;
			groupBox5.Location = new Point(6, 88);
			groupBox5.Name = "groupBox5";
			groupBox5.Size = new Size(292, 81);
			groupBox5.TabIndex = 11;
			groupBox5.TabStop = false;
			groupBox5.Text = "Engine";
			// 
			// label3
			// 
			label3.AutoSize = true;
			label3.Location = new Point(6, 50);
			label3.Name = "label3";
			label3.Size = new Size(55, 15);
			label3.TabIndex = 4;
			label3.Text = "DX Level:";
			// 
			// numericUpDown1
			// 
			numericUpDown1.DataBindings.Add(new Binding("Value", settingsDataBindingSource, "DXLevel", true, DataSourceUpdateMode.OnPropertyChanged));
			numericUpDown1.Location = new Point(67, 48);
			numericUpDown1.Name = "numericUpDown1";
			numericUpDown1.Size = new Size(120, 23);
			numericUpDown1.TabIndex = 3;
			numericUpDown1.Value = new decimal(new int[] { 90, 0, 0, 0 });
			// 
			// checkBox3
			// 
			checkBox3.AutoSize = true;
			checkBox3.DataBindings.Add(new Binding("Checked", settingsDataBindingSource, "ToolsMode", true, DataSourceUpdateMode.OnPropertyChanged));
			checkBox3.FlatStyle = FlatStyle.System;
			checkBox3.Location = new Point(6, 22);
			checkBox3.Name = "checkBox3";
			checkBox3.Size = new Size(93, 20);
			checkBox3.TabIndex = 2;
			checkBox3.Text = "Tools Mode";
			checkBox3.UseVisualStyleBackColor = true;
			// 
			// groupBox4
			// 
			groupBox4.BackColor = SystemColors.Window;
			groupBox4.Controls.Add(checkBox5);
			groupBox4.Controls.Add(checkBox4);
			groupBox4.FlatStyle = FlatStyle.System;
			groupBox4.Location = new Point(6, 6);
			groupBox4.Name = "groupBox4";
			groupBox4.Size = new Size(292, 76);
			groupBox4.TabIndex = 10;
			groupBox4.TabStop = false;
			groupBox4.Text = "Debug";
			// 
			// checkBox5
			// 
			checkBox5.AutoSize = true;
			checkBox5.DataBindings.Add(new Binding("Checked", settingsDataBindingSource, "DeveloperMode", true, DataSourceUpdateMode.OnPropertyChanged));
			checkBox5.FlatStyle = FlatStyle.System;
			checkBox5.Location = new Point(6, 48);
			checkBox5.Name = "checkBox5";
			checkBox5.Size = new Size(119, 20);
			checkBox5.TabIndex = 4;
			checkBox5.Text = "Developer Mode";
			checkBox5.UseVisualStyleBackColor = true;
			// 
			// checkBox4
			// 
			checkBox4.AutoSize = true;
			checkBox4.Checked = true;
			checkBox4.CheckState = CheckState.Checked;
			checkBox4.DataBindings.Add(new Binding("Checked", settingsDataBindingSource, "ConsoleEnabled", true, DataSourceUpdateMode.OnPropertyChanged));
			checkBox4.FlatStyle = FlatStyle.System;
			checkBox4.Location = new Point(6, 22);
			checkBox4.Name = "checkBox4";
			checkBox4.Size = new Size(75, 20);
			checkBox4.TabIndex = 3;
			checkBox4.Text = "Console";
			checkBox4.UseVisualStyleBackColor = true;
			// 
			// groupBox3
			// 
			groupBox3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			groupBox3.BackColor = SystemColors.Window;
			groupBox3.Controls.Add(textBox1);
			groupBox3.FlatStyle = FlatStyle.System;
			groupBox3.Location = new Point(6, 175);
			groupBox3.Name = "groupBox3";
			groupBox3.Size = new Size(292, 78);
			groupBox3.TabIndex = 9;
			groupBox3.TabStop = false;
			groupBox3.Text = "Other Launch Options";
			// 
			// textBox1
			// 
			textBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			textBox1.DataBindings.Add(new Binding("Text", settingsDataBindingSource, "CustomLaunchOptions", true, DataSourceUpdateMode.OnPropertyChanged));
			textBox1.Location = new Point(6, 22);
			textBox1.Multiline = true;
			textBox1.Name = "textBox1";
			textBox1.PlaceholderText = "User-Specified Launch Options";
			textBox1.Size = new Size(280, 46);
			textBox1.TabIndex = 1;
			// 
			// Form1
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(324, 361);
			Controls.Add(tabControl1);
			Controls.Add(CloseButton);
			Controls.Add(LaunchGameButton);
			Icon = (Icon)resources.GetObject("$this.Icon");
			Name = "Form1";
			StartPosition = FormStartPosition.CenterScreen;
			Text = " Garry's Mod RTX Launcher";
			Load += Form1_Load;
			((System.ComponentModel.ISupportInitialize)settingsDataBindingSource).EndInit();
			tabControl1.ResumeLayout(false);
			SettingsPage.ResumeLayout(false);
			groupBox1.ResumeLayout(false);
			groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)CustomWidthBox).EndInit();
			((System.ComponentModel.ISupportInitialize)CustomHeightBox).EndInit();
			groupBox2.ResumeLayout(false);
			groupBox2.PerformLayout();
			MountingPage.ResumeLayout(false);
			groupBox6.ResumeLayout(false);
			groupBox6.PerformLayout();
			((System.ComponentModel.ISupportInitialize)MountP2RTXCheckBox).EndInit();
			((System.ComponentModel.ISupportInitialize)MountPortalPreludeRTXCheckBox).EndInit();
			((System.ComponentModel.ISupportInitialize)MountPortalRTXCheckbox).EndInit();
			((System.ComponentModel.ISupportInitialize)MountHL2RTXCheckbox).EndInit();
			AdvancedPage.ResumeLayout(false);
			groupBox5.ResumeLayout(false);
			groupBox5.PerformLayout();
			((System.ComponentModel.ISupportInitialize)numericUpDown1).EndInit();
			groupBox4.ResumeLayout(false);
			groupBox4.PerformLayout();
			groupBox3.ResumeLayout(false);
			groupBox3.PerformLayout();
			ResumeLayout(false);
		}

		#endregion
		private Button LaunchGameButton;
		public BindingSource settingsDataBindingSource;
		private Button CloseButton;
		private TabControl tabControl1;
		private TabPage SettingsPage;
		private GroupBox groupBox1;
		private NumericUpDown CustomWidthBox;
		private NumericUpDown CustomHeightBox;
		private Label label2;
		private Label label1;
		private CheckBox checkBox1;
		private ComboBox WidthHeightComboBox;
		private GroupBox groupBox2;
		private CheckBox checkBox3;
		private CheckBox checkBox2;
		private GroupBox groupBox3;
		private TextBox textBox1;
		private TabPage AdvancedPage;
		private GroupBox groupBox5;
		private GroupBox groupBox4;
		private CheckBox checkBox4;
		private NumericUpDown numericUpDown1;
		private Label label3;
		private CheckBox checkBox5;
		private CheckBox checkBox6;
		private TabPage MountingPage;
		private GroupBox groupBox6;
		private GameMountCheckbox MountHL2RTXCheckbox;
		private GameMountCheckbox MountPortalPreludeRTXCheckBox;
		private GameMountCheckbox MountPortalRTXCheckbox;
		private GameMountCheckbox MountP2RTXCheckBox;
	}
}
