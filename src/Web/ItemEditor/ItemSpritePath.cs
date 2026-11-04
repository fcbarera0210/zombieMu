// <copyright file="ItemSpritePath.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.ItemEditor;

/// <summary>
/// Builds the inventory sprite path used by <see cref="MuItem"/>.
/// Not every item level has its own picture. Wearable items share a picture across pairs of levels.
/// </summary>
public static class ItemSpritePath
{
    /// <summary>
    /// The static web asset folder of the item pictures, relative to the application.
    /// </summary>
    public const string AssetFolder = "_content/MUnique.OpenMU.Web.ItemEditor/img/items/";

    private static readonly int[] LevelMapping = { 0, 0, 0, 3, 3, 5, 5, 7, 7, 9, 9, 11, 11, 13, 13, 15, 15 };

    /// <summary>
    /// Gets the picture level for an item level.
    /// Wings and trainable pets always use level 0. Wearable items use <see cref="LevelMapping"/>.
    /// </summary>
    /// <param name="itemLevel">The item level.</param>
    /// <param name="isWearable">Whether the item can be equipped.</param>
    /// <param name="isWingOrTrainablePet">Whether the item is a wing or a trainable pet.</param>
    /// <returns>The level segment of the picture file name.</returns>
    public static int GetEffectLevel(byte itemLevel, bool isWearable, bool isWingOrTrainablePet)
    {
        if (isWingOrTrainablePet)
        {
            return 0;
        }

        if (isWearable)
        {
            return itemLevel < LevelMapping.Length ? LevelMapping[itemLevel] : 0;
        }

        return itemLevel;
    }

    /// <summary>
    /// Gets the file suffix for ancient or excellent pictures.
    /// Ancient wins. Excellent pictures exist only for groups below 12.
    /// </summary>
    /// <param name="group">The item group.</param>
    /// <param name="isExcellent">Whether the picture should be the excellent variant.</param>
    /// <param name="isAncient">Whether the picture should be the ancient variant.</param>
    /// <returns>The suffix, including the leading underscore, or an empty string.</returns>
    public static string GetOptionSuffix(byte group, bool isExcellent, bool isAncient)
    {
        if (isAncient)
        {
            return "_a";
        }

        if (group < 12 && isExcellent)
        {
            return "_e";
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the relative URL of the item picture.
    /// </summary>
    /// <param name="group">The item group.</param>
    /// <param name="number">The item number inside the group.</param>
    /// <param name="effectLevel">The picture level from <see cref="GetEffectLevel"/>.</param>
    /// <param name="optionSuffix">The suffix from <see cref="GetOptionSuffix"/>.</param>
    /// <returns>The relative picture URL.</returns>
    public static string GetRelativeUrl(byte group, short number, int effectLevel, string optionSuffix)
    {
        return $"{AssetFolder}item_{group}_{number}_{effectLevel}{optionSuffix}.png";
    }
}
