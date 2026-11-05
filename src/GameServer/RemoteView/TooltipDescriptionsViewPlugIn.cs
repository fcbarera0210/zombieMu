// <copyright file="TooltipDescriptionsViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Globalization;
using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends item and skill tooltip descriptions to an extended client.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.TooltipDescriptionsViewPlugIn_Name), Description = nameof(PlugInResources.TooltipDescriptionsViewPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("A7C3E1F2-4B5D-4E8A-9C1F-2D3E4F5A6B7C")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public class TooltipDescriptionsViewPlugIn : ITooltipDescriptionsViewPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="TooltipDescriptionsViewPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public TooltipDescriptionsViewPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc/>
    public async ValueTask ShowTooltipDescriptionsAsync(IReadOnlyList<ItemDefinition> items, IReadOnlyList<Skill> skills)
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        var culture = this._player.Culture;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            await connection.SendItemTooltipDescriptionExtendedAsync(
                (ushort)i,
                (ushort)items.Count,
                item.Group,
                (ushort)item.Number,
                GetDescription(item.Description, culture)).ConfigureAwait(false);
        }

        for (var i = 0; i < skills.Count; i++)
        {
            var skill = skills[i];
            await connection.SendSkillTooltipDescriptionExtendedAsync(
                (ushort)i,
                (ushort)skills.Count,
                (ushort)skill.Number,
                GetDescription(skill.Description, culture)).ConfigureAwait(false);
        }
    }

    private static string GetDescription(Interfaces.LocalizedString description, CultureInfo culture)
    {
        return description.GetTranslation(culture) ?? string.Empty;
    }
}
