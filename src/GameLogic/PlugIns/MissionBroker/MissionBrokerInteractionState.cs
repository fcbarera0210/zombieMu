// <copyright file="MissionBrokerInteractionState.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.MissionBroker;

using MUnique.OpenMU.DataModel.Configuration.Quests;

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
    /// Numbered available missions; the player can type an index to accept.
    /// </summary>
    Offers,

    /// <summary>
    /// Active mission progress; the player can type ABANDON to quit.
    /// </summary>
    Abandon,
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
    }

    /// <summary>
    /// Opens the numbered offer menu with the given missions.
    /// </summary>
    /// <param name="missions">The missions shown to the player.</param>
    public void OpenOffers(IEnumerable<QuestDefinition> missions)
    {
        this.MenuKind = MissionBrokerMenuKind.Offers;
        this.ExpiresUtc = DateTime.UtcNow.Add(MissionBrokerConstants.MenuTimeout);
        this.OfferedMissions.Clear();
        foreach (var mission in missions)
        {
            this.OfferedMissions.Add(mission);
        }
    }

    /// <summary>
    /// Opens the abandon prompt for the active mission.
    /// </summary>
    public void OpenAbandonPrompt()
    {
        this.MenuKind = MissionBrokerMenuKind.Abandon;
        this.ExpiresUtc = DateTime.UtcNow.Add(MissionBrokerConstants.MenuTimeout);
        this.OfferedMissions.Clear();
    }
}
