using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Configuration;
using Newtonsoft.Json;

namespace CharSync.Config
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "")]
    [JsonObject]
    public sealed class Configuration : IPluginConfiguration
    {
        /// <summary>
        /// Configuration version
        /// </summary>
        [JsonProperty] public int Version { get; set; } = 1;

        /// <summary>
        /// Globally enable character synchronization operation
        /// </summary>
        [JsonProperty] public bool GlobalEnable = false;

        /// <summary>
        /// Synchronization groups
        /// </summary>
        [JsonProperty] public List<CharacterGroup> SyncGroups = new();
        
        /// <summary>
        /// Known Character IDs and Names
        /// </summary>
        [JsonProperty] public Dictionary<ulong, string> CharacterNames = new();
        
        /// <summary>
        /// Character Groups
        /// </summary>
        [JsonIgnore] public Dictionary<ulong, CharacterGroup> CharacterGroups = new();

        /// <summary>
        /// Set a new character group for a specified character ID
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <param name="newGroup">Synchronization Group</param>
        public void SetCharacterGroup(ulong characterId, CharacterGroup newGroup)
        {
            this.RemoveCharacterGroup(characterId);
            newGroup.CharacterIds.Add(characterId);
            this.CharacterGroups.Add(characterId, newGroup);
        }

        /// <summary>
        /// Stop character synchronization from a character ID
        /// </summary>
        /// <param name="characterId">Character ID</param>
        public void RemoveCharacterGroup(ulong characterId)
        {
            if (this.CharacterGroups.TryGetValue(characterId, out var oldGroup))
            {
                oldGroup.CharacterIds.Remove(characterId);
            }
            this.CharacterGroups.Remove(characterId);
        }

        /// <summary>
        /// Refresh synchronization groups. Needed if adding and removing group. You may also want to run this if you change each group character IDs manually.
        /// Warning: Undefined behavior may occur if a character is placed in multiple groups.
        /// </summary>
        public void RefreshCharacterGroups()
        {
            var characterGroup = new Dictionary<ulong, CharacterGroup>();
            foreach (var group in SyncGroups)
            {
                foreach (var id in group.CharacterIds)
                {
                    characterGroup[id] = group;
                }
            }
            this.CharacterGroups = characterGroup;
        }
    }
}
