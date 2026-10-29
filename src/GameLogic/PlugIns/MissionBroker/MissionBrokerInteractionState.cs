// <copyright file="MissionBrokerInteractionState.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.MissionBroker;

/// <summary>
/// Pending confirmation while talking to the Mission Broker (accept or abandon).
/// </summary>
internal enum MissionBrokerPendingAction
{
    /// <summary>
    /// No pending confirmation.
    /// </summary>
    None,

    /// <summary>
    /// The next talk will accept the offered mission.
    /// </summary>
    Accept,

    /// <summary>
    /// The next talk will abandon the active mission.
    /// </summary>
    Abandon,
}

/// <summary>
/// Per-character talk state for the Mission Broker NPC.
/// </summary>
internal sealed class MissionBrokerInteractionState
{
    /// <summary>
    /// Gets or sets the pending action.
    /// </summary>
    public MissionBrokerPendingAction PendingAction { get; set; }

    /// <summary>
    /// Gets or sets the quest group of the pending action.
    /// </summary>
    public short QuestGroup { get; set; }

    /// <summary>
    /// Gets or sets the quest number of the pending action.
    /// </summary>
    public short QuestNumber { get; set; }

    /// <summary>
    /// Gets or sets when the pending confirmation expires (UTC).
    /// </summary>
    public DateTime ExpiresUtc { get; set; }

    /// <summary>
    /// Gets or sets the last offered mission index (for cycling available missions).
    /// </summary>
    public int LastOfferIndex { get; set; } = -1;

    /// <summary>
    /// Gets a value indicating whether the pending confirmation is still valid.
    /// </summary>
    public bool HasValidPending =>
        this.PendingAction != MissionBrokerPendingAction.None
        && DateTime.UtcNow <= this.ExpiresUtc;

    /// <summary>
    /// Clears the pending confirmation.
    /// </summary>
    public void ClearPending()
    {
        this.PendingAction = MissionBrokerPendingAction.None;
        this.QuestGroup = 0;
        this.QuestNumber = 0;
        this.ExpiresUtc = DateTime.MinValue;
    }

    /// <summary>
    /// Sets a pending confirmation.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="group">The quest group.</param>
    /// <param name="number">The quest number.</param>
    public void SetPending(MissionBrokerPendingAction action, short group, short number)
    {
        this.PendingAction = action;
        this.QuestGroup = group;
        this.QuestNumber = number;
        this.ExpiresUtc = DateTime.UtcNow.Add(MissionBrokerConstants.ConfirmationTimeout);
    }
}
