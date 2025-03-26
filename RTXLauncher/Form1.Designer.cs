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
			QuickInstallGroup = new GroupBox();
			label17 = new Label();
			label16 = new Label();
			label14 = new Label();
			OneClickEasyInstallButton = new Button();
			label13 = new Label();
			groupBox2 = new GroupBox();
			EnableRTXCheckBox = new CheckBox();
			checkBox2 = new CheckBox();
			MountingPage = new TabPage();
			groupBox6 = new GroupBox();
			MountP2RTXCheckBox = new GameMountCheckbox();
			MountPortalPreludeRTXCheckBox = new GameMountCheckbox();
			MountPortalRTXCheckbox = new GameMountCheckbox();
			MountHL2RTXCheckbox = new GameMountCheckbox();
			AdvancedPage = new TabPage();
			groupBox5 = new GroupBox();
			checkBox6 = new CheckBox();
			label3 = new Label();
			numericUpDown1 = new NumericUpDown();
			checkBox3 = new CheckBox();
			groupBox4 = new GroupBox();
			checkBox5 = new CheckBox();
			checkBox4 = new CheckBox();
			groupBox3 = new GroupBox();
			textBox1 = new TextBox();
			InstallPage = new TabPage();
			groupBox13 = new GroupBox();
			groupBox12 = new GroupBox();
			label9 = new Label();
			label11 = new Label();
			remixReleaseComboBox = new ComboBox();
			remixSourceComboBox = new ComboBox();
			InstallRTXRemixButton = new Button();
			groupBox11 = new GroupBox();
			ApplyPatchesButton = new Button();
			label8 = new Label();
			patchesSourceComboBox = new ComboBox();
			groupBox9 = new GroupBox();
			label6 = new Label();
			label10 = new Label();
			packageVersionComboBox = new ComboBox();
			packageSourceComboBox = new ComboBox();
			InstallFixesPackageButton = new Button();
			UpdateInstallButton = new Button();
			CreateInstallButton = new Button();
			label12 = new Label();
			ThisInstallPath = new Label();
			ThisInstallType = new Label();
			label15 = new Label();
			groupBox10 = new GroupBox();
			label7 = new Label();
			VanillaInstallPath = new Label();
			VanillaInstallType = new Label();
			label5 = new Label();
			AboutPage = new TabPage();
			groupBox8 = new GroupBox();
			linkLabel1 = new LinkLabel();
			label4 = new Label();
			groupBox7 = new GroupBox();
			LauncherUpdateSourceComboBox = new ComboBox();
			ReleaseNotesRichTextBox = new RichTextBox();
			InstallLauncherUpdateButton = new Button();
			progressBar1 = new ProgressBar();
			CheckForLauncherUpdatesButton = new Button();
			OpenGameInstallFolderButton = new Button();
			((System.ComponentModel.ISupportInitialize)settingsDataBindingSource).BeginInit();
			tabControl1.SuspendLayout();
			SettingsPage.SuspendLayout();
			groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)CustomWidthBox).BeginInit();
			((System.ComponentModel.ISupportInitialize)CustomHeightBox).BeginInit();
			QuickInstallGroup.SuspendLayout();
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
			InstallPage.SuspendLayout();
			groupBox13.SuspendLayout();
			groupBox12.SuspendLayout();
			groupBox11.SuspendLayout();
			groupBox9.SuspendLayout();
			groupBox10.SuspendLayout();
			AboutPage.SuspendLayout();
			groupBox8.SuspendLayout();
			groupBox7.SuspendLayout();
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
			LaunchGameButton.Location = new Point(180, 432);
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
			CloseButton.Location = new Point(279, 432);
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
			tabControl1.Controls.Add(InstallPage);
			tabControl1.Controls.Add(AboutPage);
			tabControl1.Location = new Point(6, 6);
			tabControl1.Name = "tabControl1";
			tabControl1.SelectedIndex = 0;
			tabControl1.Size = new Size(352, 420);
			tabControl1.TabIndex = 10;
			tabControl1.Selected += tabControl1_Selected;
			// 
			// SettingsPage
			// 
			SettingsPage.BackColor = SystemColors.Window;
			SettingsPage.Controls.Add(groupBox1);
			SettingsPage.Controls.Add(QuickInstallGroup);
			SettingsPage.Controls.Add(groupBox2);
			SettingsPage.Location = new Point(4, 24);
			SettingsPage.Name = "SettingsPage";
			SettingsPage.Padding = new Padding(3);
			SettingsPage.Size = new Size(344, 392);
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
			groupBox1.Size = new Size(332, 123);
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
			WidthHeightComboBox.Size = new Size(320, 23);
			WidthHeightComboBox.TabIndex = 3;
			WidthHeightComboBox.Text = "1920x1080";
			WidthHeightComboBox.SelectedIndexChanged += WidthHeightComboBox_SelectedIndexChanged;
			WidthHeightComboBox.TextChanged += WidthHeightComboBox_TextUpdate;
			// 
			// QuickInstallGroup
			// 
			QuickInstallGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			QuickInstallGroup.Controls.Add(label17);
			QuickInstallGroup.Controls.Add(label16);
			QuickInstallGroup.Controls.Add(label14);
			QuickInstallGroup.Controls.Add(OneClickEasyInstallButton);
			QuickInstallGroup.Controls.Add(label13);
			QuickInstallGroup.Location = new Point(6, 222);
			QuickInstallGroup.Name = "QuickInstallGroup";
			QuickInstallGroup.Size = new Size(332, 113);
			QuickInstallGroup.TabIndex = 7;
			QuickInstallGroup.TabStop = false;
			QuickInstallGroup.Text = "Quick Installer";
			QuickInstallGroup.Visible = false;
			QuickInstallGroup.Enter += groupBox2_Enter;
			// 
			// label17
			// 
			label17.AutoSize = true;
			label17.FlatStyle = FlatStyle.System;
			label17.Location = new Point(6, 64);
			label17.Name = "label17";
			label17.Size = new Size(297, 15);
			label17.TabIndex = 14;
			label17.Text = "You can head to the install tab if that's more your thing";
			// 
			// label16
			// 
			label16.AutoSize = true;
			label16.FlatStyle = FlatStyle.System;
			label16.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
			label16.Location = new Point(6, 49);
			label16.Name = "label16";
			label16.Size = new Size(81, 15);
			label16.TabIndex = 14;
			label16.Text = "It's that easy!";
			// 
			// label14
			// 
			label14.AutoSize = true;
			label14.FlatStyle = FlatStyle.System;
			label14.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
			label14.Location = new Point(6, 34);
			label14.Name = "label14";
			label14.Size = new Size(299, 15);
			label14.TabIndex = 13;
			label14.Text = "You can make one by clicking that button just below!";
			// 
			// OneClickEasyInstallButton
			// 
			OneClickEasyInstallButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			OneClickEasyInstallButton.FlatStyle = FlatStyle.System;
			OneClickEasyInstallButton.Location = new Point(6, 84);
			OneClickEasyInstallButton.Name = "OneClickEasyInstallButton";
			OneClickEasyInstallButton.Size = new Size(320, 23);
			OneClickEasyInstallButton.TabIndex = 11;
			OneClickEasyInstallButton.Text = "Run Quick Install";
			OneClickEasyInstallButton.UseVisualStyleBackColor = true;
			OneClickEasyInstallButton.Click += OneClickEasyInstallButton_Click;
			// 
			// label13
			// 
			label13.AutoSize = true;
			label13.FlatStyle = FlatStyle.System;
			label13.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
			label13.Location = new Point(6, 19);
			label13.Name = "label13";
			label13.Size = new Size(261, 15);
			label13.TabIndex = 12;
			label13.Text = "Hey! There's no Garry's Mod RTX Install here! ";
			// 
			// groupBox2
			// 
			groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			groupBox2.Controls.Add(EnableRTXCheckBox);
			groupBox2.Controls.Add(checkBox2);
			groupBox2.Location = new Point(6, 135);
			groupBox2.Name = "groupBox2";
			groupBox2.Size = new Size(332, 81);
			groupBox2.TabIndex = 7;
			groupBox2.TabStop = false;
			groupBox2.Text = "Garry's Mod";
			groupBox2.Enter += groupBox2_Enter;
			// 
			// EnableRTXCheckBox
			// 
			EnableRTXCheckBox.AutoSize = true;
			EnableRTXCheckBox.DataBindings.Add(new Binding("Enabled", settingsDataBindingSource, "RTXInstalled", true, DataSourceUpdateMode.OnPropertyChanged));
			EnableRTXCheckBox.FlatStyle = FlatStyle.System;
			EnableRTXCheckBox.Location = new Point(6, 48);
			EnableRTXCheckBox.Name = "EnableRTXCheckBox";
			EnableRTXCheckBox.Size = new Size(70, 20);
			EnableRTXCheckBox.TabIndex = 2;
			EnableRTXCheckBox.Text = "RTX On";
			EnableRTXCheckBox.UseVisualStyleBackColor = true;
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
			MountingPage.Size = new Size(344, 392);
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
			groupBox6.Size = new Size(332, 245);
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
			AdvancedPage.Size = new Size(344, 392);
			AdvancedPage.TabIndex = 1;
			AdvancedPage.Text = "Advanced";
			// 
			// groupBox5
			// 
			groupBox5.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			groupBox5.BackColor = SystemColors.Window;
			groupBox5.Controls.Add(checkBox6);
			groupBox5.Controls.Add(label3);
			groupBox5.Controls.Add(numericUpDown1);
			groupBox5.Controls.Add(checkBox3);
			groupBox5.FlatStyle = FlatStyle.System;
			groupBox5.Location = new Point(6, 88);
			groupBox5.Name = "groupBox5";
			groupBox5.Size = new Size(332, 107);
			groupBox5.TabIndex = 11;
			groupBox5.TabStop = false;
			groupBox5.Text = "Engine";
			// 
			// checkBox6
			// 
			checkBox6.AutoSize = true;
			checkBox6.DataBindings.Add(new Binding("Checked", settingsDataBindingSource, "DisableChromium", true, DataSourceUpdateMode.OnPropertyChanged));
			checkBox6.FlatStyle = FlatStyle.System;
			checkBox6.Location = new Point(6, 48);
			checkBox6.Name = "checkBox6";
			checkBox6.Size = new Size(131, 20);
			checkBox6.TabIndex = 5;
			checkBox6.Text = "Disable Chromium";
			checkBox6.UseVisualStyleBackColor = true;
			// 
			// label3
			// 
			label3.AutoSize = true;
			label3.Location = new Point(6, 76);
			label3.Name = "label3";
			label3.Size = new Size(55, 15);
			label3.TabIndex = 4;
			label3.Text = "DX Level:";
			// 
			// numericUpDown1
			// 
			numericUpDown1.DataBindings.Add(new Binding("Value", settingsDataBindingSource, "DXLevel", true, DataSourceUpdateMode.OnPropertyChanged));
			numericUpDown1.Location = new Point(67, 74);
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
			groupBox4.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			groupBox4.BackColor = SystemColors.Window;
			groupBox4.Controls.Add(checkBox5);
			groupBox4.Controls.Add(checkBox4);
			groupBox4.FlatStyle = FlatStyle.System;
			groupBox4.Location = new Point(6, 6);
			groupBox4.Name = "groupBox4";
			groupBox4.Size = new Size(332, 76);
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
			groupBox3.Location = new Point(6, 201);
			groupBox3.Name = "groupBox3";
			groupBox3.Size = new Size(332, 78);
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
			textBox1.Size = new Size(320, 46);
			textBox1.TabIndex = 1;
			// 
			// InstallPage
			// 
			InstallPage.BackColor = SystemColors.Window;
			InstallPage.Controls.Add(groupBox13);
			InstallPage.Controls.Add(groupBox10);
			InstallPage.Location = new Point(4, 24);
			InstallPage.Name = "InstallPage";
			InstallPage.Size = new Size(344, 392);
			InstallPage.TabIndex = 4;
			InstallPage.Text = "Install";
			// 
			// groupBox13
			// 
			groupBox13.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			groupBox13.Controls.Add(groupBox12);
			groupBox13.Controls.Add(groupBox11);
			groupBox13.Controls.Add(groupBox9);
			groupBox13.Controls.Add(UpdateInstallButton);
			groupBox13.Controls.Add(CreateInstallButton);
			groupBox13.Controls.Add(label12);
			groupBox13.Controls.Add(ThisInstallPath);
			groupBox13.Controls.Add(ThisInstallType);
			groupBox13.Controls.Add(label15);
			groupBox13.FlatStyle = FlatStyle.System;
			groupBox13.Location = new Point(6, 70);
			groupBox13.Name = "groupBox13";
			groupBox13.Size = new Size(332, 319);
			groupBox13.TabIndex = 6;
			groupBox13.TabStop = false;
			groupBox13.Text = "This RTX Install";
			// 
			// groupBox12
			// 
			groupBox12.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			groupBox12.Controls.Add(label9);
			groupBox12.Controls.Add(label11);
			groupBox12.Controls.Add(remixReleaseComboBox);
			groupBox12.Controls.Add(remixSourceComboBox);
			groupBox12.Controls.Add(InstallRTXRemixButton);
			groupBox12.FlatStyle = FlatStyle.System;
			groupBox12.Location = new Point(6, 81);
			groupBox12.Name = "groupBox12";
			groupBox12.Size = new Size(320, 83);
			groupBox12.TabIndex = 15;
			groupBox12.TabStop = false;
			groupBox12.Text = "NVIDIA RTX Remix";
			// 
			// label9
			// 
			label9.AutoSize = true;
			label9.FlatStyle = FlatStyle.System;
			label9.Location = new Point(6, 54);
			label9.Name = "label9";
			label9.Size = new Size(45, 15);
			label9.TabIndex = 4;
			label9.Text = "Version";
			// 
			// label11
			// 
			label11.AutoSize = true;
			label11.FlatStyle = FlatStyle.System;
			label11.Location = new Point(6, 25);
			label11.Name = "label11";
			label11.Size = new Size(43, 15);
			label11.TabIndex = 4;
			label11.Text = "Source";
			// 
			// remixReleaseComboBox
			// 
			remixReleaseComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			remixReleaseComboBox.FlatStyle = FlatStyle.System;
			remixReleaseComboBox.FormattingEnabled = true;
			remixReleaseComboBox.Items.AddRange(new object[] { "Error" });
			remixReleaseComboBox.Location = new Point(63, 51);
			remixReleaseComboBox.Name = "remixReleaseComboBox";
			remixReleaseComboBox.Size = new Size(139, 23);
			remixReleaseComboBox.TabIndex = 2;
			remixReleaseComboBox.Text = "...";
			// 
			// remixSourceComboBox
			// 
			remixSourceComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			remixSourceComboBox.FlatStyle = FlatStyle.System;
			remixSourceComboBox.FormattingEnabled = true;
			remixSourceComboBox.Items.AddRange(new object[] { "Xenthio/gmod-rtx-fixes-2 (Any)", "Xenthio/GMRTXClassic (gmod_main)" });
			remixSourceComboBox.Location = new Point(63, 22);
			remixSourceComboBox.Name = "remixSourceComboBox";
			remixSourceComboBox.Size = new Size(251, 23);
			remixSourceComboBox.TabIndex = 2;
			remixSourceComboBox.Text = "(OFFICIAL) NVIDIAGameWorks/rtx-remix";
			// 
			// InstallRTXRemixButton
			// 
			InstallRTXRemixButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			InstallRTXRemixButton.FlatStyle = FlatStyle.System;
			InstallRTXRemixButton.Location = new Point(208, 50);
			InstallRTXRemixButton.Name = "InstallRTXRemixButton";
			InstallRTXRemixButton.Size = new Size(106, 23);
			InstallRTXRemixButton.TabIndex = 13;
			InstallRTXRemixButton.Text = "Install/Update";
			InstallRTXRemixButton.UseVisualStyleBackColor = true;
			InstallRTXRemixButton.Click += InstallRTXRemixButton_Click;
			// 
			// groupBox11
			// 
			groupBox11.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			groupBox11.Controls.Add(ApplyPatchesButton);
			groupBox11.Controls.Add(label8);
			groupBox11.Controls.Add(patchesSourceComboBox);
			groupBox11.FlatStyle = FlatStyle.System;
			groupBox11.Location = new Point(6, 170);
			groupBox11.Name = "groupBox11";
			groupBox11.Size = new Size(320, 53);
			groupBox11.TabIndex = 15;
			groupBox11.TabStop = false;
			groupBox11.Text = "Binary Patches";
			// 
			// ApplyPatchesButton
			// 
			ApplyPatchesButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			ApplyPatchesButton.FlatStyle = FlatStyle.System;
			ApplyPatchesButton.Location = new Point(208, 22);
			ApplyPatchesButton.Name = "ApplyPatchesButton";
			ApplyPatchesButton.Size = new Size(106, 23);
			ApplyPatchesButton.TabIndex = 11;
			ApplyPatchesButton.Text = "Apply Patches";
			ApplyPatchesButton.UseVisualStyleBackColor = true;
			// 
			// label8
			// 
			label8.AutoSize = true;
			label8.FlatStyle = FlatStyle.System;
			label8.Location = new Point(6, 25);
			label8.Name = "label8";
			label8.Size = new Size(43, 15);
			label8.TabIndex = 4;
			label8.Text = "Source";
			// 
			// patchesSourceComboBox
			// 
			patchesSourceComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			patchesSourceComboBox.FlatStyle = FlatStyle.System;
			patchesSourceComboBox.FormattingEnabled = true;
			patchesSourceComboBox.Items.AddRange(new object[] { "Xenthio/gmod-rtx-fixes-2 (Any)", "Xenthio/GMRTXClassic (gmod_main)" });
			patchesSourceComboBox.Location = new Point(63, 22);
			patchesSourceComboBox.Name = "patchesSourceComboBox";
			patchesSourceComboBox.Size = new Size(139, 23);
			patchesSourceComboBox.TabIndex = 2;
			patchesSourceComboBox.Text = "BlueAmulet/SourceRTXTweaks";
			// 
			// groupBox9
			// 
			groupBox9.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			groupBox9.Controls.Add(label6);
			groupBox9.Controls.Add(label10);
			groupBox9.Controls.Add(packageVersionComboBox);
			groupBox9.Controls.Add(packageSourceComboBox);
			groupBox9.Controls.Add(InstallFixesPackageButton);
			groupBox9.FlatStyle = FlatStyle.System;
			groupBox9.Location = new Point(6, 229);
			groupBox9.Name = "groupBox9";
			groupBox9.Size = new Size(320, 83);
			groupBox9.TabIndex = 15;
			groupBox9.TabStop = false;
			groupBox9.Text = "Fixes Package";
			// 
			// label6
			// 
			label6.AutoSize = true;
			label6.FlatStyle = FlatStyle.System;
			label6.Location = new Point(6, 54);
			label6.Name = "label6";
			label6.Size = new Size(45, 15);
			label6.TabIndex = 4;
			label6.Text = "Version";
			// 
			// label10
			// 
			label10.AutoSize = true;
			label10.FlatStyle = FlatStyle.System;
			label10.Location = new Point(6, 25);
			label10.Name = "label10";
			label10.Size = new Size(43, 15);
			label10.TabIndex = 4;
			label10.Text = "Source";
			// 
			// packageVersionComboBox
			// 
			packageVersionComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			packageVersionComboBox.FlatStyle = FlatStyle.System;
			packageVersionComboBox.FormattingEnabled = true;
			packageVersionComboBox.Items.AddRange(new object[] { "gmod_x86-64 Xenthio/gmod-rtx-fixes-2", "gmod_x86-64 sambow23/gmod-rtx-binary (fork)", "gmod_main skurtyyskirts/GmodRTX", "gmod_main Xenthio/GMRTXClassic" });
			packageVersionComboBox.Location = new Point(63, 51);
			packageVersionComboBox.Name = "packageVersionComboBox";
			packageVersionComboBox.Size = new Size(139, 23);
			packageVersionComboBox.TabIndex = 2;
			packageVersionComboBox.Text = "...";
			// 
			// packageSourceComboBox
			// 
			packageSourceComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			packageSourceComboBox.FlatStyle = FlatStyle.System;
			packageSourceComboBox.FormattingEnabled = true;
			packageSourceComboBox.Items.AddRange(new object[] { "Xenthio/gmod-rtx-fixes-2 (Any)", "Xenthio/GMRTXClassic (gmod_main)" });
			packageSourceComboBox.Location = new Point(63, 22);
			packageSourceComboBox.Name = "packageSourceComboBox";
			packageSourceComboBox.Size = new Size(251, 23);
			packageSourceComboBox.TabIndex = 2;
			packageSourceComboBox.Text = "Xenthio/gmod-rtx-fixes-2 (Any)";
			// 
			// InstallFixesPackageButton
			// 
			InstallFixesPackageButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			InstallFixesPackageButton.FlatStyle = FlatStyle.System;
			InstallFixesPackageButton.Location = new Point(208, 51);
			InstallFixesPackageButton.Name = "InstallFixesPackageButton";
			InstallFixesPackageButton.Size = new Size(106, 23);
			InstallFixesPackageButton.TabIndex = 12;
			InstallFixesPackageButton.Text = "Install/Update";
			InstallFixesPackageButton.UseVisualStyleBackColor = true;
			// 
			// UpdateInstallButton
			// 
			UpdateInstallButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			UpdateInstallButton.FlatStyle = FlatStyle.System;
			UpdateInstallButton.Location = new Point(93, 52);
			UpdateInstallButton.Name = "UpdateInstallButton";
			UpdateInstallButton.Size = new Size(91, 23);
			UpdateInstallButton.TabIndex = 10;
			UpdateInstallButton.Text = "Update Install";
			UpdateInstallButton.UseVisualStyleBackColor = true;
			UpdateInstallButton.Click += UpdateInstallButton_ClickAsync;
			// 
			// CreateInstallButton
			// 
			CreateInstallButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			CreateInstallButton.Enabled = false;
			CreateInstallButton.FlatStyle = FlatStyle.System;
			CreateInstallButton.Location = new Point(190, 52);
			CreateInstallButton.Name = "CreateInstallButton";
			CreateInstallButton.Size = new Size(136, 23);
			CreateInstallButton.TabIndex = 8;
			CreateInstallButton.Text = " Create RTX Install Here";
			CreateInstallButton.UseVisualStyleBackColor = true;
			CreateInstallButton.Click += CreateInstallButton_ClickAsync;
			// 
			// label12
			// 
			label12.AutoSize = true;
			label12.FlatStyle = FlatStyle.System;
			label12.Location = new Point(6, 34);
			label12.Name = "label12";
			label12.Size = new Size(65, 15);
			label12.TabIndex = 4;
			label12.Text = "Install Path";
			// 
			// ThisInstallPath
			// 
			ThisInstallPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			ThisInstallPath.FlatStyle = FlatStyle.System;
			ThisInstallPath.Location = new Point(93, 34);
			ThisInstallPath.Name = "ThisInstallPath";
			ThisInstallPath.Size = new Size(233, 15);
			ThisInstallPath.TabIndex = 3;
			ThisInstallPath.Text = "Error/Unknown";
			// 
			// ThisInstallType
			// 
			ThisInstallType.AutoSize = true;
			ThisInstallType.FlatStyle = FlatStyle.System;
			ThisInstallType.Location = new Point(93, 19);
			ThisInstallType.Name = "ThisInstallType";
			ThisInstallType.Size = new Size(124, 15);
			ThisInstallType.TabIndex = 3;
			ThisInstallType.Text = "There is no install here";
			// 
			// label15
			// 
			label15.AutoSize = true;
			label15.FlatStyle = FlatStyle.System;
			label15.Location = new Point(6, 19);
			label15.Name = "label15";
			label15.Size = new Size(68, 15);
			label15.TabIndex = 2;
			label15.Text = "Install Type:";
			// 
			// groupBox10
			// 
			groupBox10.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			groupBox10.Controls.Add(label7);
			groupBox10.Controls.Add(VanillaInstallPath);
			groupBox10.Controls.Add(VanillaInstallType);
			groupBox10.Controls.Add(label5);
			groupBox10.FlatStyle = FlatStyle.System;
			groupBox10.Location = new Point(6, 6);
			groupBox10.Name = "groupBox10";
			groupBox10.Size = new Size(332, 58);
			groupBox10.TabIndex = 5;
			groupBox10.TabStop = false;
			groupBox10.Text = "Your Vanilla Garry's Mod Install";
			// 
			// label7
			// 
			label7.AutoSize = true;
			label7.FlatStyle = FlatStyle.System;
			label7.Location = new Point(6, 34);
			label7.Name = "label7";
			label7.Size = new Size(65, 15);
			label7.TabIndex = 4;
			label7.Text = "Install Path";
			// 
			// VanillaInstallPath
			// 
			VanillaInstallPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			VanillaInstallPath.FlatStyle = FlatStyle.System;
			VanillaInstallPath.Location = new Point(93, 34);
			VanillaInstallPath.Name = "VanillaInstallPath";
			VanillaInstallPath.Size = new Size(233, 15);
			VanillaInstallPath.TabIndex = 3;
			VanillaInstallPath.Text = "N/A";
			VanillaInstallPath.Click += VanillaInstallPath_Click;
			// 
			// VanillaInstallType
			// 
			VanillaInstallType.AutoSize = true;
			VanillaInstallType.FlatStyle = FlatStyle.System;
			VanillaInstallType.Location = new Point(93, 19);
			VanillaInstallType.Name = "VanillaInstallType";
			VanillaInstallType.Size = new Size(142, 15);
			VanillaInstallType.TabIndex = 3;
			VanillaInstallType.Text = "Not Installed / Not Found";
			// 
			// label5
			// 
			label5.AutoSize = true;
			label5.FlatStyle = FlatStyle.System;
			label5.Location = new Point(6, 19);
			label5.Name = "label5";
			label5.Size = new Size(68, 15);
			label5.TabIndex = 2;
			label5.Text = "Install Type:";
			// 
			// AboutPage
			// 
			AboutPage.BackColor = SystemColors.Window;
			AboutPage.Controls.Add(groupBox8);
			AboutPage.Controls.Add(groupBox7);
			AboutPage.Location = new Point(4, 24);
			AboutPage.Name = "AboutPage";
			AboutPage.Size = new Size(344, 392);
			AboutPage.TabIndex = 3;
			AboutPage.Text = "About";
			// 
			// groupBox8
			// 
			groupBox8.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			groupBox8.Controls.Add(linkLabel1);
			groupBox8.Controls.Add(label4);
			groupBox8.FlatStyle = FlatStyle.System;
			groupBox8.Location = new Point(6, 6);
			groupBox8.Name = "groupBox8";
			groupBox8.Size = new Size(332, 140);
			groupBox8.TabIndex = 3;
			groupBox8.TabStop = false;
			groupBox8.Text = "About";
			// 
			// linkLabel1
			// 
			linkLabel1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
			linkLabel1.AutoSize = true;
			linkLabel1.Location = new Point(6, 118);
			linkLabel1.Name = "linkLabel1";
			linkLabel1.Size = new Size(228, 15);
			linkLabel1.TabIndex = 1;
			linkLabel1.TabStop = true;
			linkLabel1.Text = "https://github.com/Xenthio/RTXLauncher";
			// 
			// label4
			// 
			label4.AutoSize = true;
			label4.FlatStyle = FlatStyle.System;
			label4.Location = new Point(6, 19);
			label4.Name = "label4";
			label4.Size = new Size(271, 75);
			label4.TabIndex = 0;
			label4.Text = "The Garry's Mod RTX Launcher\r\n\r\nBased on the original by CR for the x64 rtx project.\r\nWritten by Xenthio and CR\r\n";
			label4.Click += label4_Click;
			// 
			// groupBox7
			// 
			groupBox7.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			groupBox7.Controls.Add(LauncherUpdateSourceComboBox);
			groupBox7.Controls.Add(ReleaseNotesRichTextBox);
			groupBox7.Controls.Add(InstallLauncherUpdateButton);
			groupBox7.Controls.Add(progressBar1);
			groupBox7.Controls.Add(CheckForLauncherUpdatesButton);
			groupBox7.Location = new Point(6, 152);
			groupBox7.Name = "groupBox7";
			groupBox7.Size = new Size(332, 237);
			groupBox7.TabIndex = 2;
			groupBox7.TabStop = false;
			groupBox7.Text = "Updates";
			// 
			// LauncherUpdateSourceComboBox
			// 
			LauncherUpdateSourceComboBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			LauncherUpdateSourceComboBox.FormattingEnabled = true;
			LauncherUpdateSourceComboBox.Location = new Point(6, 208);
			LauncherUpdateSourceComboBox.Name = "LauncherUpdateSourceComboBox";
			LauncherUpdateSourceComboBox.Size = new Size(106, 23);
			LauncherUpdateSourceComboBox.TabIndex = 11;
			// 
			// ReleaseNotesRichTextBox
			// 
			ReleaseNotesRichTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			ReleaseNotesRichTextBox.Location = new Point(6, 51);
			ReleaseNotesRichTextBox.Name = "ReleaseNotesRichTextBox";
			ReleaseNotesRichTextBox.Size = new Size(320, 151);
			ReleaseNotesRichTextBox.TabIndex = 4;
			ReleaseNotesRichTextBox.Text = "";
			// 
			// InstallLauncherUpdateButton
			// 
			InstallLauncherUpdateButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			InstallLauncherUpdateButton.FlatStyle = FlatStyle.System;
			InstallLauncherUpdateButton.Location = new Point(118, 208);
			InstallLauncherUpdateButton.Name = "InstallLauncherUpdateButton";
			InstallLauncherUpdateButton.Size = new Size(75, 23);
			InstallLauncherUpdateButton.TabIndex = 3;
			InstallLauncherUpdateButton.Text = "Install";
			InstallLauncherUpdateButton.UseVisualStyleBackColor = true;
			// 
			// progressBar1
			// 
			progressBar1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			progressBar1.Location = new Point(6, 22);
			progressBar1.Name = "progressBar1";
			progressBar1.Size = new Size(320, 23);
			progressBar1.TabIndex = 2;
			// 
			// CheckForLauncherUpdatesButton
			// 
			CheckForLauncherUpdatesButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			CheckForLauncherUpdatesButton.FlatStyle = FlatStyle.System;
			CheckForLauncherUpdatesButton.Location = new Point(199, 208);
			CheckForLauncherUpdatesButton.Name = "CheckForLauncherUpdatesButton";
			CheckForLauncherUpdatesButton.Size = new Size(127, 23);
			CheckForLauncherUpdatesButton.TabIndex = 1;
			CheckForLauncherUpdatesButton.Text = "Check for Updates";
			CheckForLauncherUpdatesButton.UseVisualStyleBackColor = true;
			// 
			// OpenGameInstallFolderButton
			// 
			OpenGameInstallFolderButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
			OpenGameInstallFolderButton.Location = new Point(10, 432);
			OpenGameInstallFolderButton.Name = "OpenGameInstallFolderButton";
			OpenGameInstallFolderButton.Size = new Size(120, 23);
			OpenGameInstallFolderButton.TabIndex = 8;
			OpenGameInstallFolderButton.Text = "Open Install Folder";
			OpenGameInstallFolderButton.UseVisualStyleBackColor = true;
			OpenGameInstallFolderButton.Click += OpenGameInstallFolderButton_Click;
			// 
			// Form1
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(364, 461);
			Controls.Add(OpenGameInstallFolderButton);
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
			QuickInstallGroup.ResumeLayout(false);
			QuickInstallGroup.PerformLayout();
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
			InstallPage.ResumeLayout(false);
			groupBox13.ResumeLayout(false);
			groupBox13.PerformLayout();
			groupBox12.ResumeLayout(false);
			groupBox12.PerformLayout();
			groupBox11.ResumeLayout(false);
			groupBox11.PerformLayout();
			groupBox9.ResumeLayout(false);
			groupBox9.PerformLayout();
			groupBox10.ResumeLayout(false);
			groupBox10.PerformLayout();
			AboutPage.ResumeLayout(false);
			groupBox8.ResumeLayout(false);
			groupBox8.PerformLayout();
			groupBox7.ResumeLayout(false);
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
		private TabPage MountingPage;
		private GroupBox groupBox6;
		private GameMountCheckbox MountHL2RTXCheckbox;
		private GameMountCheckbox MountPortalPreludeRTXCheckBox;
		private GameMountCheckbox MountPortalRTXCheckbox;
		private GameMountCheckbox MountP2RTXCheckBox;
		private TabPage AboutPage;
		private Label label4;
		private GroupBox groupBox7;
		private Button CheckForLauncherUpdatesButton;
		private Button InstallLauncherUpdateButton;
		private ProgressBar progressBar1;
		private GroupBox groupBox8;
		private LinkLabel linkLabel1;
		private TabPage InstallPage;
		private GroupBox groupBox10;
		private Label label7;
		private Label VanillaInstallType;
		private Label label5;
		private Label VanillaInstallPath;
		private Label label9;
		private ComboBox remixReleaseComboBox;
		private Label label10;
		private ComboBox packageSourceComboBox;
		private Button CreateInstallButton;
		private GroupBox groupBox13;
		private Label label12;
		private Label ThisInstallPath;
		private Label ThisInstallType;
		private Label label15;
		private Button InstallRTXRemixButton;
		private Button InstallFixesPackageButton;
		private Button ApplyPatchesButton;
		private Button UpdateInstallButton;
		private GroupBox groupBox9;
		private GroupBox groupBox12;
		private GroupBox groupBox11;
		private Label label6;
		private ComboBox packageVersionComboBox;
		private Label label8;
		private ComboBox patchesSourceComboBox;
		private Label label11;
		private ComboBox remixSourceComboBox;
		private Button OneClickEasyInstallButton;
		private GroupBox QuickInstallGroup;
		private Label label14;
		private Label label13;
		private Label label17;
		private Label label16;
		private RichTextBox ReleaseNotesRichTextBox;
		private ComboBox LauncherUpdateSourceComboBox;
		private Button OpenGameInstallFolderButton;
		private CheckBox EnableRTXCheckBox;
		private CheckBox checkBox6;
	}
}
