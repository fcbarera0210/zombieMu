// <copyright file="ItemSpritePathTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Components;

using MUnique.OpenMU.Web.ItemEditor;

/// <summary>
/// Tests for the inventory sprite path shared by the item editor and the inventory view.
/// </summary>
[TestFixture]
public class ItemSpritePathTests
{
    /// <summary>
    /// A plain item uses group, number and level with no option suffix.
    /// </summary>
    [Test]
    public void RelativeUrl_UsesGroupNumberAndLevel()
    {
        var url = ItemSpritePath.GetRelativeUrl(0, 1, 0, string.Empty);

        Assert.That(url, Is.EqualTo("_content/MUnique.OpenMU.Web.ItemEditor/img/items/item_0_1_0.png"));
    }

    /// <summary>
    /// Wearable items share one picture for a pair of levels, matching the inventory view.
    /// </summary>
    [Test]
    public void EffectLevel_MapsWearableLevelsInPairs()
    {
        Assert.That(ItemSpritePath.GetEffectLevel(4, isWearable: true, isWingOrTrainablePet: false), Is.EqualTo(3));
        Assert.That(ItemSpritePath.GetEffectLevel(5, isWearable: true, isWingOrTrainablePet: false), Is.EqualTo(5));
    }

    /// <summary>
    /// Wings and trainable pets always use the level 0 picture.
    /// </summary>
    [Test]
    public void EffectLevel_WingsStayAtZero()
    {
        Assert.That(ItemSpritePath.GetEffectLevel(15, isWearable: true, isWingOrTrainablePet: true), Is.EqualTo(0));
    }

    /// <summary>
    /// Ancient pictures win over excellent pictures. Excellent pictures exist only below group 12.
    /// </summary>
    [Test]
    public void OptionSuffix_AncientWinsAndExcellentStopsAtGroup12()
    {
        Assert.That(ItemSpritePath.GetOptionSuffix(0, isExcellent: true, isAncient: true), Is.EqualTo("_a"));
        Assert.That(ItemSpritePath.GetOptionSuffix(0, isExcellent: true, isAncient: false), Is.EqualTo("_e"));
        Assert.That(ItemSpritePath.GetOptionSuffix(12, isExcellent: true, isAncient: false), Is.EqualTo(string.Empty));
    }
}
