// <copyright file="MissionBrokerInteractionState.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.MissionBroker;

using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// Kind of Mission Broker menu currently open for chat input.
/// </summary>
internal enum MissionBrokerMenuKind
{
    /// <summary>
    /// No menu is open.
    /// </summary>
    None,

    /// <summary>
    /// Accept offers and/or abandon actives via chat.
    /// </summary>
    Selection,
}

/// <summary>
/// Per-character talk/chat state for the Mission Broker NPC.
/// </summary>
internal sealed class MissionBrokerInteractionState
{
    /// <summary>
    /// Gets or sets the open menu kind.
    /// </summary>
    public MissionBrokerMenuKind MenuKind { get; set; }

    /// <summary>
    /// Gets or sets when the open menu expires (UTC).
    /// </summary>
    public DateTime ExpiresUtc { get; set; }

    /// <summary>
    /// Gets the missions shown in the last offer list (1-based chat index maps into this list).
    /// </summary>
    public IList<QuestDefinition> OfferedMissions { get; } = new List<QuestDefinition>();

    /// <summary>
    /// Gets the active missions shown for abandon (1-based index after the keyword ABANDON).
    /// </summary>
    public IList<CharacterQuestState> ActiveMissions { get; } = new List<CharacterQuestState>();

    /// <summary>
    /// Gets a value indicating whether a chat menu is still valid.
    /// </summary>
    public bool HasOpenMenu =>
        this.MenuKind != MissionBrokerMenuKind.None
        && DateTime.UtcNow <= this.ExpiresUtc;

    /// <summary>
    /// Clears the open menu.
    /// </summary>
    public void ClearMenu()
    {
        this.MenuKind = MissionBrokerMenuKind.None;
        this.ExpiresUtc = DateTime.MinValue;
        this.OfferedMissions.Clear();
        this.ActiveMissions.Clear();
    }

    /// <summary>
    /// Opens the selection menu with available offers and optional actives for abandon.
    /// </summary>
    /// <param name="offers">Missions that can be accepted.</param>
    /// <param name="actives">Active mission states (for abandon N).</param>
    public void OpenSelection(IEnumerable<QuestDefinition> offers, IEnumerable<CharacterQuestState> actives)
    {
        this.MenuKind = MissionBrokerMenuKind.Selection;
        this.ExpiresUtc = DateTime.UtcNow.Add(MissionBrokerConstants.MenuTimeout);
        this.OfferedMissions.Clear();
        this.ActiveMissions.Clear();
        foreach (var offer in offers)
        {
            this.OfferedMissions.Add(offer);
        }

        foreach (var active in actives)
        {
            this.ActiveMissions.Add(active);
        }
    }
}
