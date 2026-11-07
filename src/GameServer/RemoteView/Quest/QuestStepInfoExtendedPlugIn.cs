// <copyright file="QuestStepInfoExtendedPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Quest;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlayerActions.Quests;
using MUnique.OpenMU.GameLogic.Views.Quest;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The extended implementation of the <see cref="IQuestStepInfoPlugIn"/> which sends quest title/summary
/// before the stock step-info packet so open-source clients can render text without BMD files.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.QuestStepInfoExtendedPlugIn_Name), Description = nameof(PlugInResources.QuestStepInfoExtendedPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("C4E7F0A2-5B19-4D8C-9E31-8F6A2B7C0D45")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public class QuestStepInfoExtendedPlugIn : IQuestStepInfoPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestStepInfoExtendedPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public QuestStepInfoExtendedPlugIn(RemotePlayer player)
    {
        this._player = player;
    }

    /// <inheritdoc/>
    public async ValueTask ShowQuestStepInfoAsync(short questGroup, short stepNumber)
    {
        if (this._player.Connection is null)
        {
            return;
        }

        var quest = this._player.GetQuest(questGroup, stepNumber);
        var name = QuestTextHelper.GetName(quest, this._player.Culture);
        var summary = QuestTextHelper.BuildSummary(quest, this._player.Culture);

        await this._player.Connection.SendQuestTextExtendedAsync(
            (ushort)stepNumber,
            (ushort)questGroup,
            name,
            summary).ConfigureAwait(false);

        // When the quest is already active and this is the progress Number (not StartingNumber),
        // skip stock StepInfo — it would re-open Accept/Refuse on MuMain.
        var active = this._player.GetQuestState(questGroup)?.ActiveQuest;
        if (active is not null
            && object.Equals(active, quest)
            && quest is not null
            && quest.Number == stepNumber
            && quest.StartingNumber != stepNumber)
        {
            return;
        }

        await this._player.Connection.SendQuestStepInfoAsync((ushort)stepNumber, (ushort)questGroup).ConfigureAwait(false);
    }
}
