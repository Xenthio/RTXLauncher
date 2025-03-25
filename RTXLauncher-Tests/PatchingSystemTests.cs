namespace RTXLauncher.Tests
{
	[TestClass]
	public class PatchingSystemTests
	{
		private string _tempDir;

		[TestInitialize]
		public void Setup()
		{
			_tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			Directory.CreateDirectory(_tempDir);
		}

		[TestCleanup]
		public void Cleanup()
		{
			if (Directory.Exists(_tempDir))
			{
				Directory.Delete(_tempDir, true);
			}
		}

		[TestMethod]
		public async Task ApplyPatches_ShouldApplyPatchesCorrectly()
		{
			// tell GarrysModInstallSystem.GetInstallType to return gmod_main
			GarrysModInstallSystem.TestMode = true;
			GarrysModInstallSystem.TestModeReturn = "gmod_main";

			// Arrange
			string installPath = _tempDir;
			string patchString = @"
patches32 = {
'testing/test.txt': [
   [('68656c6c6f', 0), '776f726c64'], # Pattern: hello, replacement: world
]
}
patches64 = {
'testing/test.txt': [
   [('68656c6c6f', 0), '7468657265'], #Pattern: hello, replacement: there
]
}";
			bool progressCalled = false;

			void ProgressCallback(string message, int progress)
			{
				progressCalled = true;
			}

			// Create a dummy file to patch
			string filePath = Path.Combine(installPath, "testing/test.txt");
			Directory.CreateDirectory(Path.GetDirectoryName(filePath));
			File.WriteAllText(filePath, "hello");

			// Act
			await PatchingSystem.ApplyPatches(installPath, patchString, ProgressCallback);

			// Assert
			Assert.IsTrue(progressCalled, "Progress callback was not called.");
			string patchedContent = File.ReadAllText(filePath);
			Assert.AreEqual("world", patchedContent, "The file content was not patched correctly.");
		}

		[TestMethod]
		[ExpectedException(typeof(Exception))]
		public async Task ApplyPatches_ShouldThrowExceptionForUnknownInstallType()
		{
			// Arrange
			string installPath = _tempDir;
			string patchString = "patch data here"; // Replace with actual patch data

			// tell GarrysModInstallSystem.GetInstallType to not return gmod_main
			GarrysModInstallSystem.TestMode = true;
			GarrysModInstallSystem.TestModeReturn = "unknown_type";

			// Act
			await PatchingSystem.ApplyPatches(installPath, patchString);

			// Assert is handled by ExpectedException
		}

		[TestMethod]
		public async Task ApplyPatches_ShouldHandleMissingFiles()
		{
			// Arrange
			string installPath = _tempDir;
			string patchString = @"
            {
                ""bin/missing.dll"": [
                    [
                        [""68656c6c6f"", 0], // Pattern: hello
                        ""776f726c64"" // Patch: world
                    ]
                ]
            }";
			bool progressCalled = false;

			void ProgressCallback(string message, int progress)
			{
				progressCalled = true;
			}

			// Act
			try
			{
				await PatchingSystem.ApplyPatches(installPath, patchString, ProgressCallback);
			}
			catch (Exception ex)
			{
				// Assert
				Assert.IsTrue(ex.Message.Contains("Missing files"), "Exception message does not contain 'Missing files'.");
			}
		}

		// Add more tests to cover other scenarios
	}
}
