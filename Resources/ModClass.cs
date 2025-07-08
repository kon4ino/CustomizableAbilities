using GlobalEnums;
//using HKMirror;
//using HKMirror.Hooks.ILHooks;
//using HKMirror.Hooks.OnHooks;
//using HKMirror.Reflection.SingletonClasses;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using IL;
using InControl;
using Modding;
using Modding.Converters;
using MonoMod.Cil;
using Newtonsoft.Json;
using On;
using On.HutongGames.PlayMaker;
using Satchel;
using Satchel.BetterMenus;
using Satchel.Futils;
//using SFCore.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
//using UObject = UnityEngine.Object;

/* mod by ino_ (kon4ino), 1.3.0.0
 thank CharmChanger mod for some code */
namespace CustomizableNailDamage
{

    public class MyLog // Я УСТАЛ ОТКОММЕНЧИВАТЬ КАЖДЫЙ ЛОГ
    {
        bool enablelog = true;
        public void log<T>(T messagetolog)
        {
            if (enablelog)
                Modding.Logger.Log(messagetolog);
        }
        public void log<T>(T messagetolog, bool dolog)
        {
            if (enablelog && dolog)
            {
                Modding.Logger.Log(messagetolog);
            }
        }
    }

    public class KeyBinds : PlayerActionSet // БИНДЫ
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

    public class GlobalSettings // ГЛОБАЛЬНЫЕ НАСТРОЙКИ
    {
        public bool EnableFloatNailDamage = false;
        public float CustomFloatNailDamage = 0.1f;
        public bool EnableMod = true;
        public bool DisplayNailDamage = false;
        public float ModifiedNailDamage = 0.0f;
        public bool HealEnemiesOverMaxHP = true;
        public int NailUpgrade = 0;

        [JsonConverter(typeof(PlayerActionSetConverter))]
        public KeyBinds keybinds = new KeyBinds();
    }

    public class LocalSettings // ЛОКАЛЬНЫЕ НАСТРОЙКИ
    {
        public int CustomIntNailDamage = PlayerData.instance.nailDamage;
        public int StoredNailDamage = 0;
        public bool HasStoredValue = false;
        public bool firstLoading = true;
    }

    public class CustomizableNailDamage : Mod, ICustomMenuMod, IGlobalSettings<GlobalSettings>, ILocalSettings<LocalSettings>
    {
        public CustomizableNailDamage() : base("CustomizableNailDamage") { }
        public override string GetVersion() => "1.3.0.0";
        public static LocalSettings LS = new LocalSettings();
        public static GlobalSettings GS = new GlobalSettings();
        public MyLog logf = new MyLog();
        private Menu menuRef;
        private float damageRemainder = 0f; // ОСТАТОК ДЛЯ ДРОБНОГО ГВОЗДЯ
        private Dictionary<GameObject, int> enemyMaxHealth = new(); // СЛОВАРЬ В КОТОРОМ ХРАНЯТСЯ МАКС. ЗДОРОВЬЕ КАЖДОГО ВРАГА
        private Dictionary<GameObject, float> fractionalPool = new(); // СЛОВАРЬ В КОТОРОМ ХРАНЯТСЯ ОСТАТКИ ДРОБНОГО УРОНА ДЛЯ КАЖДОГО ВРАГА
        private bool isDamageFromThornsOfAgony = false;
        private bool isThornsOfAgonyDamagedOnce = false;
        Vector2 velocity;

        // ВКЛ/ВЫКЛ МОД -----
        public void DisableMod()
        {
            ModDisplay.Instance?.Destroy();
            ModHooks.BeforeSavegameSaveHook -= OnSaveSaved;
            ModHooks.AfterSavegameLoadHook += OnSaveLoaded;
            ModHooks.HeroUpdateHook -= OnHeroUpdate;
            ModHooks.OnEnableEnemyHook -= OnEnableEnemy;
            ModHooks.OnReceiveDeathEventHook -= OnReceiveDeathEvent;
            On.HealthManager.TakeDamage -= OnEnemyDamaged;
            On.SetHP.OnEnter -= DreamShieldDamage;
            On.HutongGames.PlayMaker.Actions.SetFsmInt.OnEnter -= ThornsOfAgonyDamage;
            On.HutongGames.PlayMaker.Actions.FloatMultiply.OnEnter -= StrengthDamageIncrease;
            On.HutongGames.PlayMaker.Actions.SetFsmFloat.OnEnter -= FotFNailScaling;
            On.HutongGames.PlayMaker.Actions.FloatMultiply.OnEnter -= FotFNailArtScaling;

            Log("Disabled");
        }
        public void EnableMod()
        {
            ModHooks.BeforeSavegameSaveHook += OnSaveSaved;
            ModHooks.AfterSavegameLoadHook += OnSaveLoaded;
            ModHooks.HeroUpdateHook += OnHeroUpdate;
            ModHooks.OnEnableEnemyHook += OnEnableEnemy;
            ModHooks.OnReceiveDeathEventHook += OnReceiveDeathEvent;
            On.HealthManager.TakeDamage += OnEnemyDamaged;
            On.SetHP.OnEnter += DreamShieldDamage;
            On.HutongGames.PlayMaker.Actions.SetFsmInt.OnEnter += ThornsOfAgonyDamage;
            On.HutongGames.PlayMaker.Actions.FloatMultiply.OnEnter += StrengthDamageIncrease;
            On.HutongGames.PlayMaker.Actions.SetFsmFloat.OnEnter += FotFNailScaling;
            On.HutongGames.PlayMaker.Actions.FloatMultiply.OnEnter += FotFNailArtScaling;

            Log("Enabled");
        }
        // ВКЛ/ВЫКЛ МОД -----

