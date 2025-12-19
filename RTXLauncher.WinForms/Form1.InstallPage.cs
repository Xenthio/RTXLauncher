using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using RTXLauncher.Core.Utilities;

namespace RTXLauncher.WinForms
{
	partial class Form1
	{
		private bool _hasInitInstallPage = false;
		private void InitInstallPage()
		{
			if (_hasInitInstallPage) return;
			_hasInitInstallPage = true;
			RefreshPackageInfo();
			RefreshInstallInfo();

			InstallRTXRemixButton.Click += InstallRTXRemixButton_Click;
			InstallFixesPackageButton.Click += InstallFixesPackageButton_ClickAsync;
			ApplyPatchesButton.Click += ApplyPatchesButton_ClickAsync;

			remixSourceComboBox.SelectedIndexChanged += RemixSourceComboBox_SelectedIndexChanged;
			packageSourceComboBox.SelectedIndexChanged += PackageSourceComboBox_SelectedIndexChanged;
		}
		private void RefreshInstallInfo()
		{
			// Refresh the install info

			var vanillapath = GarrysModUtility.GetVanillaInstallFolder();
			var vanillatype = GarrysModUtility.GetInstallType(vanillapath);
			VanillaInstallType.Text = vanillatype;
			VanillaInstallPath.Text = vanillapath;

			if (vanillatype == "unknown") VanillaInstallType.Text = "Not installed / not found";

			var thispath = GarrysModUtility.GetThisInstallFolder();
			var thistype = GarrysModUtility.GetInstallType(thispath);
			ThisInstallType.Text = thistype;
			ThisInstallPath.Text = thispath;

			if (thistype == "unknown")
			{
				ThisInstallType.Text = "There's no install here, create one!";
				CreateInstallButton.Enabled = true;
				UpdateInstallButton.Enabled = false;
			}
			else
			{
				CreateInstallButton.Enabled = false;
				UpdateInstallButton.Enabled = true;
			}

			// Update visibility of the QuickInstallGroup
			UpdateQuickInstallGroupVisibility();
		}

		private async void RefreshPackageInfo()
		{
			// Simplified version
			PopulateComboBox(remixSourceComboBox, PackageInstallService.RemixSources.Keys.ToList());
			await PopulateReleasesComboBoxAsync(remixSourceComboBox, remixReleaseComboBox, PackageInstallService.RemixSources);

			PopulateComboBox(packageSourceComboBox, PackageInstallService.PackageSources.Keys.ToList());
			await PopulateReleasesComboBoxAsync(packageSourceComboBox, packageVersionComboBox, PackageInstallService.PackageSources.ToDictionary(kvp => kvp.Key, kvp => (kvp.Value.Owner, kvp.Value.Repo)));

			PopulateComboBox(patchesSourceComboBox, PackageInstallService.PatchSources.Keys.ToList());
		}


		private async void RemixSourceComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			// When the remix source changes, update the remix releases
			await PopulateReleasesComboBoxAsync(remixSourceComboBox, remixReleaseComboBox, PackageInstallService.RemixSources);
		}

