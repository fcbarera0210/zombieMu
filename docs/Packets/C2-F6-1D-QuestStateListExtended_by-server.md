# C2 F6 1D - QuestStateListExtended (by server)

## Is sent when

After the game client requested the list of all quests which are currently in progress or accepted. Extended variant includes display names for open-source clients without BMD quest texts.

## Causes the following actions on the client side

The client shows the active quest list using the provided names.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xF6  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x1D  | Packet header - sub packet type identifier |
| 5 | 1 | Byte |  | QuestCount |
| 6 | QuestIdentificationExtended.Length * QuestCount | Array of QuestIdentificationExtended |  | Quests |

### QuestIdentificationExtended Structure

Defines the information which identifies a quest, including a display name for open-source clients without BMD quest texts.

Length: 68 Bytes

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 2 | ShortLittleEndian |  | Number |
| 2 | 2 | ShortLittleEndian |  | Group |
| 4 | 64 | String |  | Name |