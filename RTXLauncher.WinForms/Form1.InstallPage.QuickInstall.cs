// Form1.InstallPage.QuickInstall.cs
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using RTXLauncher.Core.Utilities;

namespace RTXLauncher.WinForms
{
	public partial class Form1
	{
		private void UpdateQuickInstallGroupVisibility()
		{
			QuickInstallGroup.Visible = GarrysModUtility.GetInstallType(GarrysModUtility.GetThisInstallFolder()) == "unknown";
		}

		private async void OneClickEasyInstallButton_Click(object sender, EventArgs e)
		{
			var confirm = MessageBox.Show("This will perform a complete installation with recommended settings. Continue?", "Quick Install", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (confirm != DialogResult.Yes) return;

			var progressForm = new ProgressForm { Text = "Performing Quick Install" };
			var progress = new Progress<InstallProgressReport>(report => progressForm.UpdateProgress(report.Message, report.Percentage));

			try
			{
				OneClickEasyInstallButton.Enabled = false;
				progressForm.Show(this);

				// Step 1: Create Install if needed
				var installDir = GarrysModUtility.GetThisInstallFolder();
				if (GarrysModUtility.GetInstallType(installDir) == "unknown")
				{
					await _gmodInstallService.CreateNewGmodInstallAsync(GarrysModUtility.GetVanillaInstallFolder(), installDir, progress);
				}

				// Step 2: Install latest Remix
				var remixReleases = await _githubService.FetchReleasesAsync("sambow23", "dxvk-remix-gmod");
				var latestRemix = remixReleases.OrderByDescending(r => r.PublishedAt).FirstOrDefault();
				if (latestRemix != null)
				{
					await _packageInstallService.InstallRemixPackageAsync(latestRemix, installDir, progress);
				}

				// Step 3: Apply recommended patches
				var (owner, repo, file) = PackageInstallService.PatchSources["sambow23/SourceRTXTweaks"];
				await _patchingService.ApplyPatchesAsync(owner, repo, file, installDir, progress);

				// Step 4: Install recommended fixes
				var fixesReleases = await _githubService.FetchReleasesAsync("Xenthio", "gmod-rtx-fixes-2");
				var latestFixes = fixesReleases.OrderByDescending(r => r.PublishedAt).FirstOrDefault();
				if (latestFixes != null)
				{
					await _packageInstallService.InstallStandardPackageAsync(latestFixes, installDir, PackageInstallService.DefaultIgnorePatterns, progress);
				}

				MessageBox.Show("Quick Install completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Quick Install failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				progressForm.Close();
				OneClickEasyInstallButton.Enabled = true;
				RefreshInstallInfo();
			}
		}
	}
}