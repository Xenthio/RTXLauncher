using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RTXLauncher.Avalonia.Utilities
{
    public class MarkdownFormatter
    {
        // Dictionary of prefixes and their color styles (following WinForms color scheme)
        private static readonly Dictionary<string, string> PrefixColors = new()
        {
            { "Fixed:", "#2F4F4F" },      // DarkSlateGray
            { "Fixes:", "#2F4F4F" },
            { "Bug fix:", "#2F4F4F" },
            { "Added:", "#006400" },      // DarkGreen
            { "New:", "#006400" },
            { "What's New:", "#006400" },
            { "Feature:", "#006400" },
            { "Removed:", "#8B0000" },    // DarkRed
            { "Changed:", "#FF8C00" },    // DarkOrange
            { "What's Changed:", "#FF8C00" },
            { "Updated:", "#FF8C00" },
            { "Improved:", "#FF8C00" },
            { "Technical Details:", "#8B008B" }, // DarkMagenta
            { "Known Issues:", "#FF0000" },      // Red
            { "Known Issue:", "#FF0000" },
            { "Warning:", "#FF0000" },
            { "Important:", "#FF0000" },
            { "Note:", "#FF0000" },
            { "New Contributors:", "#FF00FF" },  // Fuchsia
        };

        public static string FormatReleaseNotes(string newVersion, string currentVersion, string releaseNotes, bool isUpdate = true)
        {
            var markdown = new StringBuilder();

            // Format the header section
            FormatHeaderSection(markdown, newVersion, currentVersion, isUpdate);

            // Handle missing release notes
            if (string.IsNullOrWhiteSpace(releaseNotes))
            {
                markdown.AppendLine("*No release notes available for this version.*");
                return markdown.ToString();
            }

            // Process each line
            string[] lines = releaseNotes.Replace("\r\n", "\n").Split('\n');
            bool inCodeBlock = false;

            foreach (string line in lines)
            {
                string trimmedLine = line.TrimStart();

                // Skip empty lines (preserve spacing)
                if (string.IsNullOrWhiteSpace(line))
                {
                    markdown.AppendLine();
                    continue;
                }

                // Handle code blocks
                if (trimmedLine.StartsWith("```"))
                {
                    inCodeBlock = !inCodeBlock;
                    markdown.AppendLine(line);
                    continue;
                }

                if (inCodeBlock)
                {
                    markdown.AppendLine(line);
                    continue;
                }

                // Parse line components
                (string content, int headerLevel, bool isBullet, string bulletPrefix) = ParseLineFormat(trimmedLine);

                // Find special prefix if any
                string matchedPrefix = PrefixColors.Keys.FirstOrDefault(prefix => content.StartsWith(prefix)) ?? string.Empty;

                // Format the line based on its components
                if (headerLevel > 0)
                {
                    // Headers - add the appropriate markdown header syntax
                    string headerPrefix = new string('#', headerLevel);
                    if (!string.IsNullOrEmpty(matchedPrefix))
                    {
                        markdown.AppendLine($"{headerPrefix} <span style=\"color:{PrefixColors[matchedPrefix]}\">{matchedPrefix}</span>{content.Substring(matchedPrefix.Length)}");
                    }
                    else
                    {
                        markdown.AppendLine($"{headerPrefix} {content}");
                    }
                }
                else if (isBullet)
                {
                    // Bullet points
                    if (!string.IsNullOrEmpty(matchedPrefix))
                    {
                        markdown.AppendLine($"- <span style=\"color:{PrefixColors[matchedPrefix]}\">{matchedPrefix}</span>{ProcessInlineMarkdown(content.Substring(matchedPrefix.Length))}");
                    }
                    else
                    {
                        markdown.AppendLine($"- {ProcessInlineMarkdown(content)}");
                    }
                }
                else if (!string.IsNullOrEmpty(matchedPrefix))
                {
                    // Prefixed lines
                    markdown.AppendLine($"<span style=\"color:{PrefixColors[matchedPrefix]}\">{matchedPrefix}</span>{ProcessInlineMarkdown(content.Substring(matchedPrefix.Length))}");
                }
                else
                {
                    // Regular text
                    markdown.AppendLine(ProcessInlineMarkdown(line));
                }
            }

            return markdown.ToString();
        }

        private static void FormatHeaderSection(StringBuilder markdown, string newVersion, string currentVersion, bool isUpdate)
        {
            if (isUpdate)
            {
                markdown.AppendLine("## <span style=\"color:#008000\">Update Available!</span>");
                markdown.AppendLine();
                markdown.AppendLine($"**New version:** {newVersion}");
                markdown.AppendLine($"Current version: {currentVersion}");
                markdown.AppendLine();
            }
            else
            {
                markdown.AppendLine($"## <span style=\"color:#00008B\">You're up to date! ({currentVersion})</span>");
                markdown.AppendLine();
            }
        }

        private static (string content, int headerLevel, bool isBullet, string bulletPrefix) ParseLineFormat(string line)
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

        private static string ProcessInlineMarkdown(string text)
        {
            // Handle the special "Full Changelog" link pattern
            if (text.Contains("**Full Changelog**:") && text.Contains("compare/"))
            {
                // Extract the version comparison (e.g., v1.0.4...v1.0.5)
                Regex compareRegex = new Regex(@"compare/([^/\s\)]+)");
                Match compareMatch = compareRegex.Match(text);

                // Extract the URL
                Regex urlRegex = new Regex(@"(https?://[^\s\)]+)");
                Match urlMatch = urlRegex.Match(text);

                if (urlMatch.Success)
                {
                    string url = urlMatch.Groups[1].Value;
                    string versionCompare = compareMatch.Success ? compareMatch.Groups[1].Value : "versions";
                    return $"**Full Changelog**: [{versionCompare}]({url})";
                }
            }

            // The markdown library should handle bold (**text**), italic (*text*), and links [text](url) automatically
            return text;
        }
    }
}