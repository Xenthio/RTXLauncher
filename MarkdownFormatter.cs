using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Windows.Forms;

namespace RTXLauncher
{
    public class MarkdownFormatter
    {
        private List<LinkInfo> _links = new List<LinkInfo>();

        public void FormatReleaseNotes(RichTextBox richTextBox, string newVersion, string currentVersion, string releaseNotes, bool isUpdate = true)
        {
            // Initialize and clear
            _links.Clear();
            richTextBox.Clear();
            richTextBox.SuspendLayout();

            // Dictionary of prefixes and their formatting
            var prefixFormats = new Dictionary<string, Color>
            {
                { "Fixed:", Color.Green },
                { "Fixes:", Color.Green },
                { "Bug fix:", Color.Green },
                { "Added:", Color.DarkBlue },
                { "New:", Color.DarkBlue },
                { "Feature:", Color.DarkBlue },
                { "Changed:", Color.DarkOrange },
                { "Updated:", Color.DarkOrange },
                { "Improved:", Color.DarkOrange },
                { "Warning:", Color.Red },
                { "Important:", Color.Red },
                { "Note:", Color.Red }
            };

            // Format the header section
            FormatHeaderSection(richTextBox, newVersion, currentVersion, isUpdate);

            // Handle missing release notes
            if (string.IsNullOrWhiteSpace(releaseNotes))
            {
                richTextBox.SelectionFont = new Font("Segoe UI", 9f, FontStyle.Italic);
                richTextBox.SelectionColor = Color.Gray;
                richTextBox.AppendText("No release notes available for this version.");
                richTextBox.ResumeLayout();
                return;
            }

            // Process each line
            string[] lines = releaseNotes.Replace("\r\n", "\n").Split('\n');
            bool inCodeBlock = false;
            bool inBulletList = false;

            foreach (string line in lines)
            {
                string trimmedLine = line.TrimStart();

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                {
                    inBulletList = false;
                    richTextBox.SelectionBullet = false;
                    richTextBox.AppendText("\n");
                    continue;
                }

                char mdcodechar = '`';
                // Handle code blocks
                if (trimmedLine.StartsWith($"{mdcodechar}{mdcodechar}{mdcodechar}"))
                {
                    FormatCodeBlockMarker(richTextBox, ref inCodeBlock);
                    continue;
                }

                if (inCodeBlock)
                {
                    FormatCodeLine(richTextBox, line);
                    continue;
                }

                // Parse line components
                (string content, int headerLevel, bool isBullet, string bulletPrefix) = ParseLineFormat(trimmedLine);

                // Find special prefix if any
                string matchedPrefix = prefixFormats.Keys.FirstOrDefault(prefix => content.StartsWith(prefix));

                // Format the line based on its components
                if (isBullet)
                {
                    FormatBulletLine(richTextBox, content, matchedPrefix, prefixFormats, ref inBulletList);
                }
                else if (headerLevel > 0)
                {
                    FormatHeaderLine(richTextBox, content, headerLevel, matchedPrefix, prefixFormats);
                }
                else if (matchedPrefix != null)
                {
                    FormatPrefixedLine(richTextBox, content, matchedPrefix, prefixFormats);
                }
                else
                {
                    // Regular text
                    if (inBulletList)
                    {
                        inBulletList = false;
                        richTextBox.SelectionBullet = false;
                    }

                    richTextBox.SelectionFont = new Font("Segoe UI", 9f);
                    richTextBox.SelectionColor = Color.Black;
                    ProcessMarkdownText(richTextBox, line);
                    richTextBox.AppendText("\n");
                }
            }

            // Final cleanup and setup
            richTextBox.MouseClick -= ReleaseNotesRichTextBox_MouseClick;
            richTextBox.MouseClick += ReleaseNotesRichTextBox_MouseClick;

            // Reset styling
            ResetTextBoxFormatting(richTextBox);

            richTextBox.ResumeLayout();
        }

