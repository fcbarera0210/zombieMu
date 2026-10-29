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
    /// NPC number of the Elite Skill Soldier model, used as the dedicated Mission Broker.
    /// The client model id must exist in Monster.bmd; this number is unused by default gameplay.
    /// </summary>
    public const short NpcNumber = 372;

    /// <summary>
    /// Quest group reserved for Mission Broker missions.
    /// Keeps them out of the stock F6 quest UI and allows only one active mission at a time.
    /// </summary>
    public const short QuestGroup = 200;

    /// <summary>
    /// How long a pending accept/abandon confirmation stays valid.
    /// After it expires, the next talk cycles to the next available mission instead of confirming.
    /// </summary>
    public static readonly TimeSpan ConfirmationTimeout = TimeSpan.FromSeconds(12);
}