		private async void PackageSourceComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			// When the package source changes, update the package versions
			// We need to convert the dictionary's value type to match the helper method's signature.
			var packageSourceDict = PackageInstallService.PackageSources.ToDictionary(kvp => kvp.Key, kvp => (kvp.Value.Owner, kvp.Value.Repo));
			await PopulateReleasesComboBoxAsync(packageSourceComboBox, packageVersionComboBox, packageSourceDict);
		}


		// --- Generic ComboBox Helpers ---
		private void PopulateComboBox(ComboBox box, List<string> items)
		{
			box.Items.Clear();
			box.Items.AddRange(items.ToArray());
			if (box.Items.Count > 0) box.SelectedIndex = 0;
		}

		private async Task PopulateReleasesComboBoxAsync(ComboBox sourceBox, ComboBox targetBox, Dictionary<string, (string Owner, string Repo)> sourceDict)
		{
			targetBox.Enabled = false;
			targetBox.Items.Clear();
			targetBox.Items.Add("Loading...");
			targetBox.SelectedIndex = 0;

			try
			{
				if (sourceBox.SelectedItem is not string selectedSource || !sourceDict.TryGetValue(selectedSource, out var sourceInfo)) return;
				var releases = await _githubService.FetchReleasesAsync(sourceInfo.Owner, sourceInfo.Repo);

				targetBox.Items.Clear();
				foreach (var release in releases.OrderByDescending(r => r.PublishedAt))
				{
					targetBox.Items.Add(release);
				}
				if (targetBox.Items.Count > 0) targetBox.SelectedIndex = 0;
			}
			catch (Exception ex)
			{
				targetBox.Items.Clear();
				targetBox.Items.Add($"Error: {ex.Message}");
			}
			finally { targetBox.Enabled = true; }
		}

		// --- Button Click Implementations ---

		private async void CreateInstallButton_ClickAsync(object sender, EventArgs e)
		{
			var vanillaPath = GarrysModUtility.GetVanillaInstallFolder();
			if (string.IsNullOrEmpty(vanillaPath))
			{
				MessageBox.Show("Could not find vanilla Garry's Mod installation.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			var progressForm = new ProgressForm { Text = "Creating GMod Install" };
			var progress = new Progress<InstallProgressReport>(report => progressForm.UpdateProgress(report.Message, report.Percentage));
			try
			{
				CreateInstallButton.Enabled = false;
				progressForm.Show(this);
				await _gmodInstallService.CreateNewGmodInstallAsync(vanillaPath, GarrysModUtility.GetThisInstallFolder(), progress);
				MessageBox.Show("Installation complete!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (SymlinkFailedException ex)
			{
				var result = MessageBox.Show($"{ex.Message}\n\nDo you want to try restarting as administrator?", "Permissions Error", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
				// if (result == DialogResult.Yes) { /* Logic to restart as admin */ }
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Installation failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				progressForm.Close();
				RefreshInstallInfo();
			}
		}

		private async void InstallRTXRemixButton_Click(object sender, EventArgs e)
		{
			if (remixReleaseComboBox.SelectedItem is not GitHubRelease release) { /* Show error */ return; }

			var installDir = GarrysModUtility.GetThisInstallFolder();

			// Check for existing rtx.conf and prompt for backup
			if (RemixUtility.RtxConfigExists(installDir))
			{
				var result = MessageBox.Show(
					"An existing rtx.conf file was detected. Would you like to back it up before installing?\n\n" +
					"The backup will be saved as rtx.conf.backup_[timestamp]",
					"RTX Config Found",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question);

				if (result == DialogResult.Yes)
				{
					var backupPath = RemixUtility.BackupRtxConfig(installDir);
					if (backupPath != null)
					{
						MessageBox.Show($"Backed up rtx.conf to {Path.GetFileName(backupPath)}", "Backup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					else
					{
						MessageBox.Show("Failed to backup rtx.conf. Installation will continue.", "Backup Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
				}
			}

			var progressForm = new ProgressForm { Text = "Installing RTX Remix" };
			var progress = new Progress<InstallProgressReport>(report => progressForm.UpdateProgress(report.Message, report.Percentage));
			try
			{
				InstallRTXRemixButton.Enabled = false;
				progressForm.Show(this);
				await _packageInstallService.InstallRemixPackageAsync(release, installDir, progress);
				MessageBox.Show("RTX Remix installed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex) { MessageBox.Show($"Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
			finally { progressForm.Close(); InstallRTXRemixButton.Enabled = true; }
		}
		private async void UpdateInstallButton_ClickAsync(object sender, EventArgs e)
		{
			// Disable the button to prevent multiple update operations
			UpdateInstallButton.Enabled = false;

			try
			{
				// TODO: Updating gmod install logic
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Update failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				// Re-enable the button when done
				UpdateInstallButton.Enabled = true;
			}
		}

		private async void InstallFixesPackageButton_ClickAsync(object sender, EventArgs e)
		{
			if (packageVersionComboBox.SelectedItem is not GitHubRelease release) { /* Show error */ return; }

			var installDir = GarrysModUtility.GetThisInstallFolder();

			// Check for existing rtx.conf and prompt for backup
			if (RemixUtility.RtxConfigExists(installDir))
			{
				var result = MessageBox.Show(
					"An existing rtx.conf file was detected. Would you like to back it up before installing?\n\n" +
					"The backup will be saved as rtx.conf.backup_[timestamp]",
					"RTX Config Found",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question);

				if (result == DialogResult.Yes)
				{
					var backupPath = RemixUtility.BackupRtxConfig(installDir);
					if (backupPath != null)
					{
						MessageBox.Show($"Backed up rtx.conf to {Path.GetFileName(backupPath)}", "Backup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					else
					{
						MessageBox.Show("Failed to backup rtx.conf. Installation will continue.", "Backup Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
				}
			}

			var progressForm = new ProgressForm { Text = "Installing Fixes Package" };
			var progress = new Progress<InstallProgressReport>(report => progressForm.UpdateProgress(report.Message, report.Percentage));
			try
			{
				InstallFixesPackageButton.Enabled = false;
				progressForm.Show(this);
				await _packageInstallService.InstallStandardPackageAsync(release, installDir, PackageInstallService.DefaultIgnorePatterns, progress);
				MessageBox.Show("Fixes package installed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex) { MessageBox.Show($"Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
			finally { progressForm.Close(); InstallFixesPackageButton.Enabled = true; }
		}

		private async void ApplyPatchesButton_ClickAsync(object sender, EventArgs e)
		{
			if (patchesSourceComboBox.SelectedItem is not string source || !PackageInstallService.PatchSources.TryGetValue(source, out var sourceInfo)) { /* Show error */ return; }

			var progressForm = new ProgressForm { Text = "Applying Patches" };
			var progress = new Progress<InstallProgressReport>(report => progressForm.UpdateProgress(report.Message, report.Percentage));
			try
			{
				ApplyPatchesButton.Enabled = false;
				progressForm.Show(this);
				await _patchingService.ApplyPatchesAsync(sourceInfo.Owner, sourceInfo.Repo, sourceInfo.FilePath, GarrysModUtility.GetThisInstallFolder(), progress, sourceInfo.Branch);
				MessageBox.Show("Patches applied!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex) { MessageBox.Show($"Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
			finally { progressForm.Close(); ApplyPatchesButton.Enabled = true; }
		}
	}
}
