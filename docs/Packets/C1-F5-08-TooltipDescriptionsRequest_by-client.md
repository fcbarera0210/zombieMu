# C1 F5 08 - TooltipDescriptionsRequest (by client)

## Is sent when

A client which supports server-driven tooltip descriptions requests them. It's usually sent after the character entered the game world.

## Causes the following actions on the server side

The server sends an ItemTooltipDescriptionExtended message for each item which has a description, and a SkillTooltipDescriptionExtended message for each skill which has a description.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC1  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |   4   | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0xF5  | Packet header - packet type identifier |
| 3 | 1 |    Byte   | 0x08  | Packet header - sub packet type identifier |