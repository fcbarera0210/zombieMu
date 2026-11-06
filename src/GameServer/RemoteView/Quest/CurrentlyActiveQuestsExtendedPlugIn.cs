// <copyright file="CurrentlyActiveQuestsExtendedPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Quest;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views.Quest;
using MUnique.OpenMU.GameServer.MessageHandler.Quests;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The extended implementation of the <see cref="ICurrentlyActiveQuestsPlugIn"/> which includes quest display names.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.CurrentlyActiveQuestsExtendedPlugIn_Name), Description = nameof(PlugInResources.CurrentlyActiveQuestsExtendedPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("B1D9E4A7-3C58-4F2E-8B06-7A9C1D4E5F28")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public class CurrentlyActiveQuestsExtendedPlugIn : ICurrentlyActiveQuestsPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrentlyActiveQuestsExtendedPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public CurrentlyActiveQuestsExtendedPlugIn(RemotePlayer player)
    {
        this._player = player;
    }

    /// <inheritdoc />
    public async ValueTask ShowActiveQuestsAsync()
    {
        var connection = this._player.Connection;
        if (connection is null || this._player.SelectedCharacter is not { } character)
        {
            return;
        }

        int Write()
        {
            const int maxQuestsPerPacket = 62;
            var activeQuests = character.QuestStates
                .Where(state => state.Group != QuestConstants.LegacyQuestGroup && state.ActiveQuest != null)
                .Select(s => s.ActiveQuest!)
                .Take(maxQuestsPerPacket)
                .ToList();
            var size = QuestStateListExtendedRef.GetRequiredSize(activeQuests.Count);
            var span = connection.Output.GetSpan(size)[..size];
            var message = new QuestStateListExtendedRef(span);
            byte i = 0;
            foreach (var activeQuest in activeQuests)
            {
                var questIdentification = message[i];
                questIdentification.Number = (ushort)activeQuest.Number;
                questIdentification.Group = (ushort)activeQuest.Group;
                questIdentification.Name = activeQuest.Name.GetTranslation(this._player.Culture)
                                           ?? activeQuest.Name.ToString()
                                           ?? string.Empty;
                i++;
            }

            message.QuestCount = i;
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}
