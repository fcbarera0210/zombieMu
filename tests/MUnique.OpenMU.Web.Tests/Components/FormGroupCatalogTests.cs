// <copyright file="FormGroupCatalogTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Components;

using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Web.Shared.Components.Form;

/// <summary>
/// Tests for the section names of the generated configuration forms.
/// </summary>
[TestFixture]
public class FormGroupCatalogTests
{
    /// <summary>
    /// Experience settings of the game configuration land in the Rates section.
    /// </summary>
    [Test]
    public void GameConfiguration_ExperienceRate_IsInRates()
    {
        var property = typeof(GameConfiguration).GetProperty(nameof(GameConfiguration.ExperienceRate))!;

        Assert.That(FormGroupCatalog.GetGroupName(typeof(GameConfiguration), property), Is.EqualTo("Rates"));
        Assert.That(FormGroupCatalog.GetCaption(typeof(GameConfiguration), "Rates"), Is.EqualTo("Rates"));
    }

    /// <summary>
    /// A property without a section stays ungrouped.
    /// </summary>
    [Test]
    public void ItemDefinition_Number_StaysUngrouped()
    {
        var property = typeof(ItemDefinition).GetProperty(nameof(ItemDefinition.Number))!;

        Assert.That(FormGroupCatalog.GetGroupName(typeof(ItemDefinition), property), Is.Null);
    }

    /// <summary>
    /// Drop level explains the drop and the stat formulas, and names that impact.
    /// </summary>
    [Test]
    public void ItemDefinition_DropLevel_HasImpactAndDescription()
    {
        Assert.That(typeof(ItemDefinition).GetPropertyImpact(nameof(ItemDefinition.DropLevel)), Is.EqualTo("Drop, stats"));
        Assert.That(typeof(ItemDefinition).GetPropertyDescription(nameof(ItemDefinition.DropLevel)), Does.Contain("monster level"));
    }
}
