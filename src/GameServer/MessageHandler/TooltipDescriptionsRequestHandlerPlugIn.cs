// <copyright file="TooltipDescriptionsRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handler for the request of item and skill tooltip descriptions.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.TooltipDescriptionsRequestHandlerPlugIn_Name), Description = nameof(PlugInResources.TooltipDescriptionsRequestHandlerPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("B8D4F2E3-5C6A-4F9B-8D2E-3A4B5C6D7E8F")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
[BelongsToGroup(ChatCommandGroupHandlerPlugIn.GroupKey)]
internal class TooltipDescriptionsRequestHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => TooltipDescriptionsRequest.SubCode;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (player.SelectedCharacter is null || player.GameContext.Configuration is not { } configuration)
        {
            return;
        }

        var items = configuration.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.Description.Value))
            .OrderBy(item => item.Group)
            .ThenBy(item => item.Number)
            .ToList();

        var skills = configuration.Skills
            .Where(skill => !string.IsNullOrWhiteSpace(skill.Description.Value))
            .OrderBy(skill => skill.Number)
            .ToList();

        await player.InvokeViewPlugInAsync<ITooltipDescriptionsViewPlugIn>(p => p.ShowTooltipDescriptionsAsync(items, skills)).ConfigureAwait(false);
    }
}