        private void FormatHeaderSection(RichTextBox richTextBox, string newVersion, string currentVersion, bool isUpdate)
        {
            if (isUpdate)
            {
                richTextBox.SelectionFont = new Font("Segoe UI", 12f, FontStyle.Bold);
                richTextBox.SelectionColor = Color.Green;
                richTextBox.AppendText("Update Available!\n");

                richTextBox.SelectionFont = new Font("Segoe UI", 10f, FontStyle.Regular);
                richTextBox.SelectionColor = Color.Black;
                richTextBox.AppendText($"New version: ");

                richTextBox.SelectionFont = new Font("Segoe UI", 10f, FontStyle.Bold);
                richTextBox.AppendText($"{newVersion}\n");

                richTextBox.SelectionFont = new Font("Segoe UI", 10f, FontStyle.Regular);
                richTextBox.AppendText($"Current version: {currentVersion}\n\n");
            }
            else
            {
                richTextBox.SelectionFont = new Font("Segoe UI", 12f, FontStyle.Bold);
                richTextBox.SelectionColor = Color.DarkBlue;
                richTextBox.AppendText($"You're up to date! ({currentVersion})\n\n");
            }
        }

        private void FormatCodeBlockMarker(RichTextBox richTextBox, ref bool inCodeBlock)
        {
            inCodeBlock = !inCodeBlock;

            if (inCodeBlock)
            {
                richTextBox.SelectionBackColor = Color.FromArgb(245, 245, 245);
                richTextBox.SelectionFont = new Font("Consolas", 9f);
            }
            else
            {
                richTextBox.SelectionBackColor = Color.White;
                richTextBox.SelectionFont = new Font("Segoe UI", 9f);
                richTextBox.AppendText("\n");
            }
        }

        private void FormatCodeLine(RichTextBox richTextBox, string line)
        {
            richTextBox.SelectionFont = new Font("Consolas", 9f);
            richTextBox.SelectionColor = Color.DarkBlue;
            richTextBox.AppendText(line + "\n");
        }

        private (string content, int headerLevel, bool isBullet, string bulletPrefix) ParseLineFormat(string line)
        {
            int headerLevel = 0;
            bool isBullet = false;
            string bulletPrefix = "";
            string content = line;

            // Check for headers
            if (line.StartsWith("### "))
            {
                headerLevel = 3;
                content = line.Substring(4);
            }
            else if (line.StartsWith("## "))
            {
                headerLevel = 2;
                content = line.Substring(3);
            }
            else if (line.StartsWith("# "))
            {
                headerLevel = 1;
                content = line.Substring(2);
            }

            // Check for bullets
            if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                isBullet = true;
                bulletPrefix = line.StartsWith("- ") ? "- " : "* ";
                content = line.Substring(2);
            }

            return (content, headerLevel, isBullet, bulletPrefix);
        }

        private void FormatBulletLine(RichTextBox richTextBox, string content, string matchedPrefix, Dictionary<string, Color> prefixFormats, ref bool inBulletList)
        {
            inBulletList = true;
            richTextBox.SelectionBullet = true;
            richTextBox.BulletIndent = 15;

            if (matchedPrefix != null)
            {
                // Format the special prefix part
                richTextBox.SelectionFont = new Font("Segoe UI", 9f);
                richTextBox.SelectionColor = prefixFormats[matchedPrefix];
                richTextBox.AppendText(matchedPrefix);

                // Format the rest of the bullet text
                richTextBox.SelectionFont = new Font("Segoe UI", 9f);
                richTextBox.SelectionColor = Color.Black;
                ProcessMarkdownText(richTextBox, content.Substring(matchedPrefix.Length));
            }
            else
            {
                // Process normal bullet
                richTextBox.SelectionFont = new Font("Segoe UI", 9f);
                richTextBox.SelectionColor = Color.Black;
                ProcessMarkdownText(richTextBox, content);
            }

            richTextBox.AppendText("\n");
            richTextBox.SelectionBullet = false;
        }

