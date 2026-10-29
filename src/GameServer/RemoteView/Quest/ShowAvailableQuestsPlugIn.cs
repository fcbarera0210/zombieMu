// <copyright file="ShowAvailableQuestsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Quest;

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic.PlayerActions.Quests;
using MUnique.OpenMU.GameLogic.Views.Quest;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The default implementation of the <see cref="IShowAvailableQuestsPlugIn"/> which is forwarding everything to the game client with specific data packets.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.ShowAvailableQuestsPlugIn_Name), Description = nameof(PlugInResources.ShowAvailableQuestsPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("63C7B3E2-EF66-49BB-A9F8-EFBD2389588F")]
public class ShowAvailableQuestsPlugIn : IShowAvailableQuestsPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowAvailableQuestsPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public ShowAvailableQuestsPlugIn(RemotePlayer player)
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
            "Available quests for NPC {NpcNumber} ({NpcName}): {AvailableCount} after filters (quests on monster collection: {LinkedCount}). Player level {Level}, class {Class}. Quests: {QuestList}",
            npc.Number,
            npc.Designation,
            questCount,
            linkedOnNpc,
            this._player.Level,
            this._player.SelectedCharacter?.CharacterClass?.Name,
            string.Join("; ", totalQuests.Select(q => $"{q.Name}[G{q.Group}/S{q.StartingNumber}/N{q.Number} lvl {q.MinimumCharacterLevel}-{q.MaximumCharacterLevel}]")));

        int Write()
        {
            var size = AvailableQuestsRef.GetRequiredSize(questCount);
            var span = connection.Output.GetSpan(size)[..size];
            var packet = new AvailableQuestsRef(span)
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
            }

            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}