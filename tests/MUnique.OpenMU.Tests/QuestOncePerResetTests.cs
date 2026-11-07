// <copyright file="QuestOncePerResetTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.PlayerActions.Quests;
using MUnique.OpenMU.Persistence.BasicModel;
using NUnit.Framework;

/// <summary>
/// Tests for once-per-reset quest completion rules.
/// </summary>
[TestFixture]
public class QuestOncePerResetTests
{
    /// <summary>
    /// Once-per-character quests stay blocked after finishing when OncePerReset is off.
    /// </summary>
    [Test]
    public void OncePerCharacter_BlocksAfterFinish()
    {
        var quest = new QuestDefinition { Repeatable = false, OncePerReset = false };
        Assert.That(quest.IsBlockedByCompletionHistory(quest), Is.True);
    }

    /// <summary>
    /// Once-per-reset quests are blocked after finishing until history is cleared.
    /// </summary>
    [Test]
    public void OncePerReset_BlocksAfterFinish_UntilCleared()
    {
        var quest = new QuestDefinition { Repeatable = false, OncePerReset = true };
        var character = new Character();
        var state = new CharacterQuestState { Group = 200, LastFinishedQuest = quest };
        character.QuestStates.Add(state);

        Assert.That(quest.IsBlockedByCompletionHistory(state.LastFinishedQuest), Is.True);

        character.ClearOncePerResetQuestHistory();

        Assert.That(state.LastFinishedQuest, Is.Null);
        Assert.That(quest.IsBlockedByCompletionHistory(state.LastFinishedQuest), Is.False);
    }

    /// <summary>
    /// Fully repeatable quests are never blocked by finish history.
    /// </summary>
    [Test]
    public void Repeatable_WithoutOncePerReset_IsNeverBlocked()
    {
        var quest = new QuestDefinition { Repeatable = true, OncePerReset = false };
        Assert.That(quest.IsBlockedByCompletionHistory(quest), Is.False);
    }

    /// <summary>
    /// Once-per-character finish history is not cleared on reset.
    /// </summary>
    [Test]
    public void ClearOncePerReset_LeavesOncePerCharacterHistory()
    {
        var oncePerCharacter = new QuestDefinition { Repeatable = false, OncePerReset = false };
        var oncePerReset = new QuestDefinition { Repeatable = false, OncePerReset = true };
        var character = new Character();
        var permanentState = new CharacterQuestState { Group = 201, LastFinishedQuest = oncePerCharacter };
        var resetState = new CharacterQuestState { Group = 202, LastFinishedQuest = oncePerReset };
        character.QuestStates.Add(permanentState);
        character.QuestStates.Add(resetState);

        character.ClearOncePerResetQuestHistory();

        Assert.That(permanentState.LastFinishedQuest, Is.SameAs(oncePerCharacter));
        Assert.That(resetState.LastFinishedQuest, Is.Null);
    }

    /// <summary>
    /// Characters without quest state collections (e.g. reset unit tests) must not throw.
    /// </summary>
    [Test]
    public void ClearOncePerReset_WithNullQuestStates_DoesNotThrow()
    {
        var character = new DataModel.Entities.Character();
        Assert.That(character.QuestStates, Is.Null);
        Assert.DoesNotThrow(() => character.ClearOncePerResetQuestHistory());
    }
}
