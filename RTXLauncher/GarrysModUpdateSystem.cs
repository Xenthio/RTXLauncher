namespace RTXLauncher
{
	public static class GarrysModUpdateSystem
	{
		// Class to hold file information
		public class FileUpdateInfo
		{
			public string RelativePath { get; set; }
			public string SourcePath { get; set; }
			public string DestinationPath { get; set; }
			public bool IsDirectory { get; set; }
			public bool IsNew { get; set; }
			public bool IsChanged { get; set; }
			public long Size { get; set; }
			public DateTime SourceLastModified { get; set; }
			public DateTime? DestLastModified { get; set; }

			public string Status => IsNew ? "New" : (IsChanged ? "Changed" : "Same");
		}

		// Check for updates between source and destination
		public static List<FileUpdateInfo> DetectUpdates(string sourceDir, string destDir)
		{
			var result = new List<FileUpdateInfo>();

			// Exclude these directories from update checks
			var excludedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"addons", "saves", "dupes", "demos", "settings", "cache",
				"materials", "models", "maps", "screenshots", "videos", "download"
			};

			// Exclude these file extensions
			var excludedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				".dem", ".log", ".vpk"
			};

			// Scan directories recursively
			CheckDirectory(sourceDir, destDir, "", result, excludedDirs, excludedExtensions);

			return result;
		}

		private static void CheckDirectory(string sourceDir, string destDir, string relativePath,
			List<FileUpdateInfo> results, HashSet<string> excludedDirs, HashSet<string> excludedExtensions)
		{
			// Get source directory info
			var sourceDirInfo = new DirectoryInfo(Path.Combine(sourceDir, relativePath));
			if (!sourceDirInfo.Exists) return;

			// Additional directories to exclude at the root level
			var additionalRootExcludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"crashes", "logs", "temp", "update", "xenmod"
			};


			// Check if this directory should be excluded
			if (string.IsNullOrEmpty(relativePath) || !excludedDirs.Contains(relativePath.Split('\\', '/').First()))
			{
				// Check files in this directory
				foreach (var sourceFile in sourceDirInfo.GetFiles())
				{
					// Skip excluded file extensions
					if (excludedExtensions.Contains(sourceFile.Extension))
						continue;

					var fileRelativePath = string.IsNullOrEmpty(relativePath)
						? sourceFile.Name
						: Path.Combine(relativePath, sourceFile.Name);

					// For root directory, only include gmod.exe
					if (string.IsNullOrEmpty(relativePath) &&
						!string.Equals(sourceFile.Name, "gmod.exe", StringComparison.OrdinalIgnoreCase) &&
						!string.Equals(sourceFile.Name, "hl2.exe", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					var destFilePath = Path.Combine(destDir, fileRelativePath);
					var fileInfo = new FileInfo(destFilePath);

					bool isNew = !fileInfo.Exists;

					// Compare files if the destination exists
					var comparisonResult = isNew
						? new FileComparisonResult { IsNew = true }
						: CompareFiles(sourceFile, fileInfo);

					// Add any new or changed file to results
					if (comparisonResult.IsNew || comparisonResult.IsChanged)
					{
						results.Add(new FileUpdateInfo
						{
							RelativePath = fileRelativePath,
							SourcePath = sourceFile.FullName,
							DestinationPath = destFilePath,
							IsDirectory = false,
							IsNew = comparisonResult.IsNew,
							IsChanged = comparisonResult.IsChanged,
							Size = sourceFile.Length,
							SourceLastModified = sourceFile.LastWriteTime,
							DestLastModified = isNew ? null : (DateTime?)fileInfo.LastWriteTime
						});
					}
				}

				// Recursively check subdirectories
				foreach (var sourceSubDir in sourceDirInfo.GetDirectories())
				{
					var subDirRelativePath = string.IsNullOrEmpty(relativePath)
						? sourceSubDir.Name
						: Path.Combine(relativePath, sourceSubDir.Name);

					// Skip excluded directories
					if (excludedDirs.Contains(sourceSubDir.Name))
						continue;

					// Skip additional root-level excluded directories
					if (string.IsNullOrEmpty(relativePath) && additionalRootExcludes.Contains(sourceSubDir.Name))
						continue;

					var destSubDir = Path.Combine(destDir, subDirRelativePath);

					// Check if directory is new
					if (!Directory.Exists(destSubDir))
					{
						results.Add(new FileUpdateInfo
						{
							RelativePath = subDirRelativePath,
							SourcePath = sourceSubDir.FullName,
							DestinationPath = destSubDir,
							IsDirectory = true,
							IsNew = true,
							IsChanged = false,
							Size = 0,
							SourceLastModified = sourceSubDir.LastWriteTime
						});
					}

					// Recursively check this subdirectory
					CheckDirectory(sourceDir, destDir, subDirRelativePath, results, excludedDirs, excludedExtensions);
				}
			}
		}

		// Class to hold file comparison results
		private class FileComparisonResult
		{
			public bool IsNew { get; set; }
			public bool IsChanged { get; set; }
		}

		// Compares two files and determines if they're different
		private static FileComparisonResult CompareFiles(FileInfo sourceFile, FileInfo destFile)
		{
			var result = new FileComparisonResult();

			// lmao my symlinked gwater 2 install was causing issues so I added this debug code
			if (false && destFile.Name.Equals("gmcl_gwater2_win64.dll", StringComparison.OrdinalIgnoreCase))
			{
				string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "file_comparison_log.txt");
				using (StreamWriter writer = new StreamWriter(logPath, true))
				{
					writer.WriteLine("=== DEBUG INFO FOR PROBLEM FILE ===");
					writer.WriteLine($"Time: {DateTime.Now}");
					writer.WriteLine($"Path: {destFile.FullName}");
					writer.WriteLine($"Exists: {destFile.Exists}");
					writer.WriteLine($"Length: {destFile.Length}");
					writer.WriteLine($"Source Length: {sourceFile.Length}");
					writer.WriteLine($"Dest Last Write: {destFile.LastWriteTime}");
					writer.WriteLine($"Source Last Write: {sourceFile.LastWriteTime}");
					writer.WriteLine($"Time Diff (seconds): {Math.Abs((sourceFile.LastWriteTime - destFile.LastWriteTime).TotalSeconds)}");

					try
					{
						writer.WriteLine($"Attributes: {destFile.Attributes}");
						writer.WriteLine($"Is ReparsePoint: {destFile.Attributes.HasFlag(FileAttributes.ReparsePoint)}");
					}
					catch (Exception ex)
					{
						writer.WriteLine($"Error accessing attributes: {ex.Message}");
					}

					// Let's also check if the file has the same content
					try
					{
						byte[] sourceBytes = new byte[Math.Min(100, sourceFile.Length)];
						byte[] destBytes = new byte[Math.Min(100, destFile.Length)];

						using (var sourceStream = sourceFile.OpenRead())
						{
							sourceStream.Read(sourceBytes, 0, sourceBytes.Length);
						}

						using (var destStream = destFile.OpenRead())
						{
							destStream.Read(destBytes, 0, destBytes.Length);
						}

						bool contentSame = sourceBytes.SequenceEqual(destBytes);
						writer.WriteLine($"First 100 bytes match: {contentSame}");
					}
					catch (Exception ex)
					{
						writer.WriteLine($"Error comparing content: {ex.Message}");
					}

					writer.WriteLine("=====================================");
				}
			}

			try
			{
				// Special case for zero-size source files that might be symlinks/junctions
				if (sourceFile.Length == 0 && destFile.Length > 0)
				{
					// If the source is zero bytes but destination is not,
					// we'll assume it's a special file and skip updating it
					result.IsChanged = false;
					return result;
				}

				// If it's a symlink, we'll generally assume it's not changed
				if (IsSymbolicLink(destFile.FullName))
				{
					// For symlinks, we'll ignore size differences and just keep them as-is
					//result.IsChanged = false;

					// You can uncomment this if you specifically want to check timestamp differences, but I found it doesn't work for some reason.
					// But generally, it's better to leave symlinks alone
					result.IsChanged = Math.Abs((sourceFile.LastWriteTime - destFile.LastWriteTime).TotalSeconds) > 60;
				}
				else
				{
					// Normal file comparison
					// We check both file size and last modified time
					result.IsChanged = sourceFile.Length != destFile.Length ||
								   Math.Abs((sourceFile.LastWriteTime - destFile.LastWriteTime).TotalSeconds) > 1;
				}
			}
			catch (Exception)
			{
				// If there's any error checking symlink status, assume not changed to be safe
				// This prevents unnecessary updating of special files (symlinks, junctions, etc.)
				result.IsChanged = false;
			}

			return result;
		}

		// Helper method to check if a file is a symbolic link
		private static bool IsSymbolicLink(string path)
		{
			try
			{
				FileInfo pathInfo = new FileInfo(path);

				// Check if it has the ReparsePoint attribute (which includes symlinks, junctions, etc.)
				bool hasReparsePoint = pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);

				// Many symlinks also have zero size
				bool hasZeroSize = pathInfo.Length == 0;

				// Check if it's a known symlink path (the specific one you're having trouble with)
				bool isKnownSymlinkPath = path.Contains("your_problematic_symlink_name");

				return hasReparsePoint || (hasZeroSize && isKnownSymlinkPath);
			}
			catch
			{
				// If we can't access the file for some reason, err on the side of caution
				return true;  // Assume it's a special file like a symlink
			}
		}

		// Show the update dialog
		public static async Task ShowUpdateDialogAsync()
		{
			var sourceDir = GarrysModInstallSystem.GetVanillaInstallFolder();
			var destDir = GarrysModInstallSystem.GetThisInstallFolder();

			var updateFiles = DetectUpdates(sourceDir, destDir);

			if (updateFiles.Count == 0)
			{
				MessageBox.Show(
					"No updates found. Your RTX installation is up to date.",
					"No Updates",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return;
			}

			using (var updateForm = new UpdateForm(updateFiles, sourceDir, destDir))
			{
				if (updateForm.ShowDialog() == DialogResult.OK)
				{
					// Get selected files to update
					var selectedUpdates = updateForm.GetSelectedUpdates();

					// If no files are selected, show message and return
					if (selectedUpdates.Count == 0)
					{
						MessageBox.Show("No files were selected for update.",
							"Update Canceled",
							MessageBoxButtons.OK,
							MessageBoxIcon.Information);
						return;
					}

					// Show progress form for the update
					var progressForm = new ProgressForm();
					progressForm.Show();

					await Task.Run(() =>
					{
						try
						{
							PerformUpdate(selectedUpdates, progressForm);
							progressForm.UpdateProgress("Update completed successfully!", 100);
						}
						catch (Exception ex)
						{
							progressForm.UpdateProgress($"Error during update: {ex.Message}", 100);
							MessageBox.Show($"An error occurred during update:\n\n{ex.Message}",
								"Update Error",
								MessageBoxButtons.OK,
								MessageBoxIcon.Error);
						}
					});
				}
			}
		}

		// Perform the actual update
		private static void PerformUpdate(List<FileUpdateInfo> updates, ProgressForm progressForm)
		{
			int total = updates.Count;
			int current = 0;

			foreach (var update in updates)
			{
				current++;
				int progress = (current * 100) / total;

				try
				{
					if (update.IsDirectory)
					{
						progressForm.UpdateProgress($"Creating directory: {update.RelativePath}", progress);
						Directory.CreateDirectory(update.DestinationPath);
					}
					else
					{
						progressForm.UpdateProgress($"Updating file: {update.RelativePath}", progress);

						// Ensure the directory exists
						Directory.CreateDirectory(Path.GetDirectoryName(update.DestinationPath));

						// Copy the file with overwrite
						File.Copy(update.SourcePath, update.DestinationPath, true);
					}
				}
				catch (Exception ex)
				{
					progressForm.UpdateProgress($"Error updating {update.RelativePath}: {ex.Message}", progress);
					// Continue with next file instead of aborting the whole process
				}
			}
		}
	}

	// Form to display and select updates
	public class UpdateFormOld : Form
	{
		private TreeView updateTreeView;
		private Button updateButton;
		private Button cancelButton;
		private CheckBox selectAllCheckBox;
		private Label infoLabel;
		private List<GarrysModUpdateSystem.FileUpdateInfo> updateFiles;
		private string sourcePath;
		private string destPath;

		public UpdateFormOld(List<GarrysModUpdateSystem.FileUpdateInfo> updateFiles, string sourcePath, string destPath)
		{
			this.updateFiles = updateFiles;
			this.sourcePath = sourcePath;
			this.destPath = destPath;

			InitializeComponents();
			PopulateTreeView();
		}

		private void InitializeComponents()
		{
			// Form settings
			this.Text = "Update RTX Installation";
			this.Size = new Size(700, 600);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.StartPosition = FormStartPosition.CenterScreen;

			// Info label at top
			infoLabel = new Label
			{
				Text = $"The following files need to be updated. Select the files you want to update from the vanilla installation.",
				Location = new Point(10, 10),
				Size = new Size(660, 40),
				AutoSize = false
			};
			this.Controls.Add(infoLabel);

			// Select All checkbox
			selectAllCheckBox = new CheckBox
			{
				Text = "Select All",
				Location = new Point(10, 50),
				Size = new Size(100, 20),
				Checked = true
			};
			selectAllCheckBox.CheckedChanged += SelectAllCheckBox_CheckedChanged;
			this.Controls.Add(selectAllCheckBox);

			// TreeView for updates
			updateTreeView = new TreeView
			{
				Location = new Point(10, 80),
				Size = new Size(660, 420),
				CheckBoxes = true,
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
			};
			updateTreeView.AfterCheck += UpdateTreeView_AfterCheck;
			this.Controls.Add(updateTreeView);

			// Cancel button
			cancelButton = new Button
			{
				Text = "Cancel",
				DialogResult = DialogResult.Cancel,
				Location = new Point(500, 520),
				Size = new Size(80, 30),
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right
			};
			this.Controls.Add(cancelButton);

			// Update button
			updateButton = new Button
			{
				Text = "Update",
				DialogResult = DialogResult.OK,
				Location = new Point(590, 520),
				Size = new Size(80, 30),
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right
			};
			this.Controls.Add(updateButton);

			this.AcceptButton = updateButton;
			this.CancelButton = cancelButton;
		}

		// Get list of selected files to update
		public List<GarrysModUpdateSystem.FileUpdateInfo> GetSelectedUpdates()
		{
			var selectedUpdates = new List<GarrysModUpdateSystem.FileUpdateInfo>();

			foreach (TreeNode rootNode in updateTreeView.Nodes)
			{
				ProcessSelectedNodes(rootNode, selectedUpdates);
			}

			return selectedUpdates;
		}

		// Recursively process checked nodes
		private void ProcessSelectedNodes(TreeNode node, List<GarrysModUpdateSystem.FileUpdateInfo> selectedList)
		{
			if (node.Checked && node.Tag is GarrysModUpdateSystem.FileUpdateInfo fileInfo)
			{
				selectedList.Add(fileInfo);
			}

			foreach (TreeNode childNode in node.Nodes)
			{
				ProcessSelectedNodes(childNode, selectedList);
			}
		}

		// Populate tree with update files sorted by directory hierarchy
		private void PopulateTreeView()
		{
			updateTreeView.BeginUpdate();
			updateTreeView.Nodes.Clear();

			// Group by directory
			var filesByDirectory = updateFiles.GroupBy(f => Path.GetDirectoryName(f.RelativePath));

			// Create nodes for each directory
			Dictionary<string, TreeNode> directoryNodes = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);

			// First, create directory structure
			foreach (var group in filesByDirectory)
			{
				string dirPath = group.Key ?? "";
				string[] parts = dirPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
					StringSplitOptions.RemoveEmptyEntries);

				string currentPath = "";
				TreeNode parentNode = null;

				// Create or find nodes for each part of the path
				foreach (var part in parts)
				{
					currentPath = string.IsNullOrEmpty(currentPath) ? part : Path.Combine(currentPath, part);

					if (!directoryNodes.TryGetValue(currentPath, out TreeNode node))
					{
						// Create a new node for this directory
						node = new TreeNode(part)
						{
							Checked = true,
							ImageIndex = 0  // Directory icon
						};

						if (parentNode == null)
							updateTreeView.Nodes.Add(node);
						else
							parentNode.Nodes.Add(node);

						directoryNodes[currentPath] = node;
					}

					parentNode = node;
				}

				// Add file nodes under the appropriate directory node
				foreach (var file in group)
				{
					// Skip directories themselves - we only want to list files
					if (file.IsDirectory) continue;

					string fileName = Path.GetFileName(file.RelativePath);
					TreeNode fileNode = new TreeNode(fileName + $" ({file.Status})")
					{
						Checked = true,
						Tag = file,
						ImageIndex = 1  // File icon
					};

					if (parentNode == null)
						updateTreeView.Nodes.Add(fileNode);
					else
						parentNode.Nodes.Add(fileNode);
				}
			}

			// Add root level files that don't have a directory
			foreach (var file in updateFiles.Where(f => string.IsNullOrEmpty(Path.GetDirectoryName(f.RelativePath))))
			{
				if (file.IsDirectory) continue;

				TreeNode fileNode = new TreeNode(Path.GetFileName(file.RelativePath) + $" ({file.Status})")
				{
					Checked = true,
					Tag = file
				};
				updateTreeView.Nodes.Add(fileNode);
			}

			// Don't expand all nodes by default - leave them collapsed
			updateTreeView.EndUpdate();
		}

		// Handle checking/unchecking all nodes when Select All changes
		private void SelectAllCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			updateTreeView.BeginUpdate();

			foreach (TreeNode node in updateTreeView.Nodes)
			{
				CheckAllChildNodes(node, selectAllCheckBox.Checked);
			}

			updateTreeView.EndUpdate();
		}

		// Handle checking/unchecking child nodes when a parent is changed
		private void UpdateTreeView_AfterCheck(object sender, TreeViewEventArgs e)
		{
			// Don't respond to events triggered by the code itself
			if (e.Action != TreeViewAction.Unknown)
			{
				updateTreeView.BeginUpdate();
				CheckAllChildNodes(e.Node, e.Node.Checked);
				updateTreeView.EndUpdate();
			}
		}

		// Recursively check or uncheck all child nodes
		private void CheckAllChildNodes(TreeNode node, bool isChecked)
		{
			node.Checked = isChecked;

			foreach (TreeNode childNode in node.Nodes)
			{
				CheckAllChildNodes(childNode, isChecked);
			}
		}
	}
}
