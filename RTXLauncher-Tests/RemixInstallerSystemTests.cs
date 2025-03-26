/*using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RTXLauncher.Tests
{
    [TestClass]
    public class RemixInstallerSystemTests
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
        public async Task Install_ShouldInstallSuccessfully()
        {
            // Arrange
            var selectedRelease = new GitHubRelease
            {
                Name = "Test Release",
                Assets = new List<GitHubAsset>
                {
                    new GitHubAsset
                    {
                        Name = "test-gmod.zip",
                        BrowserDownloadUrl = "https://example.com/test-gmod.zip"
                    }
                }
            };
            string owner = "testOwner";
            string repo = "testRepo";
            string installType = "gmod_main";
            string basePath = _tempDir;
            bool progressCalled = false;

            void ProgressCallback(string message, int progress)
            {
                progressCalled = true;
            }

            // Act
            await RTXRemix.Install(selectedRelease, owner, repo, installType, basePath, ProgressCallback);

            // Assert
            Assert.IsTrue(progressCalled, "Progress callback was not called.");
            // Additional assertions can be added to verify the installation
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Install_ShouldThrowArgumentNullExceptionForNullRelease()
        {
            // Arrange
            GitHubRelease selectedRelease = null;
            string owner = "testOwner";
            string repo = "testRepo";
            string installType = "gmod_main";
            string basePath = _tempDir;

            // Act
            await RTXRemix.Install(selectedRelease, owner, repo, installType, basePath, null);

            // Assert is handled by ExpectedException
        }

        [TestMethod]
        public async Task Install_ShouldHandleSpecializedX64Installation()
        {
            // Arrange
            var selectedRelease = new GitHubRelease
            {
                Name = "Test Release",
                Assets = new List<GitHubAsset>()
            };
            string owner = "testOwner";
            string repo = "testRepo";
            string installType = "gmod_x86-64";
            string basePath = _tempDir;
            bool progressCalled = false;

            void ProgressCallback(string message, int progress)
            {
                progressCalled = true;
            }

            // Mock the GitHubAPI.FetchReleasesAsync method to return a test release
            GitHubAPI.FetchReleasesAsync = (o, r) => Task.FromResult(new List<GitHubRelease>
            {
                new GitHubRelease
                {
                    Name = "Specialized Release",
                    Assets = new List<GitHubAsset>
                    {
                        new GitHubAsset
                        {
                            Name = "specialized-gmod.zip",
                            BrowserDownloadUrl = "https://example.com/specialized-gmod.zip"
                        }
                    }
                }
            });

            // Act
            await RTXRemix.Install(selectedRelease, owner, repo, installType, basePath, ProgressCallback);

            // Assert
            Assert.IsTrue(progressCalled, "Progress callback was not called.");
            // Additional assertions can be added to verify the installation
        }

        // Add more tests to cover other scenarios
    }
}
*/