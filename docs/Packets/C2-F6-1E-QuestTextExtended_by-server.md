# C2 F6 1E - QuestTextExtended (by server)

## Is sent when

After the game client selected or started a quest step, so the open-source client can show title and summary without BMD texts.

## Causes the following actions on the client side

The client shows the quest title and summary for the selected step.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |   201   | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xF6  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x1E  | Packet header - sub packet type identifier |
| 5 | 2 | ShortLittleEndian |  | QuestNumber |
| 7 | 2 | ShortLittleEndian |  | QuestGroup |
| 9 | 64 | String |  | Name |
| 73 | 128 | String |  | Summary |