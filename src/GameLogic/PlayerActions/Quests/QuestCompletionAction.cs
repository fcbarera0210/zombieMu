// <copyright file="QuestCompletionAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Quests;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.GameLogic.Views.Quest;
using MUnique.OpenMU.Persistence;

/// <summary>
/// A player action which implements the completion of a quest.
/// </summary>
public class QuestCompletionAction
{
    /// <summary>
    /// Tries to completes the quest of the given group and number for the specified player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="group">The group.</param>
    /// <param name="number">The number.</param>
    public async ValueTask CompleteQuestAsync(Player player, short group, short number)
    {
        using var loggerScope = player.Logger.BeginScope(this.GetType());
        var questState = player.GetQuestState(group, number);
        var activeQuest = questState?.ActiveQuest;
        if (activeQuest is null)
        {
            player.Logger.LogDebug("Failed, no active quest");
            return;
        }

        foreach (var requiredItem in activeQuest.RequiredItems)
        {
            var requiredLevel = requiredItem.DropItemGroup?.ItemLevel;
            var itemCount = player.Inventory?.Items
                .Count(i => Equals(i.Definition, requiredItem.Item)
                            && (requiredLevel is null || requiredLevel == i.Level));

            if (requiredItem.MinimumNumber <= itemCount)
            {
                continue;
            }

            player.Logger.LogDebug("Failed, required item not found: {0}", requiredItem.Item!.GetNameForLevel(requiredLevel ?? 0));
            return;
        }

        foreach (var requiredKills in activeQuest.RequiredMonsterKills)
        {
            var currentKillCount = questState!.RequirementStates.FirstOrDefault(r => object.Equals(r.Requirement, requiredKills))?.KillCount ?? 0;
            if (currentKillCount >= requiredKills.MinimumNumber)
            {
                continue;
            }

            player.Logger.LogDebug("Failed, required kills of monster {0}: {1}/{2};", requiredKills.Monster?.Designation, currentKillCount, requiredKills.MinimumNumber);
            return;
        }

        if (activeQuest.RequiresClientAction && !questState!.ClientActionPerformed)
        {
            player.Logger.LogDebug("Failed, client action not performed.");
            return;
        }

        if (activeQuest.EvolvesIntoInactiveClass(player))
        {
            player.Logger.LogDebug("Failed, the quest evolves into the inactive class {0}.", player.SelectedCharacter?.CharacterClass?.NextGenerationClass);
            return;
        }

        questState!.LastFinishedQuest = activeQuest;
        foreach (var requiredItem in activeQuest.RequiredItems)
        {
            var requiredLevel = requiredItem.DropItemGroup?.ItemLevel;
            var items = player.Inventory!.Items
                .Where(item => Equals(item.Definition, requiredItem.Item)
                               && (requiredLevel is null || requiredLevel == item.Level))
                .Take(requiredItem.MinimumNumber)
                .ToList();

            foreach (var item in items)
            {
                await player.DestroyInventoryItemAsync(item).ConfigureAwait(false);
            }
        }

        foreach (var reward in activeQuest.Rewards)
        {
            try
            {
                await AddRewardAsync(player, reward, activeQuest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // One broken reward (e.g. Item without Definition) must not cancel the rest.
                player.Logger.LogError(ex, "Failed to apply quest reward {Reward} of quest {Quest} for player {Player}.", reward, activeQuest, player);
            }
        }

        await questState.ClearAsync(player.PersistenceContext).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IQuestCompletionResponsePlugIn>(p => p.QuestCompletedAsync(activeQuest)).ConfigureAwait(false);
    }

    private static async ValueTask AddRewardAsync(Player player, QuestReward reward, QuestDefinition quest)
    {
        switch (reward.RewardType)
        {
            case QuestRewardType.Attribute:
                var attribute = player.SelectedCharacter!.Attributes.FirstOrDefault(a => object.Equals(a.Definition, reward.AttributeReward));
                if (attribute is null)
                {
                    attribute = player.PersistenceContext.CreateNew<StatAttribute>(reward.AttributeReward, 0);
                    player.SelectedCharacter.Attributes.Add(attribute);
                }

                attribute.Value += reward.Value;

                // Compensate level-up points when doing a quest at a later level.
                if (attribute.Definition == Stats.PointsPerLevelUp)
                {
                    var playerLevel = (int)player.Attributes![Stats.Level];
                    player.SelectedCharacter.LevelUpPoints += (playerLevel - quest.MinimumCharacterLevel) * reward.Value;
                }

                await player.InvokeViewPlugInAsync<ILegacyQuestRewardPlugIn>(p => p.ShowAsync(player, QuestRewardType.Attribute, reward.Value, attribute.Definition)).ConfigureAwait(false);
                break;
            case QuestRewardType.Item:
                if (reward.ItemReward?.Definition is null)
                {
                    player.Logger.LogWarning(
                        "Quest reward of quest {Quest} is Item but has no ItemReward.Definition. Skipping. (Changing ItemSlot alone does not select an item — set Definition.)",
                        quest);
                    await player.ShowBlueMessageAsync("[Quest] A configured item reward has no item Definition and was skipped.").ConfigureAwait(false);
                    break;
                }

                var itemTemplate = reward.ItemReward;

                // QuestReward.Value is how often the item template is awarded (usually 1).
                // Piece count for stackables/jewels lives on Item.Durability of the template.
                var itemCount = Math.Max(1, reward.Value);
                for (var i = 0; i < itemCount; i++)
                {
                    var item = player.PersistenceContext.CreateNew<Item>();
                    item.AssignValues(itemTemplate);
                    item.ItemSlot = 0;
                    NormalizeRewardItemDurability(item);

                    if (await player.Inventory!.AddItemAsync(item).ConfigureAwait(false))
                    {
                        await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
                    }
                    else if (player.CurrentMap is { } currentMap)
                    {
                        await currentMap.AddAsync(new DroppedItem(item, player.Position, currentMap, player, player.GetAsEnumerable())).ConfigureAwait(false);
                    }
                    else
                    {
                        player.Logger.LogWarning("Quest reward item {Item} of player {Player} is lost: the inventory is full and the player is on no map.", item, player);
                    }
                }

                break;
            case QuestRewardType.LevelUpPoints:
                player.SelectedCharacter!.LevelUpPoints += reward.Value;
                await player.InvokeViewPlugInAsync<ILegacyQuestRewardPlugIn>(p => p.ShowAsync(player, QuestRewardType.LevelUpPoints, reward.Value, null)).ConfigureAwait(false);
                break;
            case QuestRewardType.CharacterEvolutionFirstToSecond:
                player.SelectedCharacter!.CharacterClass = player.SelectedCharacter.CharacterClass?.NextGenerationClass
                                                           ?? throw new InvalidOperationException($"Current character class has no next generation");
                await player.ForEachWorldObserverAsync<ILegacyQuestRewardPlugIn>(
                    p => p.ShowAsync(
                        player,
                        QuestRewardType.CharacterEvolutionFirstToSecond,
                        reward.Value,
                        null),
                    true).ConfigureAwait(false);
                break;
            case QuestRewardType.CharacterEvolutionSecondToThird:
                player.SelectedCharacter!.CharacterClass = player.SelectedCharacter.CharacterClass?.NextGenerationClass
                                                           ?? throw new InvalidOperationException($"Current character class has no next generation");
                await player.ForEachWorldObserverAsync<ILegacyQuestRewardPlugIn>(
                    p => p.ShowAsync(
                        player,
                        QuestRewardType.CharacterEvolutionSecondToThird,
                        reward.Value,
                        null),
                    true).ConfigureAwait(false);
                await player.InvokeViewPlugInAsync<IUpdateMasterStatsPlugIn>(p => p.SendMasterStatsAsync()).ConfigureAwait(false);
                break;
            case QuestRewardType.Experience:
                await player.AddExperienceAsync(reward.Value, null).ConfigureAwait(false);
                break;
            case QuestRewardType.Money:
                player.TryAddMoney(reward.Value);
                await player.InvokeViewPlugInAsync<IUpdateMoneyPlugIn>(p => p.UpdateMoneyAsync()).ConfigureAwait(false);
                break;
            case QuestRewardType.GensAttribution:
                // not yet implemented.
                break;
            case QuestRewardType.Skill:
                if (reward.SkillReward is not { } skill)
                {
                    player.Logger.LogError("Reward has no skill defined.");
                    return;
                }

                if (player.SkillList is not { } skillList)
                {
                    player.Logger.LogError("Can't reward the skill; SkillList is null.");
                    return;
                }

                if (!skillList.ContainsSkill(skill.Number.ToUnsigned()))
                {
                    await skillList.AddLearnedSkillAsync(skill).ConfigureAwait(false);
                }
                else
                {
                    player.Logger.LogWarning($"Skill {skill} is already learned.");
                }

                break;
            default:
                player.Logger.LogWarning("Unknown reward type: {0}", reward.RewardType);
                break;
        }
    }

    /// <summary>
    /// Normalizes durability/piece count on a newly created reward item.
    /// Wearables get a full durability bar; non-wearables use Durability as piece count,
    /// capped to <see cref="ItemDefinition.Durability"/> (1 for jewels, higher for potions).
    /// Admin templates sometimes keep an arbitrary Durability (e.g. 10) that must not be awarded as-is.
    /// </summary>
    private static void NormalizeRewardItemDurability(Item item)
    {
        if (item.Definition is null)
        {
            return;
        }

        if (item.IsWearable())
        {
            if (item.Durability < 1)
            {
                item.Durability = item.GetMaximumDurabilityOfOnePiece();
            }

            return;
        }

        var maxPieces = Math.Max(1, (int)item.Definition.Durability);
        if (item.Durability < 1)
        {
            item.Durability = 1;
        }
        else if (item.Durability > maxPieces)
        {
            item.Durability = maxPieces;
        }
    }
}