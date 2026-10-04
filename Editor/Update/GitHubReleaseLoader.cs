using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.Editor.Update
{
    internal static class GitHubReleaseLoader
    {
        private const string ApiUrl = "https://api.github.com/repos/{0}/{1}/releases/tags/{2}";

        public static void Load(string owner, string repository, string version, VisualElement container)
        {
            string url = string.Format(ApiUrl, owner, repository, version);

            var request = UnityWebRequest.Get(url);

            request.SetRequestHeader("Accept", "application/vnd.github+json");

            request.SetRequestHeader("User-Agent", "Unity-Editor-Package-Updater");

            var operation = request.SendWebRequest();

            EditorApplication.update += WaitForRequest;

            void WaitForRequest()
            {
                if (!operation.isDone)
                {
                    return;
                }

                EditorApplication.update -= WaitForRequest;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to load GitHub release: {request.error}");

                    container.Add(new Label($"Failed to load changelog: {request.error}"));

                    request.Dispose();
                    return;
                }

                try
                {
                    var release = JsonUtility.FromJson<GitHubRelease>(request.downloadHandler.text);

                    BuildChangelog(container, release.body);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);

                    container.Add(new Label("Failed to parse changelog."));
                }
                finally
                {
                    request.Dispose();
                }
            }
        }

        private static void BuildChangelog(VisualElement container, string markdown)
        {
            container.Clear();

            if (string.IsNullOrWhiteSpace(markdown))
                return;

            var lines = markdown
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n');

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                
                if (line.StartsWith("## "))
                {
                    AddSectionHeader(container, line.Substring(3));
                    continue;
                }
                
                if (line.StartsWith("- "))
                {
                    AddBullet(container, line.Substring(2));
                    continue;
                }
                
                if (line.StartsWith("**Full Changelog**"))
                {
                    continue;
                }

                AddParagraph(container, line);
            }
        }

        private static void AddSectionHeader(VisualElement container, string text)
        {
            string cleanMarkdown = CleanMarkdown(text);
            var label = new Label(cleanMarkdown)
            {
                style =
                {
                    fontSize = 15,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 12,
                    marginBottom = 6
                }
            };

            container.Add(label);
        }

        private static void AddBullet(VisualElement container, string text)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 6
                }
            };

            var bullet = new Label("•")
            {
                style =
                {
                    width = 16,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            string cleanMarkdown = CleanMarkdown(text);
            var label = new Label(cleanMarkdown)
            {
                style =
                {
                    flexGrow = 1,
                    whiteSpace = WhiteSpace.Normal
                }
            };

            row.Add(bullet);
            row.Add(label);

            container.Add(row);
        }

        private static void AddParagraph(
            VisualElement container,
            string text)
        {
            var label = new Label(
                CleanMarkdown(text));

            label.style.whiteSpace =
                WhiteSpace.Normal;

            label.style.marginBottom = 6;

            container.Add(label);
        }

        private static string CleanMarkdown(string text)
        {
            text = Regex.Replace(text, @"\[([^\]]+)\]\([^)]+\)", "$1");
            text = Regex.Replace(text, @"`([^`]+)`", "$1");
            text = text.Replace("**", "");
            text = Regex.Replace(text, @"\*([^*]+)\*", "$1");
            text = text.Replace("__", "");

            return text.Trim();
        }

        [Serializable]
        private sealed class GitHubRelease
        {
            public string tag_name;
            public string name;
            public string body;
        }
    }
}