        public void OnLoadLocal(LocalSettings s)
        {
            LS = s;
        }
        public LocalSettings OnSaveLocal()
        {
            return LS;
        }
        public void OnLoadGlobal(GlobalSettings s)
        {
            GS = s;
        }
        public GlobalSettings OnSaveGlobal()
        {
            return GS;
        }

        private float GetPool(GameObject enemyName)
        {
            if (!fractionalPool.TryGetValue(enemyName, out var pool))
                pool = 0f;
            return pool;
        }
        private void SetPool(GameObject enemyName, float value)
        {
            fractionalPool[enemyName] = value;
        }
        private void UpdatePool(GameObject enemyName, float value)
        {
            fractionalPool[enemyName] += value;
        }

        public bool ToggleButtonInsideMenu => true;

        private void UpdateNailDamage(int newValue) // ОБНОВЛЕНИЕ УРОНА ГВОЗДЯ
        {
            if (newValue > 0)
            {
                PlayerData.instance.nailDamage = newValue;
                PlayerData.instance.nailSmithUpgrades = newValue;

            }
            else
            {
                PlayerData.instance.nailDamage = 1;
                PlayerData.instance.nailSmithUpgrades = 1;
            }
            LS.CustomIntNailDamage = newValue;
            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        }
        private void IncreaseNailDamage(int newValue) // УВЕЛИЧЕНИЕ УРОНА ГВОЗДЯ
        {
            if (!(LS.CustomIntNailDamage < 1))
            {
                PlayerData.instance.nailDamage += newValue;
                PlayerData.instance.nailSmithUpgrades += newValue;
            }
            LS.CustomIntNailDamage += newValue;
            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        }
        private void DecreaseNailDamage(int newValue) // УМЕНЬШЕНИЕ УРОНА ГВОЗДЯ
        {
            if (PlayerData.instance.nailDamage - newValue > 0)
            {
                PlayerData.instance.nailDamage -= newValue;
                PlayerData.instance.nailSmithUpgrades -= newValue;
            }
            else
            {
                PlayerData.instance.nailDamage = 1;
                PlayerData.instance.nailSmithUpgrades = 1;
            }
            LS.CustomIntNailDamage -= newValue;
            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        }

        private string BuildDisplayText() // ОТОБРАЖЕНИЕ УРОНА ГВОЗДЯ
        {
            string baseDamage = "";
            if (!GS.EnableFloatNailDamage)
                baseDamage = $"Nail DMG: {LS.CustomIntNailDamage} ({GS.ModifiedNailDamage})";
            else
                baseDamage = $"Nail DMG: {GS.CustomFloatNailDamage} ({GS.ModifiedNailDamage})";

            return baseDamage;
        }

        private float GetModifiedNailDamage(float baseDamage) // РАССЧЁТ УРОНА ГВОЗДЯ С АМУЛЕТАМИ (СИЛА, ЯРОСТЬ)
        {
            float modified = baseDamage;

            if (PlayerData.instance.equippedCharms.Contains(25))
                modified *= 1.5f;
            if (PlayerData.instance.equippedCharms.Contains(6) && PlayerData.instance.health <= 1)
                modified *= 1.75f;

            return modified;
        }
        private float GetModifiedNailArtDamage(float baseDamage, bool isCycloneSlash) // РАССЧЁТ УРОНА ТЕХНИК ГВОЗДЯ С АМУЛЕТАМИ (ЯРОСТЬ)
        {
            float modified = baseDamage;

            if (PlayerData.instance.equippedCharms.Contains(6) && PlayerData.instance.health <= 1)
                modified *= 1.75f;
            if (isCycloneSlash)
                modified *= 1.25f;
            else
                modified *= 2.5f;

            return modified;
        }
        private float GetModifiedSharpShadowDashDamage(float baseDamage) // РАССЧЁТ УРОНА ПРОНИЗЫВАЮЩЕЙ ТЕНИ С АМУЛЕТАМИ (ТРЮКАЧ)
        {
            float modified = baseDamage;

            if (PlayerData.instance.equippedCharms.Contains(31))
                modified *= 1.5f;

            return modified;
        }
        private float GetModifiedNailBeamDamage(float baseDamage) // РАССЧЁТ УРОНА ВОЛНЫ ЭЛЕГИИ КУКОЛКИ С АМУЛЕТАМИ (СИЛА, ЯРОСТЬ)
        {
            float modified = baseDamage * 0.5f;

            if (PlayerData.instance.equippedCharms.Contains(25))
                modified *= 1.5f;
            if (PlayerData.instance.equippedCharms.Contains(6) && PlayerData.instance.health <= 1)
                modified *= 1.5f;

            return modified;
        }

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
                            if (!GS.EnableMod)
                                DisableMod();
                            else
                                EnableMod();
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
                                GS.ModifiedNailDamage = GetModifiedNailDamage(GS.CustomFloatNailDamage);
                            else
                                GS.ModifiedNailDamage = Mathf.RoundToInt(GetModifiedNailDamage(LS.CustomIntNailDamage));
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
                        },

