// <copyright file="AddPotionTooltipDescriptionsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Linq.Expressions;
using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence.Initialization.Properties;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds tooltip descriptions to healing, mana, shield and complex potions.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.AddPotionTooltipDescriptionsUpdatePlugIn_Name), Description = nameof(PlugInResources.AddPotionTooltipDescriptionsUpdatePlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("E1F2A3B4-C5D6-4789-8ABC-DEF012345678")]
public class AddPotionTooltipDescriptionsUpdatePlugIn : UpdatePlugInBase
{
    private static readonly (byte Group, short Number, Expression<Func<string>> Description)[] Descriptions =
    [
        (14, 1, () => ItemDescriptions.SmallHealingPotion),
        (14, 2, () => ItemDescriptions.MediumHealingPotion),
        (14, 3, () => ItemDescriptions.LargeHealingPotion),
        (14, 4, () => ItemDescriptions.SmallManaPotion),
        (14, 5, () => ItemDescriptions.MediumManaPotion),
        (14, 6, () => ItemDescriptions.LargeManaPotion),
        (14, 35, () => ItemDescriptions.SmallShieldPotion),
        (14, 36, () => ItemDescriptions.MediumShieldPotion),
        (14, 37, () => ItemDescriptions.LargeShieldPotion),
        (14, 38, () => ItemDescriptions.SmallComplexPotion),
        (14, 39, () => ItemDescriptions.MediumComplexPotion),
        (14, 40, () => ItemDescriptions.LargeComplexPotion),
    ];

    /// <inheritdoc />
    public override string Name => PlugInResources.AddPotionTooltipDescriptionsUpdatePlugIn_Name;

    /// <inheritdoc />
    public override string Description => PlugInResources.AddPotionTooltipDescriptionsUpdatePlugIn_Description;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 5, 16, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
#pragma warning disable CS1998
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
#pragma warning restore CS1998
    {
        foreach (var (group, number, description) in Descriptions)
        {
            if (gameConfiguration.Items.FirstOrDefault(item => item.Group == group && item.Number == number) is ItemDefinition item)
            {
                item.Description = LocalizedString.FromResource(description);
            }
        }
    }
}
