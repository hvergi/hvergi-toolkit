using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

namespace HvergiToolkit
{
    public static class UpdateManager
    {
        private static readonly System.Net.Http.HttpClient HttpClient = new System.Net.Http.HttpClient();

        public class ReleaseInfo
        {
            public string TagName { get; set; }
            public string Body { get; set; }
            public List<AssetInfo> Assets { get; set; }
        }

        public class AssetInfo
        {
            public string Name { get; set; }
            public string BrowserDownloadUrl { get; set; }
        }

        public static async Task<(bool available, string version, string downloadUrl)> CheckForUpdates()
        {
            try
            {
                string url = $"https://api.github.com/repos/{AppSettings.General.GitHubRepo}/releases/latest";
                HttpClient.DefaultRequestHeaders.Add("User-Agent", "HvergiToolkit-Updater");

                var response = await HttpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return (false, "", "");

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var release = JsonSerializer.Deserialize<ReleaseInfo>(content, options);

                if (release == null) return (false, "", "");

                string currentVersion = AppSettings.General.CurrentVersion;
                string latestVersion = release.TagName.TrimStart('v');

                if (IsNewerVersion(currentVersion, latestVersion))
                {
                    string os = OS.GetName().ToLower();
                    var asset = release.Assets.FirstOrDefault(a => a.Name.ToLower().Contains(os) && a.Name.EndsWith(".zip"));
                    
                    if (asset != null)
                    {
                        return (true, latestVersion, asset.BrowserDownloadUrl);
                    }
                }
            }
            catch (Exception e)
            {
                Terminal.WriteError($"Update check failed: {e.Message}");
            }

            return (false, "", "");
        }

        private static bool IsNewerVersion(string current, string latest)
        {
            try
            {
                var currentVer = new Version(current);
                var latestVer = new Version(latest);
                return latestVer > currentVer;
            }
            catch
            {
                return current != latest;
            }
        }

        public static async Task<string> DownloadUpdate(string url)
        {
            try
            {
                string tempDir = ProjectSettings.GlobalizePath("user://temp");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

                string zipPath = Path.Combine(tempDir, "update.zip");
                
                var response = await HttpClient.GetAsync(url);
                using (var fs = new FileStream(zipPath, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                }

                return zipPath;
            }
            catch (Exception e)
            {
                Terminal.WriteError($"Download failed: {e.Message}");
                return null;
            }
        }

        public static bool ApplyUpdate(string zipPath)
        {
            try
            {
                string tempDir = ProjectSettings.GlobalizePath("user://temp");
                string extractDir = Path.Combine(tempDir, "extracted");
                
                if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                Directory.CreateDirectory(extractDir);

                ZipFile.ExtractToDirectory(zipPath, extractDir);

                string exePath = OS.GetExecutablePath();
                string installDir = Path.GetDirectoryName(exePath);
                string os = OS.GetName();

                if (os == "Windows")
                {
                    CreateWindowsUpdater(extractDir, installDir, exePath);
                }
                else
                {
                    CreateUnixUpdater(extractDir, installDir, exePath);
                }

                return true;
            }
            catch (Exception e)
            {
                Terminal.WriteError($"Failed to prepare update: {e.Message}");
                return false;
            }
        }

        private static void CreateWindowsUpdater(string extractDir, string installDir, string exePath)
        {
            string batchPath = Path.Combine(Path.GetTempPath(), "hvergi_update.bat");
            string exeName = Path.GetFileName(exePath);

            string script = $@"
@echo off
timeout /t 2 /nobreak > nul
xcopy /s /y /i ""{extractDir}\*"" ""{installDir}\""
start """" ""{exePath}""
del ""%~f0""
";
            File.WriteAllText(batchPath, script);
            OS.Execute("cmd.exe", new string[] { "/c", "start", "/min", batchPath }, null, false);
        }

        private static void CreateUnixUpdater(string extractDir, string installDir, string exePath)
        {
            string scriptPath = Path.Combine(Path.GetTempPath(), "hvergi_update.sh");
            
            string script = $@"
#!/bin/bash
sleep 2
cp -R ""{extractDir}/""* ""{installDir}/""
chmod +x ""{exePath}""
""{exePath}"" &
rm ""$0""
";
            File.WriteAllText(scriptPath, script);
            OS.Execute("chmod", new string[] { "+x", scriptPath }, null, true);
            OS.Execute("bash", new string[] { "-c", $"\"{scriptPath}\" &" }, null, false);
        }
    }
}
