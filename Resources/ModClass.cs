using InControl;
using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UObject = UnityEngine.Object;
using Modding.Converters;
using Newtonsoft.Json;
using Satchel;
using Satchel.BetterMenus;
using UnityEngine.SceneManagement;

// mod by ino_
namespace CustomizableNailDamage
{
    // БИНДЫ ----- 
    public class KeyBinds : PlayerActionSet
    {
        public PlayerAction ToggleFloatMode;
        public PlayerAction IncreaseNailDamage;
        public PlayerAction DecreaseNailDamage;
        public PlayerAction ToggleDisplay;

        public KeyBinds()
        {
            ToggleFloatMode = CreatePlayerAction("Toggle Float Mode");

            IncreaseNailDamage = CreatePlayerAction("Increase Nail Damage");
            DecreaseNailDamage = CreatePlayerAction("Decrease Nail Damage");

            IncreaseNailDamage.AddDefaultBinding(Key.U);
            DecreaseNailDamage.AddDefaultBinding(Key.I);

            ToggleDisplay = CreatePlayerAction("Toggle Display");
            ToggleDisplay.AddDefaultBinding(Key.O);
        }
    }
    // БИНДЫ ----- 

    // НАСТРОЙКИ ----- 
    public class GlobalSettings
    {
        public bool EnableFloatNailDamage = false;
        public float CustomNailDamage = 0.1f;
        public bool EnableMod = true;
        public bool DisplayNailDamage = false;
        public float ModifiedNailDamage = 0.0f;
        public bool HealEnemiesOverMaxHP = true;

        [JsonConverter(typeof(PlayerActionSetConverter))]
        public KeyBinds keybinds = new KeyBinds();
    }
    // НАСТРОЙКИ -----

    public class CustomizableNailDamage : Mod, ICustomMenuMod, IGlobalSettings<GlobalSettings>
    {
        public CustomizableNailDamage() : base("CustomizableNailDamage") { }
        public override string GetVersion() => "1.2.0.0";
        public static GlobalSettings GS = new GlobalSettings();
        private Menu menuRef;
        private float damageRemainder = 0f;
        private Dictionary<GameObject, int> enemyMaxHealth = new();

        public void OnLoadGlobal(GlobalSettings s)
        {
            GS = s;
        }

        public GlobalSettings OnSaveGlobal()
        {
            return GS;
        }

        public bool ToggleButtonInsideMenu => true;

        // ОБНОВЛЕНИЕ УРОНА ГВОЗДЯ -----
        private void UpdateNailDamage(int newValue)
        {
            PlayerData.instance.nailDamage = newValue;
            PlayerData.instance.nailSmithUpgrades = newValue;
            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        }
        // ОБНОВЛЕНИЕ УРОНА ГВОЗДЯ -----

        // УВЕЛИЧЕНИЕ УРОНА ГВОЗДЯ -----
        private void IncreaseNailDamage(int newValue)
        {
            PlayerData.instance.nailDamage += newValue;
            PlayerData.instance.nailSmithUpgrades += newValue;
            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        }
        // УВЕЛИЧЕНИЕ УРОНА ГВОЗДЯ -----

        // УМЕНЬШЕНИЕ УРОНА ГВОЗДЯ -----
        private void DecreaseNailDamage(int newValue)
        {
            PlayerData.instance.nailDamage -= newValue;
            PlayerData.instance.nailSmithUpgrades -= newValue;
            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        }
        // УМЕНЬШЕНИЕ УРОНА ГВОЗДЯ -----

        // ОТОБРАЖЕНИЕ УРОНА ГВОЗДЯ -----
        private string BuildDisplayText()
        {
            string baseDamage = "";
            if (!GS.EnableFloatNailDamage)
                baseDamage = $"Nail DMG: {PlayerData.instance.nailDamage} ({GS.ModifiedNailDamage})";
            else
                baseDamage = $"Nail DMG: {GS.CustomNailDamage} ({GS.ModifiedNailDamage})";

            if (GS.EnableFloatNailDamage)
            {
                return $"{baseDamage}\nRemainder: {damageRemainder:F2}";
            }

            return baseDamage;
        }
        // ОТОБРАЖЕНИЕ УРОНА ГВОЗДЯ -----

