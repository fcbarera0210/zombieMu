// <copyright file="CharacterLeaderboardEntry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Operations;

/// <summary>
/// An entry of the character leaderboard.
/// </summary>
/// <param name="Rank">The rank, starting at 1.</param>
/// <param name="CharacterName">The name of the character.</param>
/// <param name="ClassName">The name of the character class.</param>
/// <param name="Level">The level of the character.</param>
/// <param name="MasterLevel">The master level of the character.</param>
/// <param name="Resets">The number of resets of the character.</param>
public sealed record CharacterLeaderboardEntry(int Rank, string CharacterName, string ClassName, int Level, int MasterLevel, int Resets);
