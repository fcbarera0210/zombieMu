// <copyright file="ItemDefinitionGridFilter.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>
/// Pure filter/format helpers for the <see cref="ItemDefinition"/> admin grid.
/// </summary>
public static class ItemDefinitionGridFilter
{
    /// <summary>
    /// Item categories available in the admin items grid.
    /// </summary>
    public enum ItemCategory
    {
        /// <summary>No category filter.</summary>
        All,

        /// <summary>Weapon groups 0–5.</summary>
        Weapons,

        /// <summary>Armor slots helm through boots.</summary>
        Armor,

        /// <summary>Wings (see <see cref="ItemExtensions.IsWing(ItemDefinition)"/>).</summary>
        Wings,

        /// <summary>Any item with an equipment slot.</summary>
        AllWearable,
    }

    /// <summary>
    /// Determines whether <paramref name="item"/> matches the selected character class filter.
    /// </summary>
    /// <param name="item">The item definition.</param>
    /// <param name="selectedClass">The selected class, or <see langword="null"/> for all classes.</param>
    /// <returns>
    /// <see langword="true"/> when no class is selected, or when the item lists that class in
    /// <see cref="ItemDefinition.QualifiedCharacters"/>. Items with an empty qualified list are
    /// excluded when a class is selected.
    /// </returns>
    public static bool MatchesClass(ItemDefinition item, CharacterClass? selectedClass)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (selectedClass is null)
        {
            return true;
        }

        return item.QualifiedCharacters?.Contains(selectedClass) is true;
    }

    /// <summary>
    /// Determines whether <paramref name="item"/> matches the selected category.
    /// </summary>
    /// <param name="item">The item definition.</param>
    /// <param name="category">The selected category.</param>
    /// <returns><see langword="true"/> when the item belongs to the category.</returns>
    public static bool MatchesCategory(ItemDefinition item, ItemCategory category)
    {
        ArgumentNullException.ThrowIfNull(item);

        return category switch
        {
            ItemCategory.All => true,
            ItemCategory.Weapons => item.Group <= 5,
            ItemCategory.Armor => IsArmor(item),
            ItemCategory.Wings => item.IsWing(),
            ItemCategory.AllWearable => item.ItemSlot is not null,
            _ => true,
        };
    }

    /// <summary>
    /// Formats item requirements for display in the grid, highest minimum value first.
    /// </summary>
    /// <param name="requirements">The requirements collection.</param>
    /// <returns>A comma-separated list, or an empty string when there are none.</returns>
    public static string FormatRequirements(IEnumerable<AttributeRequirement>? requirements)
    {
        if (requirements is null)
        {
            return string.Empty;
        }

        var ordered = requirements
            .OrderByDescending(r => r.MinimumValue)
            .ThenBy(r => r.Attribute?.Designation ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Select(FormatRequirement)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        if (ordered.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(", ", ordered);
    }

    private static string FormatRequirement(AttributeRequirement requirement)
    {
        var attributeName = requirement.Attribute?.Designation;
        if (string.IsNullOrWhiteSpace(attributeName))
        {
            attributeName = requirement.Attribute?.ToString() ?? "?";
        }

        return $"{attributeName}: {requirement.MinimumValue}";
    }

    private static bool IsArmor(ItemDefinition item)
    {
        if (item.ItemSlot?.ItemSlots is not { } slots)
        {
            return false;
        }

        return slots.Any(slot =>
            slot >= InventoryConstants.HelmSlot && slot <= InventoryConstants.BootsSlot);
    }
}
