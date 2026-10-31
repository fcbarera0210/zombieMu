// <copyright file="MissionBrokerConstants.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.MissionBroker;

/// <summary>
/// Shared constants for the Mission Broker custom-quest feature.
/// </summary>
public static class MissionBrokerConstants
{
    /// <summary>
    /// NPC number used as the dedicated Mission Broker.
    /// Uses the Fireworks Girl client model (379). Do not use 372 — that id is the Elite Skeleton
    /// transformation skin and renders as an invisible/unclickable NPC.
    /// </summary>
    public const short NpcNumber = 379;

    /// <summary>
    /// Quest group reserved for Mission Broker missions.
    /// Keeps them out of the stock F6 quest UI. Multiple active missions in this group are allowed
    /// (one <see cref="DataModel.Entities.CharacterQuestState"/> per active mission).
    /// </summary>
    public const short QuestGroup = 200;

    /// <summary>
    /// Maximum number of Mission Broker missions a character may have active at once.
    /// </summary>
    public const int MaxActiveMissions = 5;

    /// <summary>
    /// Chat keyword to abandon an active mission while the menu is open.
    /// Use "abandon" for a single active mission, or "abandon N" to pick one by list index.
    /// </summary>
    public const string AbandonKeyword = "abandon";

    /// <summary>
    /// How long the numbered mission menu stays valid after talking to the NPC.
    /// During this window the player can type a mission number or ABANDON in normal chat.
    /// </summary>
    public static readonly TimeSpan MenuTimeout = TimeSpan.FromSeconds(60);
}
