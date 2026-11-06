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
    /// Inclusive lower bound of quest groups reserved for Mission Broker missions.
    /// Each mission should use a unique group in this range so up to
    /// <see cref="MaxActiveMissions"/> can run in parallel with the native F6 quest UI.
    /// </summary>
    public const short QuestGroupMin = 200;

    /// <summary>
    /// Inclusive upper bound of quest groups reserved for Mission Broker missions.
    /// </summary>
    public const short QuestGroupMax = 299;

    /// <summary>
    /// Alias of <see cref="QuestGroupMin"/> for the sample Pest Control mission.
    /// </summary>
    public const short QuestGroup = QuestGroupMin;

    /// <summary>
    /// Maximum number of Mission Broker missions a character may have active at once.
    /// </summary>
    public const int MaxActiveMissions = 5;

    /// <summary>
    /// Chat keyword to abandon an active mission while the menu is open.
    /// Kept for backwards compatibility; the F6 quest window is the preferred abandon path.
    /// </summary>
    public const string AbandonKeyword = "abandon";

    /// <summary>
    /// How long the numbered mission menu stays valid after talking to the NPC.
    /// Kept for the legacy chat plugin; unused when that plugin is inactive.
    /// </summary>
    public static readonly TimeSpan MenuTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Determines whether the given quest group belongs to the Mission Broker range.
    /// </summary>
    /// <param name="group">The quest group.</param>
    /// <returns><c>true</c> when the group is reserved for Mission Broker missions.</returns>
    public static bool IsMissionBrokerGroup(short group) => group >= QuestGroupMin && group <= QuestGroupMax;
}
