using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharSync.Config
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "")]
    public sealed class CharacterGroup
    {
        /// <summary>
        /// Group Name
        /// </summary>
        public string Name = "Everything";

        /// <summary>
        /// Group Enabled
        /// </summary>
        public bool Enabled = true;

        /// <summary>
        /// Synchronize Hud Layout (ADDON.DAT)
        /// </summary>
        public bool Layout = true;

        /// <summary>
        /// Synchronize Gear Sets (GEARSET.DAT)
        /// </summary>
        public bool GearSets = true;

        /// <summary>
        /// Synchronize Hotbars (HOTBAR.DAT)
        /// </summary>
        public bool Hotbars = true;

        /// <summary>
        /// Synchronize Macros (MACRO.DAT)
        /// </summary>
        public bool Macros = true;

        /// <summary>
        /// Synchronize Keybinds (KEYBIND.DAT)
        /// </summary>
        public bool Keybinds = true;

        /// <summary>
        /// Synchronize Log Filters (LOGFLTR.DAT)
        /// </summary>
        public bool LogFilters = true;

        /// <summary>
        /// Synchronize Character Settings (COMMON.DAT)
        /// </summary>
        public bool CharacterSettings = true;

        /// <summary>
        /// Synchronize Keyboard Settings (CONTROL0.DAT)
        /// </summary>
        public bool KeyboardSettings = true;

        /// <summary>
        /// Synchronize Gamepad Settings (CONTROL1.DAT)
        /// </summary>
        public bool GamepadSettings = true;

        /// <summary>
        /// Synchronize Card Sets (GS.DAT)
        /// </summary>
        public bool CardSets = true;

        /// <summary>
        /// Character IDs to synchronize
        /// </summary>
        public HashSet<ulong> CharacterIds = new();
    }
}
