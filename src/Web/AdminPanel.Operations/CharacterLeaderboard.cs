// <copyright file="CharacterLeaderboard.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Operations;

/// <summary>
/// The leaderboard of the characters of the server, shared by the admin panel and agents.
/// </summary>
/// <param name="Summary">The scope of the leaderboard.</param>
/// <param name="Entries">The best characters, ordered by resets, master level, level and experience.</param>
public sealed record CharacterLeaderboard(string Summary, IReadOnlyList<CharacterLeaderboardEntry> Entries);
