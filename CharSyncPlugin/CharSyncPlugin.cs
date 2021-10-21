using System;
using System.Collections.Generic;
using System.Linq;
using CharSync.Config;
using Dalamud.Game.ClientState;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;

namespace CharSync
{
    public sealed class CharSyncPlugin : IDalamudPlugin
    {
        public readonly Configuration Config;

        public ConfigurationWindow ConfigWindow { get; }
        public FirstTimeWindow FirstTimeWindow { get; }

        private bool frameworkLogin;
        private bool frameworkSync;

        public ulong lastCharacterId { get; private set; }

        [PluginService] public DalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public ClientState ClientState { get; set; }
        [PluginService] public Framework Framework { get; set; }
        [PluginService] public CommandManager Command { get; set; }

        public string Name => "CharSync";

        public CharSyncPlugin()
        {
            this.ConfigWindow = new(this);
            this.FirstTimeWindow = new(this);
#pragma warning disable CS8601 // Possible null reference assignment. (Intentionally done)
            this.Config = (Configuration?)this.PluginInterface!.GetPluginConfig(); // GetPluginConfig() may return null
            if (this.Config is null)
            {
                this.Config = new Configuration();
                this.Config.SyncGroups.Add(new());
                this.ConfigWindow.IsVisible = true;
                this.FirstTimeWindow.IsVisible = true;
            }
            this.Config.RefreshCharacterGroups();
#pragma warning restore CS8601 // Possible null reference assignment.

            this.ClientState!.Login += this.LoginHandler;
            this.ClientState!.Logout += this.LogoutHandler;
            this.Framework!.Update += this.FrameworkHandler;
            this.ClientState!.TerritoryChanged += this.TerritoryHandler;

            if (this.ClientState.IsLoggedIn) this.LoginHandler(null, null!);
        }

        private void LoginHandler(object? obj, EventArgs args) => this.frameworkLogin = true;
        private void LogoutHandler(object? obj, EventArgs args) => this.frameworkSync = true;
        private void TerritoryHandler(object? obj, ushort territory) => this.frameworkSync = true;

        private void FrameworkHandler(Framework framework)
        {
            if (this.frameworkLogin)
            {
                var player = this.ClientState.LocalPlayer;
                if (player is null) return;
                this.frameworkLogin = false;

                var characterId = this.ClientState.LocalContentId;
                var name = $"{player.Name} <{player.HomeWorld.GameData.Name}>";
                PluginLog.LogInformation($"Logged in as {name} ({characterId:X16})");
                this.lastCharacterId = characterId;
                this.Config.CharacterNames[characterId] = name;
                this.PluginInterface.SavePluginConfig(this.Config);
            }
            else if (this.frameworkSync)
            {
                this.frameworkSync = false;
                if (!this.Config.CharacterNames.ContainsKey(this.lastCharacterId)) return;
                PluginLog.LogInformation($"Logged out from {this.Config.CharacterNames.GetValueOrDefault(this.lastCharacterId) ?? $"{this.lastCharacterId:X16}"}");
                this.PerformSync(this.lastCharacterId);
            }
        }

        /// <summary>
        /// Gets the game data directory.
        /// </summary>
        /// <returns>Game Data Directory</returns>
        public static string GetGameDataDirectory() => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "FINAL FANTASY XIV - A Realm Reborn");
        
        /// <summary>
        /// Gets the character data directory
        /// </summary>
        /// <param name="id">Character ID</param>
        /// <returns>Character Data Directory</returns>
        public static string GetCharacterDirectory(ulong id) => Path.Join(GetGameDataDirectory(), $"FFXIV_CHR{id:X16}");

        /// <summary>
        /// Get a specific file path
        /// </summary>
        /// <param name="id">Character ID</param>
        /// <param name="file">File name</param>
        /// <returns>File Path</returns>
        public static string GetFilePath(ulong id, string file) => Path.Join(GetCharacterDirectory(id), file);