                        loadSetting: () => GS.DisplayNailDamage ? 1 : 0
                    ),

                    new HorizontalOption // ХИЛИТЬ ВРАГОВ ПОВЕРХ ИХ МАКСИМАЛЬНОГО ХП
                    (
                        name: "Heal Enemies Over Their Max HP",
                        description: "Toggle healing (negative) nail logic",
                        values: new[] { "Off", "On" },
                        applySetting: index =>
                        {
                            GS.HealEnemiesOverMaxHP = index == 1;
                            OnSaveGlobal();
                        },

                        loadSetting: () => GS.HealEnemiesOverMaxHP ? 1 : 0
                    ),

                    new HorizontalOption // ПРЕСЕТ ВАНИЛЬНЫХ ГВОЗДЕЙ
                    (
                        name: "Set Vanilla Nail Damage",
                        description: "Vanilla positive and negative nail damage set",
                        values: new[] { "Old", "Sharpened", "Channelled", "Coiled", "Pure", "-Old", "-Sharpened", "-Channelled", "-Coiled", "-Pure" },
                        applySetting: index =>
                        {
                            if (!GS.EnableFloatNailDamage)
                            {
                                // OLD = +-5, SHARPENED = +-9, CHANNELLED = +-13, COILED = +-17, PURE = +-21
                                int vanillaDmg = 5 * (index+1) - (index+1) + 1;
                                if (index > 4 && index <= 9)
                                    vanillaDmg = -1 * (5 * (index-4) - (index-4) + 1);
                                UpdateNailDamage(vanillaDmg);
                                GS.ModifiedNailDamage = Mathf.RoundToInt(GetModifiedNailDamage(vanillaDmg));
                            }
                        },

                        loadSetting: () => GS.NailUpgrade
                    ),

                    new HundredthSlider // УРОН ДРОБНОГО ГВОЗДЯ
                    (
                        name: "Custom Float Nail Damage",
                        storeValue: val =>
                        {
                            float rounded = Mathf.Round(val * 100f) / 100f;
                            GS.CustomFloatNailDamage = rounded;
                            if (GS.EnableFloatNailDamage)
                                GS.ModifiedNailDamage = GetModifiedNailDamage(GS.CustomFloatNailDamage);
                        },

                        loadValue: () => GS.CustomFloatNailDamage,
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

        // ПОДКЛЮЧЕНИЕ ХУКОВ -----
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            base.Initialize(preloadedObjects);
            ModHooks.BeforeSavegameSaveHook += OnSaveSaved;
            ModHooks.AfterSavegameLoadHook += OnSaveLoaded;
            ModHooks.HeroUpdateHook += OnHeroUpdate;
            ModHooks.OnEnableEnemyHook += OnEnableEnemy;
            ModHooks.OnReceiveDeathEventHook += OnReceiveDeathEvent;
            On.HealthManager.TakeDamage += OnEnemyDamaged;
            On.SetHP.OnEnter += DreamShieldDamage;
            On.HutongGames.PlayMaker.Actions.SetFsmInt.OnEnter += ThornsOfAgonyDamage;
            On.HutongGames.PlayMaker.Actions.FloatMultiply.OnEnter += StrengthDamageIncrease;
            On.HutongGames.PlayMaker.Actions.SetFsmFloat.OnEnter += FotFNailScaling;
            On.HutongGames.PlayMaker.Actions.FloatMultiply.OnEnter += FotFNailArtScaling;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;

            Log("Initializing");
        }
        // ПОДКЛЮЧЕНИЕ ХУКОВ -----

        // МЕНЯЮ МНОЖИТЕЛИ УРОНА ГВОЗДЯ У АМУЛЕТОВ, ДАБЫ ОНИ НЕ КОНФЛИКТОВАЛИ С СИСТЕМОЙ -----
        private void StrengthDamageIncrease(On.HutongGames.PlayMaker.Actions.FloatMultiply.orig_OnEnter orig, FloatMultiply self)
        {
            if (!GS.EnableMod)
            {
                orig(self);
                return;
            }
            if (self.Fsm.GameObject.name == "Attacks" && self.Fsm.Name == "Set Slash Damage" && self.State.Name == "Glass Attack Modifier")
            {
                self.multiplyBy.Value = 1f;
            }

            orig(self);
        }
        private void FotFNailScaling(On.HutongGames.PlayMaker.Actions.SetFsmFloat.orig_OnEnter orig, SetFsmFloat self)
        {
            if (!GS.EnableMod)
            {
                orig(self);
                return;
            }
            if (self.Fsm.GameObject.name == "Charm Effects" && self.Fsm.Name == "Fury" && self.State.Name == "Activate")
            {
                self.setValue.Value = 1f;
            }

            orig(self);
        }
        private void FotFNailArtScaling(On.HutongGames.PlayMaker.Actions.FloatMultiply.orig_OnEnter orig, FloatMultiply self)
        {
            if (!GS.EnableMod)
            {
                orig(self);
                return;
            }
            if (self.Fsm.Name == "nailart_damage" && self.State.Name == "Fury?")
            {
                self.multiplyBy.Value = 1f;
            }

            orig(self);
        }
        // МЕНЯЮ МНОЖИТЕЛИ УРОНА ГВОЗДЯ У АМУЛЕТОВ, ДАБЫ ОНИ НЕ КОНФЛИКТОВАЛИ С СИСТЕМОЙ -----

        // УРОН ЩИТА ГРЁЗ -----
        private void DreamShieldDamage(On.SetHP.orig_OnEnter orig, SetHP self)
        {
            if (!GS.EnableMod)
            {
                orig(self);
                return;
            }

            if (self.State.Name == "Hit" && self.Fsm.GameObject.name == "Shield")
            {
                GameObject enemy = self.Fsm.Variables.GetFsmGameObject("Enemy").Value;
                var damageVar = self.Fsm.Variables.GetFsmInt("Damage");
                var enemyHpVar = self.Fsm.Variables.GetFsmInt("Enemy HP");
                logf.log($"DreamShieldDamage: {self.hp}, {self.State.Name}, {self.Fsm.GameObject.name}", false);

                HealthManager hm = enemy.GetComponent<HealthManager>();

                if (!GS.EnableFloatNailDamage)
                {
                    damageVar.Value = PlayerData.instance.nailDamage;
                    int hpBefore = hm.hp;
                    int newHp = Mathf.Max(hpBefore - damageVar.Value, 0);
                    self.hp.Value = newHp;

                    if (LS.CustomIntNailDamage < 1)
                    {
                        if (GS.HealEnemiesOverMaxHP)
                            self.hp.Value += Mathf.Abs(LS.CustomIntNailDamage) + 1;

                        else
                        {
                            if (enemyMaxHealth.TryGetValue(enemy.gameObject, out int maxHp))
                            {
                                int totalHeal = Mathf.Abs(LS.CustomIntNailDamage);
                                int healable = Mathf.Max(0, maxHp - hm.hp);
                                int appliedHeal = Mathf.Min(totalHeal, healable);

                                if (appliedHeal == 0)
                                {
                                }
                                else
                                {
                                    self.hp.Value += appliedHeal + 1;
                                }
                            }
                        }
                    }
                }

                else
                {
                    damageVar.Value = 1;
                    int hpBefore = hm.hp;
                    int newHp = Mathf.Max(hpBefore - damageVar.Value, 0);
                    self.hp.Value = newHp;

                    UpdatePool(enemy.gameObject, GS.CustomFloatNailDamage);
                    damageRemainder = GetPool(enemy.gameObject);
                    int damageToDeal = 0;

                    if (damageRemainder >= 1)
                    {
                        while (damageRemainder >= 1)
                        {
                            damageToDeal++;
                            damageRemainder--;
                        }
                    }

                    SetPool(enemy.gameObject, damageRemainder);
                    self.hp.Value -= damageToDeal;
                    self.hp.Value++;
                }

                logf.log($"DreamShieldDamage, enemy hp: {hm.hp}, damage taken: {self.Fsm.Variables.GetFsmInt("Damage")}", false);
            }

            orig(self);
        }
        // УРОН ЩИТА ГРЁЗ -----

        private void ThornsOfAgonyDamage(On.HutongGames.PlayMaker.Actions.SetFsmInt.orig_OnEnter orig, SetFsmInt self)
        {
            if (!GS.EnableMod)
            {
                orig(self);
                return;
            }
            if (self.Fsm.GameObject.name.StartsWith("Hit ") && self.Fsm.Name == "set_thorn_damage" && self.State.Name == "Set")
            {
                if (LS.CustomIntNailDamage < 1 || GS.EnableFloatNailDamage)
                {
                    self.setValue.Value = 1;
                    isDamageFromThornsOfAgony = true;
                    isThornsOfAgonyDamagedOnce = false;
                }
                else
                {
                    self.setValue.Value = LS.CustomIntNailDamage;
                }
            }

            orig(self);
        }
        
        private void OnSaveSaved(SaveGameData data) // ПРОВЕРЯЕМ, ЕСЛИ УРОН ГВОЗДЯ < 1
        {
            if (PlayerData.instance.nailDamage < 1)
            {
                LS.StoredNailDamage = PlayerData.instance.nailDamage;
                LS.HasStoredValue = true;
                UpdateNailDamage(1);
            }
        }

        private void OnSaveLoaded(SaveGameData data)
        {
            if (LS.CustomIntNailDamage != PlayerData.instance.nailDamage && PlayerData.instance.nailDamage != 1)
            {
                LS.CustomIntNailDamage = PlayerData.instance.nailDamage;
            }
        }

        // ПРИ НАНЕСЕНИИ УРОНА ВРАГУ (ИСПОЛЬЗУЕТСЯ ДЛЯ ПРОН. ТЕНИ, ГВОЗДЯ, ТЕХНИК ГВОЗДЯ, ЭЛЕГИИ КУКОЛКИ, КОЛЮЧЕК СТРАДАНИЙ) -----
        public void OnEnemyDamaged(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (!GS.EnableMod)
            {
                orig(self, hitInstance);
                return;
            }

            string attackerName = hitInstance.Source?.name ?? "";
            logf.log($"OnEnemyDamaged: {attackerName}, {hitInstance.Source}, {hitInstance.SpecialType}", false);
            logf.log($"OnEnemyDamaged, attack type: {hitInstance.AttackType}", false);

            if (hitInstance.AttackType == AttackTypes.SharpShadow)
            {
                var enemy = self.gameObject.GetComponent<HealthManager>();
                hitInstance.DamageDealt = 1;

                if (!GS.EnableFloatNailDamage) // УРОН +/- ТЕНИ
                {
                    int dashDamage = Mathf.RoundToInt(GetModifiedSharpShadowDashDamage(LS.CustomIntNailDamage));
                    if (LS.CustomIntNailDamage <= 0)
                    {
                        if (GS.HealEnemiesOverMaxHP)
                        {
                            enemy.hp += Mathf.Abs(dashDamage);
                        }

                        else
                        {
                            if (enemyMaxHealth.TryGetValue(enemy.gameObject, out int maxHp))
                            {
                                int totalHeal = Mathf.Abs(dashDamage);
                                int healable = Mathf.Max(0, maxHp - enemy.hp);
                                int appliedHeal = Mathf.Min(totalHeal, healable);

                                if (appliedHeal == 0)
                                {
                                }
                                else
                                {
                                    enemy.hp += appliedHeal;
                                }
                            }
                        }
                        enemy.hp++;
                    }

                    else
                    {
                        hitInstance.DamageDealt = dashDamage;
                    }
                }

                else // УРОН ДРОБНОЙ ТЕНИ
                {
                    float dashDamage = GetModifiedSharpShadowDashDamage(GS.CustomFloatNailDamage);
                    UpdatePool(enemy.gameObject, dashDamage);
                    damageRemainder = GetPool(enemy.gameObject);
                    int damageToDeal = 0;

                    if (damageRemainder >= 1)
                    {
                        while (damageRemainder >= 1)
                        {
                            damageToDeal++;
                            damageRemainder--;
                        }
                        hitInstance.DamageDealt += damageToDeal;
                    }

                    SetPool(enemy.gameObject, damageRemainder);
                    enemy.hp++;
                }
            }

            else if (hitInstance.AttackType == AttackTypes.NailBeam)
            {
                var enemy = self.gameObject.GetComponent<HealthManager>();
                hitInstance.DamageDealt = 1;

                if (!GS.EnableFloatNailDamage) // УРОН ВОЛН ЭЛЕГИИ КУКОЛКИ
                {
                    int nailBeamDamage = Mathf.RoundToInt(GetModifiedNailBeamDamage(LS.CustomIntNailDamage));
                    if (LS.CustomIntNailDamage <= 0)
                    {
                        if (GS.HealEnemiesOverMaxHP)
                        {
                            enemy.hp += Mathf.Abs(nailBeamDamage);
                        }

                        else
                        {
                            if (enemyMaxHealth.TryGetValue(enemy.gameObject, out int maxHp))
                            {
                                int totalHeal = Mathf.Abs(nailBeamDamage);
                                int healable = Mathf.Max(0, maxHp - enemy.hp);
                                int appliedHeal = Mathf.Min(totalHeal, healable);

                                if (appliedHeal == 0)
                                {
                                }
                                else
                                {
                                    enemy.hp += appliedHeal;
                                }
                            }
                        }
                        enemy.hp++;
                    }

                    else
                    {
                        hitInstance.DamageDealt = nailBeamDamage;
                    }
                }

                else // УРОН ДРОБНОЙ ТЕНИ
                {
                    float nailBeamDamage = GetModifiedNailBeamDamage(GS.CustomFloatNailDamage);
                    UpdatePool(enemy.gameObject, nailBeamDamage);
                    damageRemainder = GetPool(enemy.gameObject);
                    int damageToDeal = 0;

                    if (damageRemainder >= 1)
                    {
                        while (damageRemainder >= 1)
                        {
                            damageToDeal++;
                            damageRemainder--;
                        }
                        hitInstance.DamageDealt += damageToDeal;
                    }

                    SetPool(enemy.gameObject, damageRemainder);
                    enemy.hp++;
                }
            }

            else if (hitInstance.AttackType == AttackTypes.Nail)
            {
                if (attackerName == "Great Slash" || attackerName == "Dash Slash")
                {
                    var enemy = self.gameObject.GetComponent<HealthManager>();

                    if (!GS.EnableFloatNailDamage)
                    {
                        int nailArtDamage = Mathf.RoundToInt(GetModifiedNailArtDamage(LS.CustomIntNailDamage, false));
                        if (LS.CustomIntNailDamage <= 0)
                        {
                            hitInstance.DamageDealt = 1;
                            if (GS.HealEnemiesOverMaxHP)
                            {
                                enemy.hp += Mathf.Abs(nailArtDamage);
                            }

                            else
                            {
                                if (enemyMaxHealth.TryGetValue(enemy.gameObject, out int maxHp))
                                {
                                    int totalHeal = Mathf.Abs(nailArtDamage);
                                    int healable = Mathf.Max(0, maxHp - enemy.hp);
                                    int appliedHeal = Mathf.Min(totalHeal, healable);

                                    if (appliedHeal == 0)
                                    {
                                    }
                                    else
                                    {
                                        enemy.hp += appliedHeal;
                                    }
                                }
                            }
                            enemy.hp++;
                        }

                        else
                        {
                            hitInstance.DamageDealt = nailArtDamage;
                        }
                    }

                    else
                    {
                        hitInstance.DamageDealt = 1;
                        float nailArtDamage = GetModifiedNailArtDamage(GS.CustomFloatNailDamage, false);
                        UpdatePool(enemy.gameObject, nailArtDamage);
                        damageRemainder = GetPool(enemy.gameObject);
                        int damageToDeal = 0;

                        if (damageRemainder >= 1)
                        {
                            while (damageRemainder >= 1)
                            {
                                damageToDeal++;
                                damageRemainder--;
                            }
                            hitInstance.DamageDealt += damageToDeal;
                        }

                        SetPool(enemy.gameObject, damageRemainder);
                        enemy.hp++;
                    }
                }

                else if (attackerName == "Hit L" || attackerName == "Hit R")
                {
                    var enemy = self.gameObject.GetComponent<HealthManager>();

                    if (!GS.EnableFloatNailDamage)
                    {
                        int nailArtDamage = Mathf.RoundToInt(GetModifiedNailArtDamage(LS.CustomIntNailDamage, true));
                        if (LS.CustomIntNailDamage <= 0)
                        {
                            hitInstance.DamageDealt = 1;
                            if (GS.HealEnemiesOverMaxHP)
                            {
                                enemy.hp += Mathf.Abs(nailArtDamage);
                            }

                            else
                            {
                                if (enemyMaxHealth.TryGetValue(enemy.gameObject, out int maxHp))
                                {
                                    int totalHeal = Mathf.Abs(nailArtDamage);
                                    int healable = Mathf.Max(0, maxHp - enemy.hp);
                                    int appliedHeal = Mathf.Min(totalHeal, healable);

                                    if (appliedHeal == 0)
                                    {
                                    }
                                    else
                                    {
                                        enemy.hp += appliedHeal;
                                    }
                                }
                            }
                            enemy.hp++;
                        }

                        else
                        {
                            hitInstance.DamageDealt = nailArtDamage;
                        }
                    }

                    else
                    {
                        hitInstance.DamageDealt = 1;
                        float nailArtDamage = GetModifiedNailArtDamage(GS.CustomFloatNailDamage, true);
                        UpdatePool(enemy.gameObject, nailArtDamage);
                        damageRemainder = GetPool(enemy.gameObject);
                        int damageToDeal = 0;

                        if (damageRemainder >= 1)
                        {
                            while (damageRemainder >= 1)
                            {
                                damageToDeal++;
                                damageRemainder--;
                            }
                            hitInstance.DamageDealt += damageToDeal;
                        }

                        SetPool(enemy.gameObject, damageRemainder);
                        enemy.hp++;
                    }
                }

                else
                {
                    var enemy = self.gameObject.GetComponent<HealthManager>();

                    if (!GS.EnableFloatNailDamage)
                    {
                        int nailDamage = Mathf.RoundToInt(GetModifiedNailDamage(LS.CustomIntNailDamage));
                        if (LS.CustomIntNailDamage <= 0)
                        {
                            hitInstance.DamageDealt = 1;
                            if (GS.HealEnemiesOverMaxHP)
                            {
                                enemy.hp += Mathf.Abs(nailDamage);
                            }

                            else
                            {
                                if (enemyMaxHealth.TryGetValue(enemy.gameObject, out int maxHp))
                                {
                                    int totalHeal = Mathf.Abs(nailDamage);
                                    int healable = Mathf.Max(0, maxHp - enemy.hp);
                                    int appliedHeal = Mathf.Min(totalHeal, healable);

                                    if (appliedHeal == 0)
                                    {
                                    }
                                    else
                                    {
                                        enemy.hp += appliedHeal;
                                    }
                                }
                            }
                            enemy.hp++;
                        }

                        else
                        {
                            hitInstance.DamageDealt = nailDamage;
                        }
                    }

                    else
                    {
                        hitInstance.DamageDealt = 1;
                        float nailDamage = GetModifiedNailDamage(GS.CustomFloatNailDamage);
                        UpdatePool(enemy.gameObject, nailDamage);
                        damageRemainder = GetPool(enemy.gameObject);
                        int damageToDeal = 0;

                        if (damageRemainder >= 1)
                        {
                            while (damageRemainder >= 1)
                            {
                                damageToDeal++;
                                damageRemainder--;
                            }
                            hitInstance.DamageDealt += damageToDeal;
                        }

                        SetPool(enemy.gameObject, damageRemainder);
                        enemy.hp++;
                    }
                }
            }

            else
            {
                if (isThornsOfAgonyDamagedOnce)
                {
                    var enemy = self.gameObject.GetComponent<HealthManager>();
                    isThornsOfAgonyDamagedOnce = false;

                    if (!GS.EnableFloatNailDamage)
                    {
                        if (GS.HealEnemiesOverMaxHP)
                        {
                            enemy.hp += Mathf.Abs(LS.CustomIntNailDamage);
                        }

                        else
                        {
                            if (enemyMaxHealth.TryGetValue(enemy.gameObject, out int maxHp))
                            {
                                int totalHeal = Mathf.Abs(LS.CustomIntNailDamage);
                                int healable = Mathf.Max(0, maxHp - enemy.hp);
                                int appliedHeal = Mathf.Min(totalHeal, healable);

                                if (appliedHeal == 0)
                                {
                                }
                                else
                                {
                                    enemy.hp += appliedHeal;
                                }
                            }
                        }
                        enemy.hp++;
                    }

                    else
                    {
                        UpdatePool(enemy.gameObject, GS.CustomFloatNailDamage);
                        damageRemainder = GetPool(enemy.gameObject);
                        int damageToDeal = 0;

                        if (damageRemainder >= 1)
                        {
                            while (damageRemainder >= 1)
                            {
                                damageToDeal++;
                                damageRemainder--;
                            }
                            hitInstance.DamageDealt += damageToDeal;
                        }

                        SetPool(enemy.gameObject, damageRemainder);
                        enemy.hp++;
                    }
                }

                else if (isDamageFromThornsOfAgony)
                {
                    var enemy = self.gameObject.GetComponent<HealthManager>();
                    isDamageFromThornsOfAgony = false;
                    isThornsOfAgonyDamagedOnce = true;

                    if (!GS.EnableFloatNailDamage)
                    {
                        if (GS.HealEnemiesOverMaxHP)
                        {
                            enemy.hp += Mathf.Abs(LS.CustomIntNailDamage);
                        }

                        else
                        {
                            if (enemyMaxHealth.TryGetValue(enemy.gameObject, out int maxHp))
                            {
                                int totalHeal = Mathf.Abs(LS.CustomIntNailDamage);
                                int healable = Mathf.Max(0, maxHp - enemy.hp);
                                int appliedHeal = Mathf.Min(totalHeal, healable);

                                if (appliedHeal == 0)
                                {
                                }
                                else
                                {
                                    enemy.hp += appliedHeal;
                                }
                            }
                        }
                        enemy.hp++;
                    }

                    else
                    {
                        UpdatePool(enemy.gameObject, GS.CustomFloatNailDamage);
                        damageRemainder = GetPool(enemy.gameObject);
                        int damageToDeal = 0;

                        if (damageRemainder >= 1)
                        {
                            while (damageRemainder >= 1)
                            {
                                damageToDeal++;
                                damageRemainder--;
                            }
                            hitInstance.DamageDealt += damageToDeal;
                        }

                        SetPool(enemy.gameObject, damageRemainder);
                        enemy.hp++;
                    }
                }
            }

            orig(self, hitInstance);
        }
        // ПРИ НАНЕСЕНИИ УРОНА ВРАГУ (ИСПОЛЬЗУЕТСЯ ДЛЯ ПРОН. ТЕНИ, ГВОЗДЯ, ТЕХНИК ГВОЗДЯ, ЭЛЕГИИ КУКОЛКИ, КОЛЮЧЕК СТРАДАНИЙ) -----

        private void OnReceiveDeathEvent(EnemyDeathEffects enemyDeathEffects, bool eventAlreadyReceived, ref float? attackDirection, ref bool resetDeathEvent, ref bool spellBurn, ref bool isWatery)
        {
            if (!GS.EnableMod) return;
            if (enemyDeathEffects == null) return;

            GameObject corpse = enemyDeathEffects.gameObject;
            enemyMaxHealth.Remove(corpse);
            fractionalPool.Remove(corpse);

            logf.log($"OnReceiveDeathEvent: removed {corpse.name} from dictionaries — enemy is dead", false);
        }

        private bool OnEnableEnemy(GameObject enemy, bool isDead)
        {
            if (!GS.EnableMod) return isDead; // ЕСЛИ МОД ОТКЛЮЧЁН, ВЕРНУТЬСЯ
            if (enemy == null || isDead) return isDead;

            var hm = enemy.GetComponent<HealthManager>();
            if (hm != null && !enemyMaxHealth.ContainsKey(enemy))
            {
                enemyMaxHealth[enemy] = hm.hp;
                fractionalPool[enemy] = 0f;
                logf.log($"OnEnableEnemy: {enemy.name} with max HP: {hm.hp}", false);
            }

            return isDead;
        }

        private void OnSceneChanged(Scene from, Scene to)
        {
            // ОЧИЩАЕМ СЛОВАРИ
            enemyMaxHealth.Clear();
            fractionalPool.Clear();

            if (to.name == "Menu_Title" || to.name == "Quit_To_Menu")
            {
                if (PlayerData.instance.nailDamage < 1) // СОХРАНЯЕМ УРОН ГВОЗДЯ, ДАБЫ ПОТОМ ВЕРНУТЬ ЕГО
                {
                    LS.StoredNailDamage = PlayerData.instance.nailDamage;
                    LS.HasStoredValue = true;
                    UpdateNailDamage(1);
                }
                ModDisplay.Instance?.Destroy();
            }
        }

        public void OnHeroUpdate()
        {
            if (!GS.EnableMod) // ЕСЛИ МОД ОТКЛЮЧЁН, ТО УДАЛИТЬ ДИСПЛЕЙ
            {
                ModDisplay.Instance?.Destroy();
                return;
            }

            if (LS.firstLoading)
            {
                LS.CustomIntNailDamage = PlayerData.instance.nailDamage;
                LS.firstLoading = false;
            }

            if (LS.HasStoredValue) // ВОЗВРАЩАЕМ УРОН ГВОЗДЮ ИЗ ЛОКАЛЬНЫХ НАСТРОЕК
            {
                velocity = HeroController.instance.GetComponent<Rigidbody2D>().velocity;
                if (velocity != Vector2.zero)
                {
                    UpdateNailDamage(LS.StoredNailDamage);
                    LS.HasStoredValue = false;
                }
            }
            
            // УБОГАЯ ЛОГИКА ОТОБРАЖЕНИЯ ДИСПЛЕЯ -----
            if (GS.keybinds.ToggleDisplay.WasPressed)
            {
                GS.DisplayNailDamage = !GS.DisplayNailDamage;
                OnSaveGlobal();

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
                    float modified = GetModifiedNailDamage(LS.CustomIntNailDamage);
                    GS.ModifiedNailDamage = Mathf.RoundToInt(modified);
                }
                else
                    GS.ModifiedNailDamage = GetModifiedNailDamage(GS.CustomFloatNailDamage);
                ModDisplay.Instance.Display(BuildDisplayText());
            }
            else
                ModDisplay.Instance?.Destroy();
            // УБОГАЯ ЛОГИКА ОТОБРАЖЕНИЯ ДИСПЛЕЯ -----

            if (GS.keybinds.IncreaseNailDamage.WasPressed)
            {
                IncreaseNailDamage(1);
                float modified = GetModifiedNailDamage(LS.CustomIntNailDamage);
                GS.ModifiedNailDamage = Mathf.RoundToInt(modified);
            }

            if (GS.keybinds.DecreaseNailDamage.WasPressed)
            {
                DecreaseNailDamage(1);
                float modified = GetModifiedNailDamage(LS.CustomIntNailDamage);
                GS.ModifiedNailDamage = Mathf.RoundToInt(modified);
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
    }
}