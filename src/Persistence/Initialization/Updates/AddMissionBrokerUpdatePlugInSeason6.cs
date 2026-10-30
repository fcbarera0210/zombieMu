// <copyright file="AddMissionBrokerUpdatePlugInSeason6.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.MissionBroker;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Maps;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sets up the Mission Broker NPC (Fireworks Girl model 379), spawns it in Lorencia,
/// configures the talk plugin, and adds a sample kill mission.
/// </summary>
/// <remarks>
/// Version 1 used monster id 372, which is the Elite Skeleton transformation skin and does not
/// render as a clickable NPC. Version 2 switches to 379.
/// </remarks>
[PlugIn]
[Display(
    Name = nameof(PlugInResources.AddMissionBrokerUpdatePlugInSeason6_Name),
    Description = nameof(PlugInResources.AddMissionBrokerUpdatePlugInSeason6_Description),
    ResourceType = typeof(PlugInResources))]
[Guid("E4A91C7B-2F58-4D03-9E6A-1B8C0D5F2743")]
public class AddMissionBrokerUpdatePlugInSeason6 : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Add Mission Broker NPC";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Sets up Fireworks Girl (379) as the Mission Broker for custom quests, spawns her in Lorencia, configures the talk plugin, and adds a sample kill mission. Replaces the unusable Elite Skeleton model (372).";

    private const short LegacyInvisibleNpcNumber = 372;
    private const short LorenciaSpawnNumber = 28;
    private const short SampleQuestNumber = 1;
    private const short SpiderMonsterNumber = 3;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override int Version => 3;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override DateTime UpdatedAt => new(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var npc = gameConfiguration.Monsters.FirstOrDefault(m => m.Number == MissionBrokerConstants.NpcNumber);
        if (npc is null)
        {
            return ValueTask.CompletedTask;
        }

        npc.Designation = LocalizedString.FromResource(() => MonsterNames.FireworksGirl);
        npc.NpcWindow = NpcWindow.Undefined;
        npc.ObjectKind = NpcObjectKind.PassiveNpc;
        npc.MerchantStore = null;

        MigrateQuestsFromLegacyNpc(gameConfiguration, npc);
        EnsureLorenciaSpawn(context, gameConfiguration, npc);
        EnsureSampleQuest(context, gameConfiguration, npc);
        EnsurePlugInConfiguration(context, gameConfiguration, npc);

        return ValueTask.CompletedTask;
    }

    private static void MigrateQuestsFromLegacyNpc(GameConfiguration gameConfiguration, MonsterDefinition npc)
    {
        var legacyNpc = gameConfiguration.Monsters.FirstOrDefault(m => m.Number == LegacyInvisibleNpcNumber);
        if (legacyNpc is null)
        {
            return;
        }

        foreach (var quest in legacyNpc.Quests.ToList())
        {
            legacyNpc.Quests.Remove(quest);
            if (!npc.Quests.Any(q => q.Group == quest.Group && q.Number == quest.Number))
            {
                npc.Quests.Add(quest);
            }

            quest.QuestGiver = npc;
        }
    }

    private static void EnsureLorenciaSpawn(IContext context, GameConfiguration gameConfiguration, MonsterDefinition npc)
    {
        var lorencia = gameConfiguration.Maps.FirstOrDefault(m => m.Number == Lorencia.Number && m.Discriminator == 0);
        if (lorencia is null)
        {
            return;
        }

        var spawnId = GuidHelper.CreateGuid<MonsterSpawnArea>(Lorencia.Number, LorenciaSpawnNumber);

        // Prefer reusing the v1 spawn (same Id) or any leftover 372 spawn — never remove+recreate
        // with the same key, or EF throws an identity conflict while both instances are tracked.
        var spawn = lorencia.MonsterSpawns.FirstOrDefault(s => s.GetId() == spawnId)
                    ?? lorencia.MonsterSpawns.FirstOrDefault(s => s.MonsterDefinition?.Number == LegacyInvisibleNpcNumber)
                    ?? lorencia.MonsterSpawns.FirstOrDefault(s =>
                        s.MonsterDefinition?.Number == MissionBrokerConstants.NpcNumber
                        && s.X1 == 145 && s.Y1 == 125);

        if (spawn is null)
        {
            spawn = context.CreateNew<MonsterSpawnArea>();
            lorencia.MonsterSpawns.Add(spawn);
            spawn.SetGuid(Lorencia.Number, LorenciaSpawnNumber);
            spawn.GameMap = lorencia;
        }

        spawn.MonsterDefinition = npc;
        spawn.Quantity = 1;
        spawn.SpawnTrigger = SpawnTrigger.Automatic;
        spawn.Direction = Direction.SouthEast;
        spawn.X1 = 145;
        spawn.X2 = 145;
        spawn.Y1 = 125;
        spawn.Y2 = 125;
    }

    private static void EnsureSampleQuest(IContext context, GameConfiguration gameConfiguration, MonsterDefinition npc)
    {
        if (npc.Quests.Any(q => q.Group == MissionBrokerConstants.QuestGroup && q.Number == SampleQuestNumber))
        {
            return;
        }

        var quest = context.CreateNew<QuestDefinition>();
        quest.SetGuid(MissionBrokerConstants.QuestGroup, SampleQuestNumber);
        quest.QuestGiver = npc;
        quest.Name = LocalizedString.FromResource(() => QuestNames.PestControl);
        quest.Group = MissionBrokerConstants.QuestGroup;
        quest.Number = SampleQuestNumber;
        quest.StartingNumber = SampleQuestNumber;
        quest.RefuseNumber = 0;
        quest.MinimumCharacterLevel = 1;
        quest.MaximumCharacterLevel = 50;
        quest.Repeatable = true;
        npc.Quests.Add(quest);

        quest.WithMonsterKillRequirement(10, SpiderMonsterNumber, context, gameConfiguration);
        quest.WithMoneyReward(10000, context);
        quest.WithExperienceReward(5000, context);
    }

    private static void EnsurePlugInConfiguration(IContext context, GameConfiguration gameConfiguration, MonsterDefinition npc)
    {
        var plugInType = typeof(MissionBrokerNpcPlugIn);
        if (gameConfiguration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == plugInType.GUID) is not { } plugInConfiguration)
        {
            plugInConfiguration = context.CreateNew<PlugInConfiguration>();
            plugInConfiguration.SetGuid(plugInType.GUID);
            plugInConfiguration.TypeId = plugInType.GUID;
            gameConfiguration.PlugInConfigurations.Add(plugInConfiguration);
        }

        plugInConfiguration.IsActive = true;
        plugInConfiguration.SetConfiguration(
            new NpcTalkPlugInConfiguration { Npc = npc },
            new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler());
    }
}