        // РАССЧЁТ УРОНА ГВОЗДЯ С АМУЛЕТАМИ (СИЛА, ЯРОСТЬ) -----
        private float GetModifiedNailDamage(float baseDamage)
        {
            float modified = baseDamage;

            if (PlayerData.instance.equippedCharms.Contains(6))
                modified *= 1.5f;
            if (PlayerData.instance.equippedCharms.Contains(25) && PlayerData.instance.health <= 1)
                modified *= 1.75f;

            //Log($"Got modified nail DMG: {modified}");
            return modified;
        }

        // РАССЧЁТ УРОНА ГВОЗДЯ С АМУЛЕТАМИ (СИЛА, ЯРОСТЬ) -----

        // МЕНЮ -----
        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            if (menuRef == null)
            {
                menuRef = new Menu("CustomizableNailDamage", new Element[]
                {

                    new HorizontalOption // ВКЛ/ВЫКЛ МОД
                    (
                        name: "Enable Mod",
                        description: "Toggle mod On/Off",
                        values: new[] { "Off", "On" },
                        applySetting: index =>
                        {
                            GS.EnableMod = index == 1;
                            OnSaveGlobal();
                            Log($"Mod {(GS.EnableMod ? "enabled" : "disabled")}");
                        },

                        loadSetting: () => GS.EnableMod ? 1 : 0
                    ),

                    new HorizontalOption // ВКЛ/ВЫКЛ ДРОБНЫЙ УРОН ГВОЗДЯ
                    (
                        name: "Enable Float Damage",
                        description: "Toggle fractional nail damage logic",
                        values: new[] { "Off", "On" },
                        applySetting: index =>
                        {
                            GS.EnableFloatNailDamage = index == 1;
                            OnSaveGlobal();
                            if (GS.EnableFloatNailDamage)
                                GS.ModifiedNailDamage = GetModifiedNailDamage(GS.CustomNailDamage);
                            else
                                GS.ModifiedNailDamage = Mathf.RoundToInt(GetModifiedNailDamage(PlayerData.instance.nailDamage));
                            //Log($"Float nail damage {(GS.EnableFloatNailDamage ? "enabled" : "disabled")}");
                        },

                        loadSetting: () => GS.EnableFloatNailDamage ? 1 : 0
                    ),

                    new HorizontalOption // ВКЛ/ВЫКЛ ОТОБРАЖЕНИЕ УРОНА ГВОЗДЯ
                    (
                        name: "Show Nail Damage",
                        description: "Toggle display On/Off",
                        values: new[] { "Off", "On" },
                        applySetting: index =>
                        {
                            GS.DisplayNailDamage = index == 1;
                            OnSaveGlobal();
                            //Log($"Display nail damage {(GS.DisplayNailDamage ? "enabled" : "disabled")}");
                        },

                        loadSetting: () => GS.DisplayNailDamage ? 1 : 0
                    ),

                    new HorizontalOption // ВКЛ/ВЫКЛ ОТОБРАЖЕНИЕ УРОНА ГВОЗДЯ
                    (
                        name: "Heal Enemies Over Their Max HP",
                        description: "Toggle healing (negative) nail logic",
                        values: new[] { "Off", "On" },
                        applySetting: index =>
                        {
                            GS.HealEnemiesOverMaxHP = index == 1;
                            OnSaveGlobal();
                            //Log($"Heal enemies over their max HP {(GS.HealEnemiesOverMaxHP ? "enabled" : "disabled")}");
                        },

                        loadSetting: () => GS.HealEnemiesOverMaxHP ? 1 : 0
                    ),

                    new CustomSlider // УРОН ДРОБНОГО ГВОЗДЯ
                    (
                        name: "Custom Float Nail Damage",
                        storeValue: val =>
                        {
                            float rounded = Mathf.Round(val * 100f) / 100f;
                            GS.CustomNailDamage = rounded;
                            if (GS.EnableFloatNailDamage)
                                GS.ModifiedNailDamage = GetModifiedNailDamage(GS.CustomNailDamage);
                            //Log($"Custom float nail damage set to {rounded}");
                        },

                        loadValue: () => GS.CustomNailDamage,
                        minValue: 0.01f,
                        maxValue: 0.99f,
                        wholeNumbers: false
                    ),

                    new KeyBind
                    (
                        name: "Increase Nail Damage Key",
                        playerAction: GS.keybinds.IncreaseNailDamage
                    ),

                    new KeyBind
                    (
                        name: "Decrease Nail Damage Key",
                        playerAction: GS.keybinds.DecreaseNailDamage
                    ),

                    new KeyBind
                    (
                        name: "Display Nail Damage Key",
                        playerAction: GS.keybinds.ToggleDisplay
                    )

                });
            }

