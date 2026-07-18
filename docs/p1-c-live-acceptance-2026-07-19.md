# P1-C Live Server Acceptance

Date: 2026-07-19

## Result

The single-player, read-only, and reversible subset of P1-C passed on the
physical Android device against `192.168.0.100:17000`, using character
`翻云覆雨` (level 100).

The previously empty Quest, Social, Mail, and Market panels now have complete
runtime-generated controls and serialized bindings. Quest tracking, group
permission switching, mail/market empty-state gating, market search, restart
consistency, and UI-touch isolation passed. All temporary reversible state was
restored to its recorded baseline.

P1-C is not marked fully complete because group/guild/trade end-to-end flows
require a second test character, while mail and market mutation cases require
disposable mail, items, and gold. No guild creation, trade confirmation, mail
send/delete/collect, market purchase, or consignment was performed.

## UI Assembly And Validation

- Added complete generated UI assembly for Quests, Social, Mail, and Market.
- Added scene validation for every required P1-C control and serialized field.
- Unity scene generation and command-line validation both completed with exit
  code 0.
- Added conservative button gating so operations are unavailable until their
  required selection, text, pending request, open trade, item, count, or price
  state exists.

## Quest Validation

- The server returned five real quests, including `补给比奇县` in the tracked
  baseline state.
- Untracking `补给比奇县` immediately changed the action from `Untrack` to
  `Track` and emitted one `QuestTrack` command.
- Tracking it again immediately restored `Untrack` and emitted one command.
- A short per-quest pending state and cooldown now provide immediate feedback
  and prevent repeated taps from sending duplicate commands while awaiting the
  server snapshot.
- After a force-stop, cold start, login, and reopening Quests, `补给比奇县`
  remained tracked. StartGame also restored `items=26`, `skills=22`, and
  `buffs=3` at `(161,232)`.

Evidence:

- `Builds/Android/DeviceQA/p1c_final_quest_baseline_2026-07-19.png`
- `Builds/Android/DeviceQA/p1c_final_quest_feedback_2026-07-19.png`
- `Builds/Android/DeviceQA/p1c_final_relogin_quest_2026-07-19.png`

## UI Touch Isolation

The first quest test exposed a mobile-only input defect: a touch beginning on
the Quest UI could also reach world targeting and open a nearby bookstore NPC.
World touch handling now records whether a touch began over the EventSystem UI
and ignores that whole touch sequence. The final quest tracking tests produced
no NPC interaction.

## Group, Guild, And Trade

- Enabled the group-permission switch. The server received group switch packet
  2081 with `allow=true`.
- Disabled it again. The server received `allow=false`, restoring the baseline.
- Group invite requires a player name; accept/decline require a pending invite.
- Trade accept/decline require a pending request. Item, gold, confirm, and close
  actions require an open trade.
- Guild creation requires a guild name, guild invitation requires a player
  name, notice update requires text, and accept/decline require a pending guild
  invite.
- The final Social screenshot confirmed unavailable or potentially destructive
  actions are greyed when their prerequisites are absent.

Evidence:

- `Builds/Android/DeviceQA/p1c_group_allowed_2026-07-18.png`
- `Builds/Android/DeviceQA/p1c_final_social_gating_2026-07-19.png`

## Mail

- The live mailbox was empty.
- Collect and Delete remained unavailable without a selected mail.
- Send is now unavailable until both recipient and subject are present, and the
  command handler repeats the same guard.
- No mail was sent, collected, or deleted.

Evidence:

- `Builds/Android/DeviceQA/p1c_mail_fixed_2026-07-18.png`

## Market

- An empty market search reached server MarketSearch packet 2098 and returned
  zero results without error.
- Buy and Cancel remained unavailable without a selected result or listing.
- Submit Consign now requires a valid occupied inventory slot, a positive count
  not exceeding the stack count, and a positive price. The command handler
  repeats the same guard.
- No purchase, consignment, or listing cancellation was performed.

Evidence:

- `Builds/Android/DeviceQA/p1c_market_fixed_2026-07-18.png`
- `Builds/Android/DeviceQA/p1c_market_search_2026-07-19.png`

## Regression, APK, And Device Stability

- Protocol/world regression tests: 14/14 passed.
- Offline Unity UI compile check: passed with 0 warnings and 0 errors.
- Final ARM64 IL2CPP APK build: exit code 0.
- Final APK size: 75,021,647 bytes.
- Final APK SHA-256:
  `092941968F8AAA241955FE2BA0EA7B91FEE015EB2CFDA462D650570B47D2B881`.
- The APK was installed with replacement mode on the authorized Xiaomi Android
  device. The final cold-start process remained alive with no recorded managed
  exception, native crash, ANR, or fatal error.

## Deferred P1-C Cases

The following cases remain deferred rather than failed:

- Group invite/accept/decline/leave: requires a second online test character.
- Guild invite/accept/decline and membership/notice workflows: requires a test
  guild or explicit permission to create one and another test character.
- Trade request/accept/item/gold/confirm/cancel: requires a second character and
  disposable items/gold; no trade was confirmed.
- Mail open/collect/delete: requires existing disposable mail.
- Mail send: changes another character's mailbox and requires an approved test
  recipient.
- Market buy/consign/cancel: requires disposable gold/items and an approved
  economy mutation scope.

These cases can be closed later with a second disposable account plus prepared
mail, items, gold, and explicit authorization for the external mutations.