        private void FormatHeaderLine(RichTextBox richTextBox, string content, int level, string matchedPrefix, Dictionary<string, Color> prefixFormats)
        {
            // Define header font based on level
            Font headerFont;
            switch (level)
            {
                case 1: headerFont = new Font("Segoe UI", 12f, FontStyle.Bold); break;
                case 2: headerFont = new Font("Segoe UI", 11f, FontStyle.Bold); break;
                default: headerFont = new Font("Segoe UI", 10f, FontStyle.Bold); break;
            }

            if (matchedPrefix != null)
            {
                // Add the prefix with special color
                richTextBox.SelectionFont = headerFont;
                richTextBox.SelectionColor = prefixFormats[matchedPrefix];
                richTextBox.AppendText(matchedPrefix);

                // Add the rest with header style
                richTextBox.SelectionFont = headerFont;
                richTextBox.SelectionColor = Color.DarkBlue;
                richTextBox.AppendText(content.Substring(matchedPrefix.Length));
            }
            else
            {
                // Format the whole header
                richTextBox.SelectionFont = headerFont;
                richTextBox.SelectionColor = Color.DarkBlue;
                richTextBox.AppendText(content);
            }

            richTextBox.AppendText("\n");
        }

        private void FormatPrefixedLine(RichTextBox richTextBox, string content, string prefix, Dictionary<string, Color> prefixFormats)
        {
            // Format the prefix part
            richTextBox.SelectionFont = new Font("Segoe UI", 9f,
                prefix.StartsWith("Warning:") || prefix.StartsWith("Important:") || prefix.StartsWith("Note:")
                    ? FontStyle.Bold : FontStyle.Regular);
            richTextBox.SelectionColor = prefixFormats[prefix];
            richTextBox.AppendText(prefix);

            // Format the rest of the line
            richTextBox.SelectionFont = new Font("Segoe UI", 9f);
            richTextBox.SelectionColor = Color.Black;
            ProcessMarkdownText(richTextBox, content.Substring(prefix.Length));

            richTextBox.AppendText("\n");
        }

        private void ResetTextBoxFormatting(RichTextBox richTextBox)
        {
            richTextBox.SelectionFont = new Font("Segoe UI", 9f);
            richTextBox.SelectionColor = Color.Black;
            richTextBox.SelectionBackColor = Color.White;
            richTextBox.SelectionBullet = false;
            richTextBox.SelectionStart = 0;
            richTextBox.ScrollToCaret();
        }

        private void ProcessMarkdownText(RichTextBox richTextBox, string line)
        {
            // Check for full changelog line
            if (line.Contains("**Full Changelog**:") && line.Contains("compare/"))
            {
                // Extract the version comparison (e.g., v1.0.4...v1.0.5)
                Regex compareRegex = new Regex(@"compare/([^/\s\)]+)");
                Match compareMatch = compareRegex.Match(line);

                // Extract the URL
                Regex urlRegex = new Regex(@"(https?://[^\s\)]+)");
                Match urlMatch = urlRegex.Match(line);

                if (urlMatch.Success)
                {
                    string url = urlMatch.Groups[1].Value;
                    string versionCompare = compareMatch.Success ? compareMatch.Groups[1].Value : "versions";

                    // Add "Full Changelog" in bold
                    richTextBox.SelectionFont = new Font(richTextBox.Font, FontStyle.Bold);
                    richTextBox.AppendText("Full Changelog");

                    // Add the colon
                    richTextBox.SelectionFont = new Font(richTextBox.Font, FontStyle.Regular);
                    richTextBox.AppendText(": ");

                    // Add the version comparison as a link
                    int linkStart = richTextBox.TextLength;
                    richTextBox.AppendText(versionCompare);
                    int linkEnd = richTextBox.TextLength;

                    // Format the link text
                    richTextBox.Select(linkStart, linkEnd - linkStart);
                    richTextBox.SelectionFont = new Font(richTextBox.Font, FontStyle.Underline);
                    richTextBox.SelectionColor = Color.Blue;

                    // Store link info
                    _links.Add(new LinkInfo
                    {
                        StartIndex = linkStart,
                        EndIndex = linkEnd,
                        Url = url
                    });

                    return;
                }
            }

            // Handle bold text (**bold**) and links ([text](url))
            // We'll need to parse the line character by character to handle nested formatting

            // First, find all the special markdown segments
            List<MarkdownSegment> segments = new List<MarkdownSegment>();

            // Find bold segments
            FindMarkdownPatterns(line, @"\*\*(.*?)\*\*", segments, MarkdownType.Bold);

            // Find italic segments
            FindMarkdownPatterns(line, @"\*(.*?)\*", segments, MarkdownType.Italic);

            // Find link segments
            FindMarkdownPatterns(line, @"\[(.*?)\]\((https?://[^\s\)]+)\)", segments, MarkdownType.Link);

            // Sort segments by their starting position
            segments = segments.OrderBy(s => s.StartIndex).ToList();

            // Detect overlapping segments and remove them
            for (int i = segments.Count - 1; i >= 1; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    if (segments[i].StartIndex < segments[j].EndIndex)
                    {
                        // Segments overlap, remove the later one
                        segments.RemoveAt(i);
                        break;
                    }
                }
            }

