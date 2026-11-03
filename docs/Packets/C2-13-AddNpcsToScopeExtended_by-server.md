# C2 13 - AddNpcsToScopeExtended (by server)

## Is sent when

One or more NPCs got into the observed scope of the player. Extended variant for custom clients that need instance level and absolute health without waiting for a hit.

## Causes the following actions on the client side

The client adds the NPCs to the shown map and can show level and health on the enemy panel immediately.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0x13  | Packet header - packet type identifier |
| 4 | 1 | Byte |  | NpcCount |
| 5 | NpcData.Length * NpcCount | Array of NpcData |  | NPCs |

### NpcData Structure

Contains the data of an NPC, including effective instance level and absolute health. The first 10 bytes match the Season 6 NpcData layout; Level, CurrentHealth and MaximumHealth are appended. For non-attackable NPCs these three fields are 0.

Length: 20 Bytes

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 2 | ShortBigEndian |  | Id |
| 2 | 2 | ShortBigEndian |  | TypeNumber |
| 4 | 1 | Byte |  | CurrentPositionX |
| 5 | 1 | Byte |  | CurrentPositionY |
| 6 | 1 | Byte |  | TargetPositionX |
| 7 | 1 | Byte |  | TargetPositionY |
| 8 | 4 bit | Byte |  | Rotation |
| 9 | 1 | Byte |  | EffectCount; Defines the number of effects which would be sent after this field. This is currently not supported. |
| 10 | 2 | ShortLittleEndian |  | Level; Effective combat level of this NPC instance (includes multipliers / event scaling). 0 when not applicable. |
| 12 | 4 | IntegerLittleEndian |  | CurrentHealth; Current absolute health of this NPC instance. 0 when not applicable. |
| 16 | 4 | IntegerLittleEndian |  | MaximumHealth; Maximum absolute health of this NPC instance. 0 when not applicable. |