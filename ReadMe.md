# CustomizableAbilities — ino_ (kon4ino)

Mod that allows you to change the abilities: nail negative and fractional damage; amount of SOUL gaining from hits; dream nail damage and SOUL gain; spells; crystal dash damage; focus cost and focus time.
Negative damage will heal enemies.

Standard binds:
U — +1 nail damage
I — -1 nail damage
O — enable/disable display of nail damage

Settings:
1. Enable/disable mod
2. Heal enemies beyond their max health Switch between whole nail modes (positive, negative) and fractional

3. Focus settings
1) Enable/disable display of focus cost
2) Enable/disable display of focus time multiplier
3) Focus cost: set how much SOUL one focus (one healed mask) costs (0-999, vanilla is 33)
4) Focus time multiplier: make focus faster or slower (0.01-10, vanilla is 1)
5) Reset focus settings

How focus settings work:
- Focus duration does not depend on the cost. A 66 SOUL focus drains SOUL twice as fast as a 33 SOUL one, but takes the same time. The time multiplier stretches or shortens the whole focus.
- You can only start focusing if you have enough SOUL for the full cost. SOUL from reserve vessels counts too: when the main vessel runs out, the rest is taken from the reserve vessels. The maximum payable cost is 198 (99 in the main vessel + 99 in reserve vessels); anything higher makes focus unavailable.
- Cost 0 makes focus free: you can start it with any amount of SOUL, no SOUL is spent, and you keep healing as long as you hold the button (until your masks are full).
- With cost 33 and multiplier 1 the mod does not touch focus at all.
- Settings are not written to your save file: without the mod, focus works as in vanilla.

4. Nail settings
1) Enable/disable display of nail damage
2) Switch between whole nail modes (positive, negative) and fractional
3) Set vanilla positive and negative nail damage
4) Set nail damage by entering number
5) Custom float nail damage: set fractional nail damage (0.01-0.99)
6) Set custom amount of soul you get by nail hit to main vessel
7) Set custom amount of soul you get by nail hit to reserve vessel
8) Reset nail settings

5. Dream Nail settings
1) Enable/disable display of dream nail SOUL gain
2) Enable/disable display of dream nail damage
3) Dream nail SOUL gain: set how much SOUL one dream nail hit gives (0-198, vanilla is 33)
4) Dream nail damage: make the dream nail deal damage (-9999 to 9999, vanilla is 0 — no damage)
5) Reset dream nail settings

How dream nail settings work:
- Damage can be fractional (e.g. 0.5): it adds up on each enemy separately and deals 1 damage every time it reaches a whole number.
- Negative damage heals enemies (beyond their max health only if this option is enabled in the main menu).
- If an enemy's health drops to 0 from a dream nail hit, the enemy dies.

6. Spells settings
1) Set custom damage of each spell (Vengeful Spirit, Desolate Dive, Howling Wraiths; Shade Soul, Descending Dark, Abyss Shriek)
2) Reset spells settings

7. Crystal Dash settings
1) Enable/disable display of crystal dash damage
2) Crystal dash damage: set the damage of the crystal dash (-9999 to 9999, vanilla is 10)
3) Reset crystal dash settings

Crystal dash damage works the same way as dream nail damage: it can be fractional (adds up on each enemy) and negative (heals enemies).

8. Change key binds (U, I, O)
9. Reset global settings to defaults

Nail interactions: with everything that depends on the nail damage (Nail Arts, Charms: Fragile Strength, Fury of the Fallen, Sharp Shadow, Dreamshield, Thorns of Agony, Grubberfly's Elegy).
Spells interactions: with everything that influences on spells (Flukenest, Shaman Stone, Defender's Crest).
Dream nail interactions: Dream Wielder still doubles the SOUL gained from dream nail hits.
Focus interactions: Quick Focus and Deep Focus keep working on top of the time multiplier (Quick Focus is still faster, Deep Focus is still slower and heals 2 masks).