            // Process the line with the segments
            int lastIndex = 0;
            foreach (var segment in segments)
            {
                // Add text before the segment
                if (segment.StartIndex > lastIndex)
                {
                    string beforeText = line.Substring(lastIndex, segment.StartIndex - lastIndex);
                    richTextBox.SelectionFont = new Font(richTextBox.Font, FontStyle.Regular);
                    richTextBox.SelectionColor = Color.Black;
                    richTextBox.AppendText(beforeText);
                }

                // Add the segment with appropriate formatting
                switch (segment.Type)
                {
                    case MarkdownType.Bold:
                        richTextBox.SelectionFont = new Font(richTextBox.Font, FontStyle.Bold);
                        richTextBox.SelectionColor = Color.Black;
                        richTextBox.AppendText(segment.Text);
                        break;

                    case MarkdownType.Italic:
                        richTextBox.SelectionFont = new Font(richTextBox.Font, FontStyle.Italic);
                        richTextBox.SelectionColor = Color.Black;
                        richTextBox.AppendText(segment.Text);
                        break;

                    case MarkdownType.Link:
                        int linkStart = richTextBox.TextLength;
                        richTextBox.SelectionFont = new Font(richTextBox.Font, FontStyle.Underline);
                        richTextBox.SelectionColor = Color.Blue;
                        richTextBox.AppendText(segment.Text);
                        int linkEnd = richTextBox.TextLength;

                        _links.Add(new LinkInfo
                        {
                            StartIndex = linkStart,
                            EndIndex = linkEnd,
                            Url = segment.Url
                        });
                        break;
                }

                lastIndex = segment.EndIndex;
            }

            // Add any remaining text after the last segment
            if (lastIndex < line.Length)
            {
                richTextBox.SelectionFont = new Font(richTextBox.Font, FontStyle.Regular);
                richTextBox.SelectionColor = Color.Black;
                richTextBox.AppendText(line.Substring(lastIndex));
            }
        }

        private void FindMarkdownPatterns(string text, string pattern, List<MarkdownSegment> segments, MarkdownType type)
        {
            Regex regex = new Regex(pattern);
            foreach (Match match in regex.Matches(text))
            {
                if (type == MarkdownType.Link && match.Groups.Count >= 3)
                {
                    segments.Add(new MarkdownSegment
                    {
                        StartIndex = match.Index,
                        EndIndex = match.Index + match.Length,
                        Text = match.Groups[1].Value,
                        Url = match.Groups[2].Value,
                        Type = type
                    });
                }
                else if (match.Groups.Count >= 2)
                {
                    segments.Add(new MarkdownSegment
                    {
                        StartIndex = match.Index,
                        EndIndex = match.Index + match.Length,
                        Text = match.Groups[1].Value,
                        Type = type
                    });
                }
            }
        }

        private void ReleaseNotesRichTextBox_MouseClick(object sender, MouseEventArgs e)
        {
            RichTextBox richTextBox = sender as RichTextBox;
            if (richTextBox == null) return;

            // Get the character index at the clicked position
            int index = richTextBox.GetCharIndexFromPosition(e.Location);

            // Check if the click is on a link
            foreach (var link in _links)
            {
                if (index >= link.StartIndex && index < link.EndIndex)
                {
                    try
                    {
                        // Open the URL
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = link.Url,
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    break;
                }
            }
        }

        private class LinkInfo
        {
            public int StartIndex { get; set; }
            public int EndIndex { get; set; }
            public string Url { get; set; }
        }

        private enum MarkdownType
        {
            Bold,
            Italic,
            Link
        }

        private class MarkdownSegment
        {
            public int StartIndex { get; set; }
            public int EndIndex { get; set; }
            public string Text { get; set; }
            public string Url { get; set; }
            public MarkdownType Type { get; set; }
        }
    }
}