            return menuRef.GetMenuScreen(modListMenu);
        }
        // МЕНЮ -----

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            base.Initialize(preloadedObjects);
            ModHooks.HeroUpdateHook += OnHeroUpdate;
            ModHooks.SlashHitHook += OnSlashHit;
            ModHooks.OnEnableEnemyHook += OnEnableEnemy;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;

            Log("CustomizableNailDamage initialized");
        }

        private bool OnEnableEnemy(GameObject enemy, bool isDead)
        {
            if (!GS.EnableMod) return isDead; // ЕСЛИ МОД ОТКЛЮЧЁН, ВЕРНУТЬСЯ
            if (enemy == null || isDead) return isDead;

            var hm = enemy.GetComponent<HealthManager>();
            if (hm != null && !enemyMaxHealth.ContainsKey(enemy))
            {
                enemyMaxHealth[enemy] = hm.hp;
                //Log($"[Enemy Enabled] {enemy.name} with max HP: {hm.hp}");
            }

            return isDead;
        }

        private List<string> recentlyHit = new List<string>();

        // УДАЛИТЬ ДИСПЛЕЙ ПРИ ВЫХОДЕ В ГЛАВНОЕ МЕНЮ ИЛИ ОЧИСТИТЬ СПИСОК ВРАГОВ ПРИ СМЕНЕ СЦЕНЫ -----
        private void OnSceneChanged(Scene from, Scene to)
        {
            enemyMaxHealth.Clear();
            if (to.name == "Menu_Title" || to.name == "Quit_To_Menu")
            {
                ModDisplay.Instance?.Destroy();
                //Log("[SceneChange] Destroyed display due to exiting save");
            }
        }
        // УДАЛИТЬ ДИСПЛЕЙ ПРИ ВЫХОДЕ В ГЛАВНОЕ МЕНЮ ИЛИ ОЧИСТИТЬ СПИСОК ВРАГОВ ПРИ СМЕНЕ СЦЕНЫ -----

        public void OnHeroUpdate()
        {
            if (!GS.EnableMod) // ЕСЛИ МОД ОТКЛЮЧЁН, ТО УДАЛИТЬ ДИСПЛЕЙ
            {
                ModDisplay.Instance?.Destroy();
                return;
            }

            // УБОГАЯ ЛОГИКА ОТОБРАЖЕНИЯ ДИСПЛЕЯ -----
            if (GS.keybinds.ToggleDisplay.WasPressed)
            {
                GS.DisplayNailDamage = !GS.DisplayNailDamage;
                OnSaveGlobal();
                //Log($"[ToggleDisplay] Now: {(GS.DisplayNailDamage ? "ON" : "OFF")}");

                if (!GS.DisplayNailDamage)
                    ModDisplay.Instance?.Destroy();
                else
                {
                    if (ModDisplay.Instance == null)
                        ModDisplay.Instance = new ModDisplay();
                    ModDisplay.Instance.Display(BuildDisplayText());
                }
            }

            if (GS.DisplayNailDamage)
            {
                if (ModDisplay.Instance == null)
                    ModDisplay.Instance = new ModDisplay();
                if (!GS.EnableFloatNailDamage)
                {
                    float modified = GetModifiedNailDamage(PlayerData.instance.nailDamage);
                    GS.ModifiedNailDamage = Mathf.RoundToInt(modified);
                }
                else
                    GS.ModifiedNailDamage = GetModifiedNailDamage(GS.CustomNailDamage);
                ModDisplay.Instance.Display(BuildDisplayText());
            }
            else
                ModDisplay.Instance?.Destroy();
            // УБОГАЯ ЛОГИКА ОТОБРАЖЕНИЯ ДИСПЛЕЯ -----

            if (GS.keybinds.IncreaseNailDamage.WasPressed)
            {
                IncreaseNailDamage(1);
                float modified = GetModifiedNailDamage(PlayerData.instance.nailDamage);
                GS.ModifiedNailDamage = Mathf.RoundToInt(modified);
                //Log($"+1 NailDMG, now: {PlayerData.instance.nailDamage}");
            }

            if (GS.keybinds.DecreaseNailDamage.WasPressed)
            {
                DecreaseNailDamage(1);
                float modified = GetModifiedNailDamage(PlayerData.instance.nailDamage);
                GS.ModifiedNailDamage = Mathf.RoundToInt(modified);
                //Log($"-1 NailDMG, now: {PlayerData.instance.nailDamage}");
            }

            // УБОГАЯ ЛОГИКА ОТОБРАЖЕНИЯ ДИСПЛЕЯ -----
            if (GS.DisplayNailDamage)
            {
                if (ModDisplay.Instance == null)
                    ModDisplay.Instance = new ModDisplay();

                ModDisplay.Instance.Display(BuildDisplayText());
            }
            else
            {
                ModDisplay.Instance?.Destroy();
            }
            // УБОГАЯ ЛОГИКА ОТОБРАЖЕНИЯ ДИСПЛЕЯ -----
        }

        // УБОГАЯ ЛОГИКА УДАРА ВРАГОВ ДЛЯ ОТРИЦАТЕЛЬНОГО И ДРОБНОГО ГВОЗДЯ -----
        private void OnSlashHit(Collider2D otherCollider, GameObject slash)
        {
            if (!GS.EnableMod) return; // ЕСЛИ МОД ОТКЛЮЧЁН, ВЕРНУТЬСЯ

            // ОТРИЦАТЕЛЬНЫЙ ГВОЗДЬ И ХИЛ -----
            if (!GS.EnableFloatNailDamage)
            {
                if (PlayerData.instance.nailDamage > 0) return;

                var enemy = otherCollider.gameObject.GetComponent<HealthManager>();
                if (enemy == null || recentlyHit.Contains(enemy.name)) return;

                float modified = GetModifiedNailDamage(PlayerData.instance.nailDamage);
                GS.ModifiedNailDamage = Mathf.RoundToInt(modified);
                int healAmount = Mathf.Abs(Mathf.RoundToInt(modified));
                int previousNailDMG = PlayerData.instance.nailDamage;

                UpdateNailDamage(1);
                //enemy.hp++;

                if (previousNailDMG < 0)
                {
                    if (enemyMaxHealth.TryGetValue(enemy.gameObject, out int maxHp))
                    {
                        int totalHeal = healAmount + 1;

                        if (PlayerData.instance.equippedCharms.Contains(25) && PlayerData.instance.health <= 1)
                        {
                            totalHeal += 2;
                        }

                        if (!GS.HealEnemiesOverMaxHP)
                        {
                            int healable = Mathf.Max(0, maxHp - enemy.hp);
                            int appliedHeal = Mathf.Min(totalHeal, healable);

                            if (appliedHeal == 0)
                            {
                                //Log($"{enemy.name} not healed: current HP {enemy.hp} >= max HP {maxHp}");
                            }
                            else
                            {
                                enemy.hp += appliedHeal;
                                //Log($"{enemy.name} partially healed: +{appliedHeal} (max {maxHp}, now {enemy.hp})");
                            }
                        }
                        else
                        {
                            enemy.hp += totalHeal;
                            //Log($"{enemy.name} fully healed over max: +{totalHeal} (now {enemy.hp}, max {maxHp})");
                        }
                    }
                    else
                    {
                        int totalHeal = healAmount + 1;
                        if (PlayerData.instance.equippedCharms.Contains(25) && PlayerData.instance.health <= 1)
                        {
                            totalHeal += 2;
                        }

                        enemy.hp += totalHeal;
                        //Log($"{enemy.name} healed without maxHp info: +{totalHeal} (now {enemy.hp})");
                    }

                    //Log($"{enemy.name} was cured on {healAmount} HP (with neutralized hit)");
                }

                if (previousNailDMG == 0)
                    enemy.hp++;

                recentlyHit.Add(enemy.name);
                GameManager.instance.StartCoroutine(RemoveFromRecentlyHit(enemy.name));
                enemy.hp++;

                UpdateNailDamage(previousNailDMG);
            }
            // ОТРИЦАТЕЛЬНЫЙ ГВОЗДЬ И ХИЛ -----

            // ДРОБНЫЙ ГВОЗДЬ -----
            else
            {
                var enemy = otherCollider.gameObject.GetComponent<HealthManager>();
                if (enemy == null || recentlyHit.Contains(enemy.name)) return;

                int prevNailDMG = PlayerData.instance.nailDamage;

                float modified = GetModifiedNailDamage(GS.CustomNailDamage);
                GS.ModifiedNailDamage = modified;
                damageRemainder += modified;

                if (modified < 1)
                {
                    UpdateNailDamage(1);

                    if (damageRemainder >= 1f)
                    {
                        damageRemainder -= 1f;
                        //Log($"[Fractional Hit] DEAL 1 real damage to {enemy.name}");
                        recentlyHit.Add(enemy.name);
                        GameManager.instance.StartCoroutine(RemoveFromRecentlyHit(enemy.name));
                    }
                    else
                    {
                        enemy.hp++;
                        recentlyHit.Add(enemy.name);
                        GameManager.instance.StartCoroutine(RemoveFromRecentlyHit(enemy.name));
                    }
                    
                    enemy.hp++;
                    if (PlayerData.instance.equippedCharms.Contains(25) && PlayerData.instance.health <= 1)
                    {
                        enemy.hp += 2;
                    }
                }

                else
                {
                    int wholeDamage = Mathf.FloorToInt(modified);
                    float fractional = modified - wholeDamage;
                    UpdateNailDamage(wholeDamage);

                    for (int i = 0; i < wholeDamage; i++)
                    {
                        damageRemainder -= 1f;
                    }

                    //Log($"[Fractional Hit] +{modified:F2} (Whole: {wholeDamage}, Fractional: {fractional}, Remainder now: {damageRemainder})");

                    if (damageRemainder >= 1f)
                    {
                        damageRemainder -= 1f;
                        enemy.hp -= 1;
                        //Log($"[Fractional Hit] Added 1 extra damage to {enemy.name}");
                    }

                    recentlyHit.Add(enemy.name);
                    GameManager.instance.StartCoroutine(RemoveFromRecentlyHit(enemy.name));
                    enemy.hp++;

                    if (PlayerData.instance.equippedCharms.Contains(25) && PlayerData.instance.health <= 1)
                    {
                        enemy.hp += 2;
                    }
                }

                UpdateNailDamage(prevNailDMG);
            }
            // ДРОБНЫЙ ГВОЗДЬ -----

        }
        // УБОГАЯ ЛОГИКА УДАРА ВРАГОВ ДЛЯ ОТРИЦАТЕЛЬНОГО И ДРОБНОГО ГВОЗДЯ -----

        private IEnumerator RemoveFromRecentlyHit(string name) // НЕ ЗНАЮ ЧТО ЭТО, НО ПУСТЬ БУДЕТ
        {
            yield return new WaitForSeconds(0.1f);
            recentlyHit.Remove(name);
        }
    }
}