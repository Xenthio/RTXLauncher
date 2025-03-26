/*namespace RTXLauncher;

public static class Symlinker
{
	private static bool _userAcceptedSymlinkFailures = false;

	public static bool CreateDirectorySymbolicLink(string path, string pathToTarget)
	{
		try
		{
			// Attempt to create a symbolic link
			Directory.CreateSymbolicLink(path, pathToTarget);
			return true;
		}
		catch (UnauthorizedAccessException)
		{
			// If symlink fails, check if the user wants to continue
			if (!_userAcceptedSymlinkFailures)
			{
				bool continueOperation = PromptSymlinkFailure(
				$"Failed to create directory symlink to {Path.GetFileName(path)}. " +
				"This may be due to insufficient privileges.\n\n" +
				"Content mounting requires symbolic links to function correctly.\n\n" +
				"Do you want to run RTX Launcher as administrator and try again?");

				if (continueOperation)
				{
					// Restart as admin
					RestartAsAdmin();
					// We won't actually get past this point if restart succeeds
				}
				else
				{
					_userAcceptedSymlinkFailures = true;
				}
			}

			LogProgress($"ERROR: Insufficient privileges to create symlink: {Path.GetFileName(path)}", 0);
			return false;
		}
		catch (Exception ex)
		{
			// For other errors, also check if the user wants to continue
			if (!_userAcceptedSymlinkFailures)
			{
				bool continueInstallation = PromptSymlinkFailure(
				$"Failed to create directory symlink to {Path.GetFileName(path)}. " +
				$"Error: {ex.Message}\n\n" +
				"Do you want to continue with the installation without this symlink?");

				if (!continueInstallation)
				{
					throw new OperationCanceledException("Installation cancelled due to symlink creation failure.");
				}
			}

			// Log other errors but don't stop installation if user agreed to continue
			LogProgress($"  Error creating symlink: {ex.Message}", 0);
			return false;
		}
	}

	public static bool CreateFileSymbolicLink(string path, string pathToTarget)
	{
		try
		{
			// Attempt to create a symbolic link
			File.CreateSymbolicLink(path, pathToTarget);
			return true;
		}
		catch (UnauthorizedAccessException)
		{
			// If symlink fails, check if the user wants to continue
			if (!_userAcceptedSymlinkFailures)
			{
				bool continueOperation = PromptSymlinkFailure(
				$"Failed to create file symlink to {Path.GetFileName(path)}. " +
				"This may be due to insufficient privileges.\n\n" +
				"Content mounting requires symbolic links to function correctly.\n\n" +
				"Do you want to run RTX Launcher as administrator and try again?");

				if (continueOperation)
				{
					// Restart as admin
					RestartAsAdmin();
					// We won't actually get past this point if restart succeeds
				}
				else
				{
					_userAcceptedSymlinkFailures = true;
				}
			}

			LogProgress($"ERROR: Insufficient privileges to create symlink: {Path.GetFileName(path)}", 0);
			return false;
		}
		catch (Exception ex)
		{
			// For other errors, also check if the user wants to continue
			if (!_userAcceptedSymlinkFailures)
			{
				bool continueInstallation = PromptSymlinkFailure(
				$"Failed to create file symlink to {Path.GetFileName(path)}. " +
				$"Error: {ex.Message}\n\n" +
				"Do you want to continue with the installation without this symlink?");

				if (!continueInstallation)
				{
					throw new OperationCanceledException("Installation cancelled due to symlink creation failure.");
				}
			}

			// Log other errors but don't stop installation if user agreed to continue
			LogProgress($"  Error creating symlink: {ex.Message}", 0);
			return false;
		}
	}

	private static void RestartAsAdmin()
	{
		var exeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
		var startInfo = new System.Diagnostics.ProcessStartInfo(exeName)
		{
			Verb = "runas"
		};

		try
		{
			System.Diagnostics.Process.Start(startInfo);
			System.Windows.Forms.Application.Exit();
		}
		catch (Exception ex)
		{
			LogProgress($"Failed to restart as admin: {ex.Message}", 0);
		}
	}

	// Helper method to prompt the user about symlink failures
	private static bool PromptSymlinkFailure(string message)
	{
		// We need to ensure this runs on the UI thread
		bool result = false;

		var task = Task.Factory.StartNew(() =>
		{
			DialogResult dialogResult = MessageBox.Show(
				message,
				"Symlink Creation Failed",
				MessageBoxButtons.OKCancel,
				MessageBoxIcon.Warning);

			result = (dialogResult == DialogResult.OK);

			// Set the flag if user clicked OK to prevent future prompts
			if (result)
			{
				_userAcceptedSymlinkFailures = true;
			}

		}, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

		task.Wait();
		return result;
	}

	// Log a message with progress update
	private static void LogProgress(string message, int progress)
	{
		System.Diagnostics.Debug.WriteLine(message);
		// You can add an event or other logging mechanism here if needed
	}
}*/