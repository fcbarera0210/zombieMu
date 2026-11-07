// <copyright file="QuestRewardChoiceChatPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlayerActions.Quests;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handles chat replies when a quest completion requires picking one choice reward.
/// </summary>
[Guid("E3A6F1B2-8C47-4D9E-A1B0-5F6D7E8C9A01")]
[PlugIn]
[Display(
    Name = nameof(PlugInResources.QuestRewardChoiceChatPlugIn_Name),
    Description = nameof(PlugInResources.QuestRewardChoiceChatPlugIn_Description),
    ResourceType = typeof(PlugInResources))]
public class QuestRewardChoiceChatPlugIn : IChatMessageReceivedPlugIn
{
    private readonly QuestCompletionAction _completionAction = new();

    /// <inheritdoc />
    public void ChatMessageReceived(Player sender, string message, CancelEventArgs cancelEventArgs)
    {
        if (sender.SelectedCharacter is null
            || !QuestRewardChoiceState.TryGet(sender.SelectedCharacter.Id, out var pending))
        {
            return;
        }

        var trimmed = message.Trim();
        if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
        {
            return;
        }

        cancelEventArgs.Cancel = true;
        if (index < 1 || index > pending.Choices.Count)
        {
            _ = sender.ShowGoldenMessageAsync(
                $"[Quest] Invalid reward. Type a number from 1 to {pending.Choices.Count}.");
            return;
        }

        var selected = pending.Choices[index - 1];
        QuestRewardChoiceState.Clear(sender.SelectedCharacter.Id);
        _ = this.CompleteWithChoiceAsync(sender, pending.Quest.Group, pending.Quest.Number, selected);
    }

    private async Task CompleteWithChoiceAsync(Player player, short group, short number, DataModel.Configuration.Quests.QuestReward selected)
    {
        var completed = await this._completionAction
            .CompleteQuestAsync(player, group, number, selected)
            .ConfigureAwait(false);
        if (completed)
        {
            await player.ShowGoldenMessageAsync($"[Quest] Reward selected: {selected}").ConfigureAwait(false);
        }
        else
        {
            await player.ShowGoldenMessageAsync("[Quest] Could not complete with that reward. Try again.").ConfigureAwait(false);
        }
    }
}
