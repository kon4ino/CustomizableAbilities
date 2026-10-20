# CustomizableAbilities — ino_ (kon4ino)

Mod that allows you to change the abilities: positive, negative and fractional nail damage; nail cooldown; amount of SOUL gained from nail hits; dream nail damage and SOUL gain; spells damage; crystal dash damage; focus cost and focus time.
Negative damage heals enemies.

All settings are changed in an in-game **settings panel**, and the current values can be shown on a **display** while you play.


## Quick start

1. Pause the game (Esc).
2. Press **F3** to open the settings panel.
3. Change what you want — changes apply immediately.
4. Press **F5** to show/hide window, where displays ability values.
5. Close the panel with **F3**, the **CLOSE** button, or just by unpausing the game.


## Key binds

| Action | Default |
|---|---|
| Open/close the settings panel (only while paused) | F3 |
| +1 nail damage | not bound |
| -1 nail damage | not bound |
| Show/hide the display | F5 |

Key binds can be changed in the **Keys** tab of the settings panel or in the mod menu (Options → Mods → CustomizableAbilities).


## Settings panel

Opens only while the game is paused, at the bottom of the screen (so it doesn't overlap DebugMod).

- **ON/OFF** buttons toggle options.
- Numbers are changed with the **−** / **+** buttons or typed into the field (press Enter or click outside the field to apply; both `.` and `,` work as the decimal separator).
- Every tab has a button to reset its settings to defaults.

### General
- Enable/disable the mod
- Heal enemies beyond their max health (for negative damage)
- Show/hide the display
- Reset display position
- Reset global settings to defaults (also resets key binds)

### Focus
- Show focus cost / focus time on the display
- **Focus cost** — how much SOUL one focus (one healed mask) costs: 0–999, vanilla is 33
- **Focus time multiplier** — makes focus faster or slower: 0.01–99, vanilla is 1

How focus settings work:
- Focus duration does not depend on the cost. A 66 SOUL focus drains SOUL twice as fast as a 33 SOUL one, but takes the same time. The time multiplier stretches or shortens the whole focus. The display shows the resulting focus time in seconds.
- You can only start focusing if you have enough SOUL for the full cost. SOUL from reserve vessels counts too: when the main vessel runs out, the rest is taken from the reserve vessels. The maximum payable cost is 198 (99 in the main vessel + 99 in reserve vessels); anything higher makes focus unavailable.
- Cost 0 makes focus free: you can start it with any amount of SOUL, no SOUL is spent, and you keep healing as long as you hold the button (until your masks are full).
- With cost 33 and multiplier 1 the mod does not touch focus at all.
- Focus settings are not written to your save file: without the mod, focus works as in vanilla.

### Nail
- Show nail damage / SOUL gain / cooldown on the display
- **Ignore Nail Binding** — the nail binding in Godhome won't limit your custom damage
- **Enable Float Damage** — switch between whole damage (positive or negative) and fractional damage
- **Vanilla Nail Damage** — quick presets: Old, Sharpened, Channelled, Coiled, Pure and their negative versions (whole damage mode only)
- **Nail Damage** — any whole damage from -9999 to 9999; negative damage heals enemies
- **Float Nail Damage** — fractional damage from 0.01 to 0.99: it adds up on each enemy separately and deals 1 damage every time it reaches a whole number
- **Main Vessel Soul Gain** — SOUL per nail hit into the main vessel: 0–198, vanilla is 11
- **Reserve Vessel Soul Gain** — SOUL per nail hit when the main vessel is full: 0–198, vanilla is 6
- **Nail Cooldown** — time between nail swings in seconds, vanilla is 0.41

### Dream Nail
- Show dream nail SOUL gain / damage on the display
- **Dream Nail Soul Gain** — SOUL per dream nail hit: 0–198, vanilla is 33
- **Dream Nail Damage** — makes the dream nail deal damage: -9999 to 9999, vanilla is 0 (no damage)

How dream nail damage works:
- Damage can be fractional (e.g. 0.5): it adds up on each enemy separately and deals 1 damage every time it reaches a whole number.
- Negative damage heals enemies (beyond their max health only if this option is enabled in the General tab).
- If an enemy's health drops to 0 from a dream nail hit, the enemy dies.

### Spells
- Show spells damage on the display
- Custom damage of each spell: Vengeful Spirit (15), Desolate Dive (35), Howling Wraiths (39), Shade Soul (30), Descending Dark (60), Abyss Shriek (80) — vanilla values in brackets, range 1–9999

### Crystal Dash
- Show crystal dash damage on the display
- **Crystal Dash Damage** — -9999 to 9999, vanilla is 10. Works the same way as dream nail damage: it can be fractional (adds up on each enemy) and negative (heals enemies).

### Keys
- Click a key bind button and press a new key. **Backspace** or **Delete** removes the bind, clicking the button again cancels.
- Avoid binding **Enter** and **Space**: while paused they also press the selected pause menu button.


## Display

A panel that shows the current values while you play. Each section (Focus, Nail, Dream Nail, Spells, Crystal Dash) can be shown or hidden in its tab of the settings panel.

- Values in brackets are the real values with charms applied (for example nail damage with Fragile Strength).
- Negative damage is shown in red.
- Spells you haven't obtained yet are shown as "not obtained".
- **Move the display:** pause the game and drag the panel with the mouse. Its position is saved and stays the same on any screen resolution. Use "Reset Display Position" to move it back.


## Mod menu

Options → Mods → CustomizableAbilities contains only the basics:
- Enable/disable the mod
- Key binds (including the settings panel key — use it if you lost the bind)
- Reset defaults (global settings and key binds)


## Interactions

- **Nail:** works with everything that depends on nail damage — Nail Arts and charms: Fragile Strength, Fury of the Fallen, Sharp Shadow, Dreamshield, Thorns of Agony, Grubberfly's Elegy.
- **Spells:** works with everything that affects spells — Flukenest, Shaman Stone, Defender's Crest.
- **Dream nail:** Dream Wielder still doubles the SOUL gained from dream nail hits.
- **Focus:** Quick Focus and Deep Focus keep working on top of the time multiplier (Quick Focus is still faster, Deep Focus is still slower and heals 2 masks).


## Requirements

Modding API, Satchel, HKMirror, SFCore (installed automatically with Lumafly / Scarab).