        /// <summary>
        /// Perform synchronization from a specified character ID among its group
        /// </summary>
        /// <param name="from">Character ID</param>
        public void PerformSync(ulong from)
        {
            if (!this.Config.GlobalEnable || !this.Config.CharacterGroups.TryGetValue(from, out var group) || !group.Enabled) return;

            var ids = group.CharacterIds;
            if (ids.Count <= 1) return;

            PluginLog.LogInformation($"Synchronizing Character Data with {ids.Count} characters");
            foreach (var to in ids)
            {
                if (from == to) continue; // Would be pointless synchronizing with itself
                PluginLog.LogDebug($"{from:X16} => {to:X16}");
                if (group.Layout) File.Copy(GetFilePath(from, "ADDON.DAT"), GetFilePath(to, "ADDON.DAT"), true);
                if (group.GearSets) File.Copy(GetFilePath(from, "GEARSET.DAT"), GetFilePath(to, "GEARSET.DAT"), true);
                if (group.Hotbars) File.Copy(GetFilePath(from, "HOTBAR.DAT"), GetFilePath(to, "HOTBAR.DAT"), true);
                if (group.Macros) File.Copy(GetFilePath(from, "MACRO.DAT"), GetFilePath(to, "MACRO.DAT"), true);
                if (group.Keybinds) File.Copy(GetFilePath(from, "KEYBIND.DAT"), GetFilePath(to, "KEYBIND.DAT"), true);
                if (group.LogFilters) File.Copy(GetFilePath(from, "LOGFLTR.DAT"), GetFilePath(to, "LOGFLTR.DAT"), true);
                if (group.CharacterSettings) File.Copy(GetFilePath(from, "COMMON.DAT"), GetFilePath(to, "COMMON.DAT"), true);
                if (group.KeyboardSettings) File.Copy(GetFilePath(from, "CONTROL0.DAT"), GetFilePath(to, "CONTROL0.DAT"), true);
                if (group.GamepadSettings) File.Copy(GetFilePath(from, "CONTROL1.DAT"), GetFilePath(to, "CONTROL1.DAT"), true);
                if (group.CardSets) File.Copy(GetFilePath(from, "GS.DAT"), GetFilePath(to, "GS.DAT"), true);
            }
        }

        #region Backup Implementation
        private static Regex DirNameRegex = new("^FFXIV_CHR[0-9A-Z]{16}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static Regex FileNameRegex = new(@"^[A-Z]*\.DAT$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private int backupLock; // Simple Lock (Interlocked)
        public string BackupProgress { get; private set; } = string.Empty;

        /// <summary>
        /// Performs a backup of all character data
        /// </summary>
        public void PerformBackup()
        {
            if (Interlocked.Exchange(ref this.backupLock, 1) == 1) return;
            try
            {
                var dtString = DateTime.Now.ToString("s")
                    .Replace("-", "", StringComparison.InvariantCulture)
                    .Replace("T", "-", StringComparison.InvariantCulture)
                    .Replace(":", "", StringComparison.InvariantCulture);
                var archivePath = Path.Join(GetGameDataDirectory(), $"CharSync-{dtString}-Backup.zip");

                using var fileStream = new FileStream(archivePath, FileMode.CreateNew);
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

                var dataDir = new DirectoryInfo(GetGameDataDirectory());
                var directories = dataDir.EnumerateDirectories("FFXIV_CHR*")
                    .Where(d => DirNameRegex.IsMatch(d.Name))
                    .ToList(); // Done to be able to .Count

                var count = 0;
                foreach (var directory in directories)
                {
                    this.BackupProgress = $"Creating Backup...{100 * ++count / directories.Count}%";
                    var files = directory.EnumerateFiles()
                        .Where(f => FileNameRegex.IsMatch(f.Name));

                    foreach (var file in files)
                    {
                        var archiveFilePath = Path.Join(directory.Name, file.Name);
                        var realFilePath = Path.Join(directory.FullName, file.Name);
                        PluginLog.LogVerbose($"Backing up {archiveFilePath}");
                        archive.CreateEntryFromFile(realFilePath, archiveFilePath, CompressionLevel.Optimal);
                    }
                }

                this.BackupProgress = $"Backup Created: {archivePath}";
            }
            catch (Exception ex)
            {
                this.BackupProgress = $"Exception caught: {ex.Message}";
                PluginLog.LogError(ex, string.Empty);
            }
            finally
            {
                this.backupLock = 0; // No interlock needed
            }
        }
        #endregion

        public void Dispose()
        {
            this.ConfigWindow.Dispose();
            this.Framework.Update -= this.FrameworkHandler;
            this.ClientState.Logout -= this.LogoutHandler;
            this.ClientState.Login -= this.LoginHandler;
        }
    }
}
