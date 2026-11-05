# C2 F5 09 - ItemTooltipDescriptionExtended (by server)

## Is sent when

After the client requested tooltip descriptions. One message is sent for each item definition which has a non-empty description.

## Causes the following actions on the client side

The client stores the description and shows it in the item tooltip when the player hovers that item.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |   266   | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xF5  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x09  | Packet header - sub packet type identifier |
| 5 | 2 | ShortLittleEndian |  | Index; The index of this item within the list, starting at 0. |
| 7 | 2 | ShortLittleEndian |  | Count; The total number of item descriptions which will be sent, so that the client knows when it received all of them. |
| 9 | 1 | Byte |  | Group; The item group (0-15). |
| 10 | 2 | ShortLittleEndian |  | Number; The item number within the group. |
| 12 | 254 | String |  | Description; The description of the item, in the language of the player. |