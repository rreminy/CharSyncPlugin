using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin;
using ImGuiNET;
using System.Diagnostics;
using CharSync.Config;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Game.Command;

namespace CharSync
{
    public sealed class ConfigurationWindow : IDisposable
    {
        public CharSyncPlugin Plugin { get; }
        public DalamudPluginInterface PluginInterface => this.Plugin.PluginInterface;
        public Configuration Config => this.Plugin.Config;

        private int configGroupIndex;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "its fine")]
        public bool IsVisible;

        public ConfigurationWindow(CharSyncPlugin plugin)
        {
            this.Plugin = plugin;

            this.Plugin.Command.AddHandler("/pcharsync", new CommandInfo((cmd, args) => this.OpenConfigUiHandler())
            {
                HelpMessage = "Open CharSync Configuration"
            });

            this.PluginInterface.UiBuilder.Draw += this.Draw;
            this.PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUiHandler;
        }

        private void OpenConfigUiHandler()
        {
            this.IsVisible = true;
        }

        public void Draw()
        {
            if (!this.IsVisible) return;

            ImGui.SetNextWindowSize(new(300, 400), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new(400, 600), new(int.MaxValue, int.MaxValue));
            if (ImGui.Begin("CharSync Configuration", ref this.IsVisible))
            {
                if (ImGui.Checkbox("Plugin Enabled", ref this.Config.GlobalEnable))
                {
                    this.PluginInterface.SavePluginConfig(this.Config);
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Enable Plugin Functionality");

                ImGui.SameLine();
                if (ImGui.Button("Show First Time Guide"))
                {
                    this.Plugin.FirstTimeWindow.IsVisible = true;
                }

                this.DrawKnownCharacters();
                this.DrawCharacterGroupConfig();
            }
            ImGui.End();
        }

        public void DrawKnownCharacters()
        {
            if (ImGui.TreeNodeEx("Known Characters", ImGuiTreeNodeFlags.CollapsingHeader | ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();

                ImGui.TextUnformatted("Select the synchronization group for each character.");
                ImGui.TextUnformatted("Sync happens upon changing zones and logging out.");
                ImGui.TextUnformatted("To perform a manual sync logout first");
                ImGui.TextUnformatted("Right click on a character name to open the data directory.");

                ImGui.Columns(2, "characters", true);
                ImGui.TextUnformatted("Character"); ImGui.NextColumn();
                ImGui.TextUnformatted("Group"); ImGui.NextColumn();

                ImGui.Separator();
                foreach (var (characterId, characterName) in this.Config.CharacterNames.OrderBy(c => c.Value))
                {
                    ImGui.TextUnformatted(characterName);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip($"{characterId:X16}");
                    }

                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        Task.Run(() => Process.Start(new ProcessStartInfo
                        {
                            FileName = CharSyncPlugin.GetCharacterDirectory(characterId),
                            UseShellExecute = true
                        }));
                    }
                    ImGui.NextColumn();

                    this.DrawCharacterGroupCombo(characterId);

                    if (!this.Plugin.ClientState.IsLoggedIn)
                    {
                        ImGui.SameLine();
                        ImGui.PushFont(UiBuilder.IconFont);
                        if (ImGui.Button($"{((FontAwesomeIcon)0xF0E2).ToIconString()}##perform_sync_{characterId}"))
                        {
                            var oldEnabled = this.Config.GlobalEnable;
                            this.Config.GlobalEnable = true;
                            this.Plugin.PerformSync(characterId);
                            this.Config.GlobalEnable = oldEnabled;
                        }
                        ImGui.PopFont();
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Perform a sync using this character as the starting point");
                        }
                    }
                    ImGui.NextColumn();
                }

                ImGui.Columns(1);

                ImGui.Unindent();
            }
        }

        public void DrawCharacterGroupConfig()
        {
            if (ImGui.TreeNodeEx("Group Configuration", ImGuiTreeNodeFlags.CollapsingHeader | ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();
                var items = new List<string>() { "-- None --" }.Concat(this.Config.SyncGroups.Select(g => g.Name)).ToArray();
                ImGui.Combo("##config_group", ref this.configGroupIndex, items, items.Length);
                var index = this.configGroupIndex - 1;

                ImGui.PushFont(UiBuilder.IconFont);

                ImGui.SameLine();
                if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString()))
                {
                    this.Config.SyncGroups.Add(new()
                    {
                        Name = $"Group {this.Config.SyncGroups.Count + 1}",
                    });
                    this.configGroupIndex = this.Config.SyncGroups.Count; // Trust me, it'll land on the correct group
                    this.PluginInterface.SavePluginConfig(this.Config);
                }

                ImGui.SameLine();
                if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString()) && index >= 0)
                {
                    this.Config.SyncGroups.RemoveAt(index);
                    this.configGroupIndex--;
                    index--;
                    this.Config.RefreshCharacterGroups();
                    this.PluginInterface.SavePluginConfig(this.Config);
                }

                ImGui.PopFont();

                if (index >= 0)
                {
                    bool save = false;
                    var group = this.Config.SyncGroups[index];

                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    ImGui.TextUnformatted("Selected Group Configuration");
                    
                    ImGui.Text("Group Name");
                    ImGui.SameLine();
                    
                    save |= ImGui.InputText("##Group Name", ref group.Name, 32);
                    save |= ImGui.Checkbox("Enabled", ref group.Enabled);
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip("Enable this group");

                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Spacing();

                    save |= ImGui.Checkbox("HUD / UI Layout", ref group.Layout);
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip("Synchronize Hud / UI Layouts");

                    save |= ImGui.Checkbox("Gear sets", ref group.GearSets);
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip("Synchronize Gear Sets");

                    save |= ImGui.Checkbox("Hotbars", ref group.Hotbars);
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip("Synchronize Hotbars");
                    
                    save |= ImGui.Checkbox("Macros", ref group.Macros);
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip("Synchronize Macros");
                    
                    save |= ImGui.Checkbox("Keybinds", ref group.Keybinds);
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip("Synchronize Keybinds");

                    save |= ImGui.Checkbox("Log Filters", ref group.LogFilters);
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip("Synchronize Chat Log Filters");

                    save |= ImGui.Checkbox("Character Settings", ref group.CharacterSettings);
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip("Synchronize Character Settings");
                    
                    save |= ImGui.Checkbox("Keyboard Settings", ref group.KeyboardSettings);
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip("Synchronize Keyboard Settings");

                    save |= ImGui.Checkbox("Gamepad Settings", ref group.GamepadSettings);
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip("Synchronize Gamepad Settings");

                    save |= ImGui.Checkbox("Card Sets", ref group.CardSets);
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip("Synchronize Card Sets");

                    if (save) this.PluginInterface.SavePluginConfig(this.Config);
                }
                ImGui.Unindent();
            }
        }

        public void DrawCharacterGroupCombo(ulong characterId)
        {
            var group = this.Config.CharacterGroups.GetValueOrDefault(characterId);
            int index = 0;
            if (group is not null)
            {
                index = this.Config.SyncGroups.IndexOf(group) + 1;
            }

            var items = new List<string>() { "-- None --" }.Concat(this.Config.SyncGroups.Select(g => g.Name)).ToArray();
            if (ImGui.Combo($"##character_group_{characterId:X16}", ref index, items, items.Length))
            {
                if (index > 0) this.Config.SetCharacterGroup(characterId, this.Config.SyncGroups[index - 1]);
                else this.Config.RemoveCharacterGroup(characterId);
                this.PluginInterface.SavePluginConfig(this.Config);
            }
        }

        public void Dispose()
        {
            this.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUiHandler;
            this.PluginInterface.UiBuilder.Draw -= this.Draw;
            this.Plugin.Command.RemoveHandler("/pcharsync");
        }
    }
}
