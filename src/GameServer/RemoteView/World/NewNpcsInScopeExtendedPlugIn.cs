// <copyright file="NewNpcsInScopeExtendedPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.World;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.Views.World;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The extended implementation of the <see cref="INewNpcsInScopePlugIn"/> which additionally sends
/// effective instance level and absolute health for attackable NPCs.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.NewNpcsInScopeExtendedPlugIn_Name), Description = nameof(PlugInResources.NewNpcsInScopeExtendedPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("7C3E8A91-4B2D-4F6A-9E1C-5D8A0B7F3C24")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public class NewNpcsInScopeExtendedPlugIn : NewNpcsInScopePlugIn, INewNpcsInScopePlugIn
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NewNpcsInScopeExtendedPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public NewNpcsInScopeExtendedPlugIn(RemotePlayer player)
        : base(player)
    {
    }

    /// <inheritdoc />
    protected override int WriteNpcsInScope(
        bool isSpawned,
        IConnection connection,
        ICollection<NonPlayerCharacter> npcs)
    {
        var size = AddNpcsToScopeExtendedRef.GetRequiredSize(npcs.Count);
        var span = connection.Output.GetSpan(size)[..size];
        var packet = new AddNpcsToScopeExtendedRef(span)
        {
            NpcCount = (byte)npcs.Count,
        };

        int i = 0;
        foreach (var npc in npcs)
        {
            var npcBlock = packet[i];
            npcBlock.Id = npc.Id;
            if (isSpawned)
            {
                npcBlock.Id |= 0x8000;
            }

            npcBlock.TypeNumber = GetNpcTypeNumber(npc, this.Player);
            npcBlock.CurrentPositionX = npc.Position.X;
            npcBlock.CurrentPositionY = npc.Position.Y;

            if (npc is ISupportWalk { IsWalking: true } supportWalk)
            {
                npcBlock.TargetPositionX = supportWalk.WalkTarget.X;
                npcBlock.TargetPositionY = supportWalk.WalkTarget.Y;
            }
            else
            {
                npcBlock.TargetPositionX = npc.Position.X;
                npcBlock.TargetPositionY = npc.Position.Y;
            }

            npcBlock.Rotation = npc.Rotation.ToPacketByte();
            FillCombatStats(npc, npcBlock);

            i++;
        }

        return size;
    }

    private static void FillCombatStats(NonPlayerCharacter npc, AddNpcsToScopeExtendedRef.NpcDataRef npcBlock)
    {
        // Only attackable instances expose meaningful combat stats for the enemy panel.
        // Merchants, traps and other passive NPCs stay at 0; the client ignores them anyway.
        if (npc is not AttackableNpcBase attackable)
        {
            npcBlock.Level = 0;
            npcBlock.CurrentHealth = 0;
            npcBlock.MaximumHealth = 0;
            return;
        }

        npcBlock.Level = (ushort)Math.Clamp(attackable.Attributes[Stats.Level], 0, ushort.MaxValue);
        npcBlock.CurrentHealth = (uint)Math.Max(0, attackable.Health);
        npcBlock.MaximumHealth = (uint)Math.Max(0, attackable.Attributes[Stats.MaximumHealth]);
    }
}
