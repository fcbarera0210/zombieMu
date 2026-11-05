# C2 F5 0A - SkillTooltipDescriptionExtended (by server)

## Is sent when

After the client requested tooltip descriptions. One message is sent for each skill which has a non-empty description.

## Causes the following actions on the client side

The client stores the description and shows it in the skill tooltip when the player hovers that skill.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |   263   | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xF5  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x0A  | Packet header - sub packet type identifier |
| 5 | 2 | ShortLittleEndian |  | Index; The index of this skill within the list, starting at 0. |
| 7 | 2 | ShortLittleEndian |  | Count; The total number of skill descriptions which will be sent, so that the client knows when it received all of them. |
| 9 | 2 | ShortLittleEndian |  | SkillNumber; The skill number which the client uses to identify the skill. |
| 11 | 252 | String |  | Description; The description of the skill, in the language of the player. |