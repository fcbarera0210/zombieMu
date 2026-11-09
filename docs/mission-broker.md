# Mission Broker

The Mission Broker (monster **379**, Fireworks Girl model) offers custom quests created
in the admin panel. Players talk to the NPC and use the normal F6 quest UI — the same
windows as other Season 6 quests.

## Player flow

1. Talk to the Mission Broker → NPC dialog opens.
2. Choose **View missions** → list of available quests.
3. Select a quest → accept / refuse dialog.
4. Active missions appear in the player quest window (F) for tracking and give-up.
5. When requirements are met, open the quest (or talk to the NPC again) and complete it for rewards.

Level min/max only applies when **accepting** a mission. Once taken, it stays selectable at the
NPC and can be completed regardless of current level, as long as kill/item requirements are met.

`/missions` still lists active Mission Broker progress in chat as a convenience.

## Admin: creating missions

Edit **Monsters → 379 (Fireworks Girl) → Quests**.

Rules for each mission:

| Field | Rule |
|-------|------|
| **Group** | Unique value in **200–299**. Each active mission needs its own group (native F6 allows one active quest per group). Up to **5** Mission Broker missions can be active at once. |
| **StartingNumber** | Dialog id before accept. Must be **different** from `Number`. |
| **Number** | Progress / completion id after accept. |
| **RefuseNumber** | Optional refuse dialog id (often `0`). |
| **Name** | Shown in the open-source client via Extended packets (no BMD required). |
| **QuestGiver** | Keep linked to monster 379. |
| **Repeatable** | `false` for one-time missions (e.g. first wings). Completed ones are hidden from the NPC list. |
| **OncePerReset** | Optional: can be taken again only after character reset. |

Example (sample *Pest Control*): Group `200`, StartingNumber `1`, Number `2`.

Do **not** put multiple Mission Broker missions in the same Group if you want them to run in parallel.

If a click on a listed mission does nothing, check server logs / in-game golden messages: already completed, wrong level, `StartingNumber == Number`, or class filter. Each mission needs its **own Group** in 200–299.

## Technical notes

- NPC window type is `NpcDialog` (not the legacy chat menu).
- OpenMU sends Extended quest packets (`F6/1C`, `F6/1D`, `F6/1E`) with display names for MuMain.
- `F6/1E` Summary is built from kill/item requirements (there is no free-text description on `QuestDefinition`). Live counts still come from the progress packet (`F6/0C` Extended).
- After accept the server tracks by `Number`. Re-sending Accept/`QuestStepInfo` for that Number must not reopen Accept/Refuse on the client.
- If a quest has `IsChoiceOption` rewards, completing opens a chat prompt (`1`, `2`, …) before rewards are granted.
- The old chat accept / `ABANDON` talk plugin is inactive; prefer the quest window give-up button.
