// <copyright file="ItemDefinitionGridFilterTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Services;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Web.AdminPanel.Services;
using static MUnique.OpenMU.Web.AdminPanel.Services.ItemDefinitionGridFilter;
using BasicItemDefinition = MUnique.OpenMU.Persistence.BasicModel.ItemDefinition;
using BasicItemSlotType = MUnique.OpenMU.Persistence.BasicModel.ItemSlotType;
using BasicAttributeRequirement = MUnique.OpenMU.Persistence.BasicModel.AttributeRequirement;

/// <summary>Unit tests for <see cref="ItemDefinitionGridFilter"/>.</summary>
[TestFixture]
public sealed class ItemDefinitionGridFilterTests
{
    /// <summary>Without a class filter, every item matches.</summary>
    [Test]
    public void MatchesClassWithoutSelectionIncludesEmptyQualifiedList()
    {
        var item = CreateItem();
        Assert.That(MatchesClass(item, null), Is.True);
    }

    /// <summary>A selected class excludes items that list no qualified characters.</summary>
    [Test]
    public void MatchesClassExcludesEmptyQualifiedListWhenClassSelected()
    {
        var elf = CreateClass(1, "Fairy Elf");
        var item = CreateItem();
        Assert.That(MatchesClass(item, elf), Is.False);
    }

    /// <summary>A selected class matches items that list that class.</summary>
    [Test]
    public void MatchesClassIncludesQualifiedItem()
    {
        var elf = CreateClass(1, "Fairy Elf");
        var item = CreateItem();
        item.QualifiedCharacters.Add(elf);
        Assert.That(MatchesClass(item, elf), Is.True);
    }

    /// <summary>Weapon category uses groups 0–5.</summary>
    [Test]
    public void MatchesCategoryWeapons()
    {
        Assert.That(MatchesCategory(CreateItem(group: 0), ItemCategory.Weapons), Is.True);
        Assert.That(MatchesCategory(CreateItem(group: 5), ItemCategory.Weapons), Is.True);
        Assert.That(MatchesCategory(CreateItem(group: 6), ItemCategory.Weapons), Is.False);
    }

    /// <summary>Armor category uses helm–boots slots.</summary>
    [Test]
    public void MatchesCategoryArmor()
    {
        var armor = CreateItem(group: 7);
        armor.ItemSlot = CreateSlot(InventoryConstants.ArmorSlot);
        var weapon = CreateItem(group: 0);
        weapon.ItemSlot = CreateSlot(InventoryConstants.LeftHandSlot);
        Assert.That(MatchesCategory(armor, ItemCategory.Armor), Is.True);
        Assert.That(MatchesCategory(weapon, ItemCategory.Armor), Is.False);
    }

    /// <summary>Wings category uses <see cref="ItemExtensions.IsWing"/>.</summary>
    [Test]
    public void MatchesCategoryWings()
    {
        var wings = CreateItem(group: 12, width: 2, height: 2, maxLevel: 11, durability: 200);
        wings.ItemSlot = CreateSlot(InventoryConstants.WingsSlot);
        Assert.That(MatchesCategory(wings, ItemCategory.Wings), Is.True);
        Assert.That(MatchesCategory(CreateItem(group: 12), ItemCategory.Wings), Is.False);
    }

    /// <summary>All wearable matches any item with a slot.</summary>
    [Test]
    public void MatchesCategoryAllWearable()
    {
        var wearable = CreateItem();
        wearable.ItemSlot = CreateSlot(InventoryConstants.HelmSlot);
        Assert.That(MatchesCategory(wearable, ItemCategory.AllWearable), Is.True);
        Assert.That(MatchesCategory(CreateItem(), ItemCategory.AllWearable), Is.False);
    }

    /// <summary>Requirements are formatted highest first.</summary>
    [Test]
    public void FormatRequirementsOrdersByMinimumValueDescending()
    {
        var strength = new AttributeDefinition(Guid.NewGuid(), "Strength", "Strength");
        var agility = new AttributeDefinition(Guid.NewGuid(), "Agility", "Agility");
        var requirements = new List<AttributeRequirement>
        {
            new BasicAttributeRequirement { Attribute = agility, MinimumValue = 20 },
            new BasicAttributeRequirement { Attribute = strength, MinimumValue = 40 },
        };

        Assert.That(FormatRequirements(requirements), Is.EqualTo("Strength: 40, Agility: 20"));
        Assert.That(FormatRequirements(Array.Empty<AttributeRequirement>()), Is.EqualTo(string.Empty));
        Assert.That(FormatRequirements(null), Is.EqualTo(string.Empty));
    }

    private static CharacterClass CreateClass(byte number, string name)
        => new() { Number = number, Name = name };

    private static BasicItemDefinition CreateItem(byte group = 0, byte width = 1, byte height = 1, byte maxLevel = 0, byte durability = 1)
        => new()
        {
            Group = group,
            Width = width,
            Height = height,
            MaximumItemLevel = maxLevel,
            Durability = durability,
        };

    private static BasicItemSlotType CreateSlot(byte slot)
    {
        var slotType = new BasicItemSlotType();
        slotType.ItemSlots.Add(slot);
        return slotType;
    }
}
