using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace CharSync
{
    public sealed class FirstTimeWindow
    {
        public bool IsVisible = false;
        private CharSyncPlugin Plugin { get; set; }

        public FirstTimeWindow(CharSyncPlugin plugin)
        {
            this.Plugin = plugin;

            this.Plugin.PluginInterface.UiBuilder.Draw += this.Draw;
        }

        private void Draw()
        {
            if (!this.IsVisible) return;

            ImGui.SetNextWindowSize(new(400, 600));
            ImGui.SetNextWindowSizeConstraints(new(400, 600), new(int.MaxValue, int.MaxValue));
            if (ImGui.Begin("CharSync Instructions"))
            {
                ImGui.TextUnformatted("Welcome to CharSync");

                ImGui.TextWrapped("To get started:");
                ImGui.Spacing();
                ImGui.TextWrapped("1. Log into all characters you wish to synchronize their data with. Plugin will add each character you log into to the Known Characters, from which you can configure synchronization later.");
                ImGui.Spacing();
                ImGui.TextWrapped("2. After you're done logging into all characters, set their synchronization groups and settings. One has been provided by default named Everything to get you started. You can configure groups in Group Configuration.");
                ImGui.Spacing();
                ImGui.TextWrapped("3. Once you have everything setup, make sure you're on the character you want to synchronize from then check Plugin Enabled at the top of the configuration window. The data will then start synchronizing automatically every time you change zones or logoff.");
                ImGui.Spacing();
                ImGui.TextWrapped("4. Enjoy your new unified experience.");

                ImGui.Spacing();

                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 0.0f, 1.0f));
                ImGui.TextWrapped("Be sure to follow the instructions above correctly, failure to do so may result in overwritten data! Plugin is disabled by default to avoid this from happening.");
                ImGui.PopStyleColor();

                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.Spacing();

                ImGui.TextWrapped("This Plugin has been remade by Azure Gem based on goat's work of CharacterSync, and therefore shares a lot of similar functionality.");
                ImGui.TextWrapped("This plugin has been remade without the use of hooks and instead uses events built into XIVLauncher / Dalamud.");
                ImGui.TextWrapped("Credits for the original work goes to goat");
            }
            ImGui.End();
        }

        public void Dispose()
        {
            this.Plugin.PluginInterface.UiBuilder.Draw -= this.Draw;
        }
    }
}
