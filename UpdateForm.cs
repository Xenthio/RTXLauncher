using System.Data;

namespace RTXLauncher
{
	public partial class UpdateForm : Form
	{
		private List<GarrysModUpdateSystem.FileUpdateInfo> updateFiles;
		private string sourcePath;
		private string destPath;
		public UpdateForm(List<GarrysModUpdateSystem.FileUpdateInfo> updateFiles, string sourcePath, string destPath)
		{
			this.updateFiles = updateFiles;
			this.sourcePath = sourcePath;
			this.destPath = destPath;

			InitializeComponent();
			PopulateTreeView();
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

			// Get all unique directories (excluding null/empty which is the root)
			var directoriesWithFiles = updateFiles
				.Where(f => !f.IsDirectory)
				.Select(f => Path.GetDirectoryName(f.RelativePath))
				.Where(dir => !string.IsNullOrEmpty(dir))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			// Create a dictionary to hold our directory nodes
			var directoryNodes = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);

			// Create directory hierarchy first
			foreach (var dirPath in directoriesWithFiles)
			{
				string[] parts = dirPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
					StringSplitOptions.RemoveEmptyEntries);

				string currentPath = "";
				TreeNode parentNode = null;

				// Create nodes for each directory level
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
			}

			// Now add files to their respective directory nodes
			foreach (var file in updateFiles.Where(f => !f.IsDirectory))
			{
				string dirPath = Path.GetDirectoryName(file.RelativePath);
				string fileName = Path.GetFileName(file.RelativePath);

				TreeNode fileNode = new TreeNode(fileName + $" ({file.Status})")
				{
					Checked = true,
					Tag = file,
					ImageIndex = 1  // File icon
				};

				if (string.IsNullOrEmpty(dirPath))
				{
					// Root level file
					updateTreeView.Nodes.Add(fileNode);
				}
				else if (directoryNodes.TryGetValue(dirPath, out TreeNode dirNode))
				{
					// File in a directory
					dirNode.Nodes.Add(fileNode);
				}
			}

			// Don't expand all nodes by default
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
