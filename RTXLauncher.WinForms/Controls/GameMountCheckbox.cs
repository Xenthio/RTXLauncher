// In your RTXLauncher.WinForms project, probably in a 'Controls' folder.
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using RTXLauncher.Core.Utilities;
using System.ComponentModel;

namespace RTXLauncher.WinForms.Controls;

public class GameMountCheckbox : CheckBox, ISupportInitialize
{
	// --- Public Properties (Set in the Form Designer) ---
	[Category("Mounting")]
	[Description("The name of the game's root content folder (e.g., 'hl2rtx').")]
	public string GameFolder { get; set; } = "hl2rtx";

	[Category("Mounting")]
	[Description("The name of the game's installation folder in steamapps/common (e.g., 'Half-Life 2 RTX').")]
	public string InstallFolder { get; set; } = "Half-Life 2: RTX";

	[Category("Mounting")]
	[Description("The name of the RTX Remix mod folder (e.g., 'hl2rtx').")]
	public string RemixModFolder { get; set; } = "hl2rtx";

	// --- Service Dependency ---
	private MountingService? _mountingService;

	public GameMountCheckbox()
	{
		// The service will be provided later via the Initialize method.
	}

	// This method will be called from Form1 to provide the service.
	public void Initialize(MountingService mountingService)
	{
		_mountingService = mountingService;

		// --- Initial State Setup ---
		// Use utilities for checking status.
		this.Enabled = SteamLibraryUtility.GetGameInstallFolder(InstallFolder) != null;
		this.Checked = MountingUtility.IsGameMounted(GameFolder, RemixModFolder);

		// Wire up the event handler
		this.Click -= OnClick; // Ensure it's not wired up multiple times
		this.Click += OnClick;
	}

	private async void OnClick(object? sender, EventArgs e)
	{
		if (_mountingService == null)
		{
			MessageBox.Show("Mounting service has not been initialized.", "Internal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		// Prevent user from clicking again while the operation is in progress
		this.Enabled = false;

		try
		{
			if (this.Checked)
			{
				await HandleMounting();
			}
			else
			{
				HandleUnmounting();
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"An error occurred: {ex.Message}", "Operation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
			// Revert the checkbox state on failure
			this.Checked = !this.Checked;
		}
		finally
		{
			// Re-enable the checkbox based on whether the game is installed
			this.Enabled = SteamLibraryUtility.GetGameInstallFolder(InstallFolder) != null;
		}
	}

	private async Task HandleMounting()
	{
		// Your existing confirmation dialog logic can be placed here.
		var confirmResult = MessageBox.Show($"Are you sure you want to mount {InstallFolder}?\n\nThis will create symbolic links in your Garry's Mod addons folder.",
			"Confirm Mount", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

		if (confirmResult != DialogResult.Yes)
		{
			this.Checked = false; // Revert if user cancels
			return;
		}

		var progressForm = new ProgressForm { Text = $"Mounting {this.Text}" };
		var progress = new Progress<InstallProgressReport>(report => progressForm.UpdateProgress(report.Message, report.Percentage));

		progressForm.Show(this.FindForm());

		try
		{
			await _mountingService.MountGameAsync(this.Text, GameFolder, InstallFolder, RemixModFolder, progress);
		}
		catch (Exception)
		{
			this.Checked = false; // Revert on failure
			throw; // Re-throw to be caught by the outer handler
		}
		finally
		{
			progressForm.Close();
		}
	}

	private void HandleUnmounting()
	{
		var confirmResult = MessageBox.Show($"Are you sure you want to unmount {InstallFolder}?",
			"Confirm Unmount", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

		if (confirmResult == DialogResult.Yes)
		{
			_mountingService.UnmountGame(GameFolder, RemixModFolder);
		}
		else
		{
			this.Checked = true; // Revert if user cancels
		}
	}

	// --- ISupportInitialize Implementation ---
	// This is a standard WinForms pattern to run code after designer properties have been set.
	// We don't use it here since we are initializing manually from Form1.
	public void BeginInit() { }
	public void EndInit() { }
}