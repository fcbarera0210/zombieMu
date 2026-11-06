// <copyright file="ShowAvailableQuestsExtendedPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Quest;

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic.PlayerActions.Quests;
using MUnique.OpenMU.GameLogic.Views.Quest;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The extended implementation of the <see cref="IShowAvailableQuestsPlugIn"/> which includes quest display names.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.ShowAvailableQuestsExtendedPlugIn_Name), Description = nameof(PlugInResources.ShowAvailableQuestsExtendedPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("A8F2C1D4-6E3B-4F09-9A7C-5D1E8B0F2436")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public class ShowAvailableQuestsExtendedPlugIn : IShowAvailableQuestsPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowAvailableQuestsExtendedPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public ShowAvailableQuestsExtendedPlugIn(RemotePlayer player)
    {
        this._player = player;
    }

    /// <inheritdoc />
    public async ValueTask ShowAvailableQuestsAsync()
    {
        var connection = this._player.Connection;
        if (connection is null || this._player.OpenedNpc is null)
        {
            return;
        }

        var npc = this._player.OpenedNpc.Definition;
        var linkedOnNpc = npc.Quests.Count;
        var totalQuests = this._player.GetAvailableQuestsOfOpenedNpc().ToList();
        var questCount = totalQuests.Count;

        this._player.Logger.LogInformation(
            "Available quests (extended) for NPC {NpcNumber} ({NpcName}): {AvailableCount} after filters (quests on monster collection: {LinkedCount}). Player level {Level}, class {Class}. Quests: {QuestList}",
            npc.Number,
            npc.Designation,
            questCount,
            linkedOnNpc,
            this._player.Level,
            this._player.SelectedCharacter?.CharacterClass?.Name,
            string.Join("; ", totalQuests.Select(q => $"{q.Name}[G{q.Group}/S{q.StartingNumber}/N{q.Number} lvl {q.MinimumCharacterLevel}-{q.MaximumCharacterLevel}]")));

        int Write()
        {
            var size = AvailableQuestsExtendedRef.GetRequiredSize(questCount);
            var span = connection.Output.GetSpan(size)[..size];
            var packet = new AvailableQuestsExtendedRef(span)
            {
                QuestCount = (ushort)questCount,
                QuestNpcNumber = (ushort)npc.Number,
            };

            for (int i = 0; i < questCount; i++)
            {
                var block = packet[i];
                var quest = totalQuests[i];
                block.Number = (ushort)quest.StartingNumber;
                block.Group = (ushort)quest.Group;
                block.Name = quest.Name.GetTranslation(this._player.Culture)
                             ?? quest.Name.ToString()
                             ?? string.Empty;
            }

            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}
