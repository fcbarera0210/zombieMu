// <copyright file="MissionBrokerSpawnHint.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.MissionBroker;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Quests;

/// <summary>
/// Builds approximate hunt-zone hints from <see cref="MonsterSpawnArea"/> data.
/// </summary>
internal static class MissionBrokerSpawnHint
{
    /// <summary>
    /// Formats hunt hints for every required monster of the quest (one line each when possible).
    /// </summary>
    /// <param name="player">The player (culture + current map preference).</param>
    /// <param name="quest">The quest.</param>
    /// <returns>Hint lines; empty if no spawn data is available.</returns>
    public static IList<string> FormatHuntHints(Player player, QuestDefinition quest)
    {
        var lines = new List<string>();
        foreach (var requirement in quest.RequiredMonsterKills)
        {
            if (requirement.Monster is not { } monster)
            {
                continue;
            }

            if (TryFormatMonsterHint(player, monster) is { } hint)
            {
                lines.Add(hint);
            }
        }

        return lines;
    }

    private static string? TryFormatMonsterHint(Player player, MonsterDefinition monster)
    {
        var spawn = FindBestSpawn(player, monster);
        if (spawn?.GameMap is null)
        {
            return null;
        }

        var centerX = (spawn.X1 + spawn.X2) / 2;
        var centerY = (spawn.Y1 + spawn.Y2) / 2;
        var mapName = spawn.GameMap.Name.GetTranslation(player.Culture)
                      ?? spawn.GameMap.Name.ToString()
                      ?? "the map";
        var direction = FormatCompass(centerX, centerY);
        var monsterName = monster.Designation.GetTranslation(player.Culture)
                          ?? monster.Designation.ToString()
                          ?? "targets";

        return direction is null
            ? $"Hunt {monsterName} in {mapName} ({centerX}, {centerY})"
            : $"Hunt {monsterName} in {mapName}, {direction} ({centerX}, {centerY})";
    }

    private static MonsterSpawnArea? FindBestSpawn(Player player, MonsterDefinition monster)
    {
        var maps = player.GameContext.Configuration.Maps;
        var currentMap = player.SelectedCharacter?.CurrentMap;

        var candidates = maps
            .SelectMany(map => map.MonsterSpawns
                .Where(spawn => spawn.MonsterDefinition is not null
                                && (ReferenceEquals(spawn.MonsterDefinition, monster)
                                    || spawn.MonsterDefinition.Number == monster.Number))
                .Select(spawn =>
                {
                    // Prefer the player's current map; then higher quantity spawn areas.
                    var mapBonus = currentMap is not null && (ReferenceEquals(spawn.GameMap, currentMap) || spawn.GameMap?.Number == currentMap.Number)
                        ? 1_000_000
                        : 0;
                    return (Spawn: spawn, Score: mapBonus + spawn.Quantity);
                }))
            .OrderByDescending(entry => entry.Score)
            .Select(entry => entry.Spawn)
            .FirstOrDefault();

        return candidates;
    }

    /// <summary>
    /// Approximate compass label from spawn center vs typical MU map mid (128,128).
    /// </summary>
    private static string? FormatCompass(int x, int y)
    {
        const int mid = 128;
        var dx = x - mid;
        var dy = y - mid;
        if (Math.Abs(dx) < 20 && Math.Abs(dy) < 20)
        {
            return "center";
        }

        // Client Y grows northward on the coordinate display players use.
        var northSouth = Math.Abs(dy) < 20 ? string.Empty : dy > 0 ? "north" : "south";
        var eastWest = Math.Abs(dx) < 20 ? string.Empty : dx > 0 ? "east" : "west";
        if (northSouth.Length == 0)
        {
            return eastWest.Length == 0 ? null : eastWest;
        }

        if (eastWest.Length == 0)
        {
            return northSouth;
        }

        return $"{northSouth}-{eastWest}";
    }
}
