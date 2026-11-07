// <copyright file="QuestTextHelper.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Quest;

using System.Globalization;
using MUnique.OpenMU.DataModel.Configuration.Quests;

/// <summary>
/// Builds Extended quest title/summary text for clients without BMD quest scripts.
/// </summary>
internal static class QuestTextHelper
{
    /// <summary>
    /// Gets the display name for the quest.
    /// </summary>
    /// <param name="quest">The quest.</param>
    /// <param name="culture">The player culture.</param>
    /// <returns>The name, or an empty string.</returns>
    public static string GetName(QuestDefinition? quest, CultureInfo culture)
    {
        return quest?.Name.GetTranslation(culture)
               ?? quest?.Name.ToString()
               ?? string.Empty;
    }

    /// <summary>
    /// Builds a short summary from kill/item requirements so the client can show "what to do"
    /// even though <see cref="QuestDefinition"/> has no free-text description field.
    /// </summary>
    /// <param name="quest">The quest.</param>
    /// <param name="culture">The player culture.</param>
    /// <returns>A summary string (falls back to the quest name).</returns>
    public static string BuildSummary(QuestDefinition? quest, CultureInfo culture)
    {
        if (quest is null)
        {
            return string.Empty;
        }

        var name = GetName(quest, culture);
        var parts = new List<string>();

        foreach (var kill in quest.RequiredMonsterKills)
        {
            var monster = kill.Monster?.Designation ?? "Monster";
            parts.Add($"{monster} x{kill.MinimumNumber}");
        }

        foreach (var item in quest.RequiredItems)
        {
            var itemName = item.Item?.Name ?? "Item";
            parts.Add($"{itemName} x{item.MinimumNumber}");
        }

        if (parts.Count == 0)
        {
            return name;
        }

        var summary = string.Join(", ", parts);
        if (string.IsNullOrEmpty(name))
        {
            return summary;
        }

        // QuestTextExtended summary field is 128 UTF-8 bytes; keep headroom.
        var combined = $"{name}: {summary}";
        return combined.Length <= 120 ? combined : summary;
    }
}
