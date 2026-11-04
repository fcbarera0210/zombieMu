// <copyright file="FormGroupCatalog.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Shared.Components.Form;

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;

/// <summary>
/// Groups generated form fields into sections.
/// A <see cref="DisplayAttribute.GroupName"/> wins. Otherwise the catalog below is used,
/// so the largest configuration forms can be split without adding attributes to every property.
/// </summary>
public static class FormGroupCatalog
{
    private static readonly Dictionary<Type, Dictionary<string, string>> GroupsByType = new()
    {
        [typeof(GameConfiguration)] = new()
        {
            [nameof(GameConfiguration.MaximumLevel)] = "Levels",
            [nameof(GameConfiguration.MaximumMasterLevel)] = "Levels",
            [nameof(GameConfiguration.MinimumMonsterLevelForMasterExperience)] = "Levels",
            [nameof(GameConfiguration.ExperienceRate)] = "Rates",
            [nameof(GameConfiguration.MasterExperienceRate)] = "Rates",
            [nameof(GameConfiguration.PreventExperienceOverflow)] = "Rates",
            [nameof(GameConfiguration.ExperienceFormula)] = "Rates",
            [nameof(GameConfiguration.MasterExperienceFormula)] = "Rates",
            [nameof(GameConfiguration.MaximumInventoryMoney)] = "Money",
            [nameof(GameConfiguration.MaximumVaultMoney)] = "Money",
            [nameof(GameConfiguration.ClampMoneyOnPickup)] = "Money",
            [nameof(GameConfiguration.ShouldDropMoney)] = "Money",
            [nameof(GameConfiguration.ExcellentItemDropLevelDelta)] = "Items",
            [nameof(GameConfiguration.ExcellentDropLevelBonus)] = "Items",
            [nameof(GameConfiguration.AncientDropLevelBonus)] = "Items",
            [nameof(GameConfiguration.ItemDropDuration)] = "Items",
            [nameof(GameConfiguration.MaximumItemOptionLevelDrop)] = "Items",
            [nameof(GameConfiguration.DamagePerOneItemDurability)] = "Items",
            [nameof(GameConfiguration.DamagePerOnePetDurability)] = "Items",
            [nameof(GameConfiguration.HitsPerOneItemDurability)] = "Items",
            [nameof(GameConfiguration.MaximumCharactersPerAccount)] = "Characters",
            [nameof(GameConfiguration.CharacterNameRegex)] = "Characters",
            [nameof(GameConfiguration.MaximumPasswordLength)] = "Characters",
            [nameof(GameConfiguration.MaximumPartySize)] = "Characters",
            [nameof(GameConfiguration.InfoRange)] = "Characters",
            [nameof(GameConfiguration.RecoveryInterval)] = "Characters",
            [nameof(GameConfiguration.AreaSkillHitsPlayer)] = "Characters",
            [nameof(GameConfiguration.MaximumLetters)] = "Letters",
            [nameof(GameConfiguration.LetterSendPrice)] = "Letters",
            [nameof(GameConfiguration.DuelConfiguration)] = "Events",
            [nameof(GameConfiguration.CastleSiegeConfiguration)] = "Events",
            [nameof(GameConfiguration.CashShopConfiguration)] = "Events",
            [nameof(GameConfiguration.JewelMixes)] = "Lists",
            [nameof(GameConfiguration.WarpList)] = "Lists",
            [nameof(GameConfiguration.DropItemGroups)] = "Lists",
            [nameof(GameConfiguration.Skills)] = "Lists",
            [nameof(GameConfiguration.CharacterClasses)] = "Lists",
            [nameof(GameConfiguration.Items)] = "Lists",
            [nameof(GameConfiguration.ItemLevelBonusTables)] = "Lists",
            [nameof(GameConfiguration.ItemStatProfiles)] = "Lists",
            [nameof(GameConfiguration.ItemPriceDefinitions)] = "Lists",
            [nameof(GameConfiguration.ItemSlotTypes)] = "Lists",
            [nameof(GameConfiguration.ItemOptions)] = "Lists",
            [nameof(GameConfiguration.ItemOptionTypes)] = "Lists",
            [nameof(GameConfiguration.ItemSetGroups)] = "Lists",
            [nameof(GameConfiguration.ItemOptionCombinationBonuses)] = "Lists",
            [nameof(GameConfiguration.Maps)] = "Lists",
            [nameof(GameConfiguration.Monsters)] = "Lists",
            [nameof(GameConfiguration.Attributes)] = "Lists",
            [nameof(GameConfiguration.MagicEffects)] = "Lists",
            [nameof(GameConfiguration.MasterSkillRoots)] = "Lists",
            [nameof(GameConfiguration.GlobalAttributeCombinations)] = "Lists",
            [nameof(GameConfiguration.GlobalBaseAttributeValues)] = "Lists",
            [nameof(GameConfiguration.PlugInConfigurations)] = "Lists",
            [nameof(GameConfiguration.MiniGameDefinitions)] = "Lists",
        },
        [typeof(MonsterDefinition)] = new()
        {
            [nameof(MonsterDefinition.Number)] = "Identity",
            [nameof(MonsterDefinition.Designation)] = "Identity",
            [nameof(MonsterDefinition.IsActive)] = "Identity",
            [nameof(MonsterDefinition.IntroducedIn)] = "Identity",
            [nameof(MonsterDefinition.ObjectKind)] = "Identity",
            [nameof(MonsterDefinition.NpcWindow)] = "Identity",
            [nameof(MonsterDefinition.AttackRange)] = "Combat",
            [nameof(MonsterDefinition.ViewRange)] = "Combat",
            [nameof(MonsterDefinition.AttackDelay)] = "Combat",
            [nameof(MonsterDefinition.AttackSkill)] = "Combat",
            [nameof(MonsterDefinition.Attribute)] = "Combat",
            [nameof(MonsterDefinition.MoveRange)] = "Combat",
            [nameof(MonsterDefinition.MoveDelay)] = "Combat",
            [nameof(MonsterDefinition.RespawnDelay)] = "Combat",
            [nameof(MonsterDefinition.NumberOfMaximumItemDrops)] = "Drops",
            [nameof(MonsterDefinition.DropItemGroups)] = "Drops",
            [nameof(MonsterDefinition.MerchantStore)] = "Role",
            [nameof(MonsterDefinition.ItemCraftings)] = "Role",
            [nameof(MonsterDefinition.Quests)] = "Role",
            [nameof(MonsterDefinition.IntelligenceTypeName)] = "Role",
            [nameof(MonsterDefinition.Attributes)] = "Role",
            [nameof(MonsterDefinition.Buffs)] = "Role",
        },
        [typeof(Skill)] = new()
        {
            [nameof(Skill.Number)] = "Identity",
            [nameof(Skill.Name)] = "Identity",
            [nameof(Skill.SkillType)] = "Identity",
            [nameof(Skill.AttackDamage)] = "Damage",
            [nameof(Skill.DamageType)] = "Damage",
            [nameof(Skill.NumberOfHitsPerAttack)] = "Damage",
            [nameof(Skill.Range)] = "Damage",
            [nameof(Skill.ElementalModifierTarget)] = "Damage",
            [nameof(Skill.SkipElementalModifier)] = "Damage",
            [nameof(Skill.Target)] = "Targeting",
            [nameof(Skill.ImplicitTargetRange)] = "Targeting",
            [nameof(Skill.TargetRestriction)] = "Targeting",
            [nameof(Skill.MovesToTarget)] = "Targeting",
            [nameof(Skill.MovesTarget)] = "Targeting",
            [nameof(Skill.Requirements)] = "Requirements",
            [nameof(Skill.ConsumeRequirements)] = "Requirements",
            [nameof(Skill.AttributeRelationships)] = "Requirements",
            [nameof(Skill.QualifiedCharacters)] = "Requirements",
            [nameof(Skill.MagicEffectDef)] = "Effects",
            [nameof(Skill.MasterDefinition)] = "Effects",
            [nameof(Skill.AreaSkillSettings)] = "Effects",
        },
    };

    /// <summary>
    /// Gets the section name for a property, or <see langword="null"/> when the property stays ungrouped.
    /// </summary>
    /// <param name="modelType">The runtime model type.</param>
    /// <param name="property">The property.</param>
    /// <returns>The section name.</returns>
    public static string? GetGroupName(Type modelType, PropertyInfo property)
    {
        var declared = property.GetCustomAttribute<DisplayAttribute>(true)?.GetGroupName();
        if (!string.IsNullOrWhiteSpace(declared))
        {
            return declared;
        }

        for (var type = modelType; type is not null; type = type.BaseType)
        {
            if (GroupsByType.TryGetValue(type, out var groups)
                && groups.TryGetValue(property.Name, out var groupName))
            {
                return groupName;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the localized section caption.
    /// </summary>
    /// <param name="modelType">The runtime model type.</param>
    /// <param name="groupName">The section name returned by <see cref="GetGroupName"/>.</param>
    /// <returns>The caption shown on the section.</returns>
    public static string GetCaption(Type modelType, string groupName)
    {
        return modelType.GetPropertyCaption($"Group_{groupName}");
    }
}
