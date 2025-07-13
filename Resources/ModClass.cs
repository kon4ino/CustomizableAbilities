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
//using System.Reflection.Emit;
using Mono.Cecil.Cil;
//using UObject = UnityEngine.Object;

/* mod by ino_ (kon4ino), 1.3.1.0
 thank CharmChanger mod for some code */

namespace CustomizableNailDamage
{

    #region ЛОГИРОВАНИЕ
    public static class inoLog
    {
        private static bool enablelog = true;
        public static void log<T>(T messagetolog)
        {
            if (enablelog)
                Modding.Logger.Log(messagetolog);
        }
        public static void log<T>(T messagetolog, bool dolog)
        {
            if (enablelog && dolog)
            {
                Modding.Logger.Log(messagetolog);
            }
        }
    }
    #endregion ЛОГИРОВАНИЕ

    #region БИНДЫ
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
    #endregion БИНДЫ

    #region ГЛОБАЛЬНЫЕ НАСТРОЙКИ
    public class GlobalSettings
    {
        public bool EnableFloatNailDamage = false;
        public bool EnableMod = true;
        public bool DisplayNailDamage = false;
        public bool HealEnemiesOverMaxHP = true;
        public int NailUpgrade = 0;

        [JsonConverter(typeof(PlayerActionSetConverter))]
        public KeyBinds keybinds = new KeyBinds();
    }
    #endregion ГЛОБАЛЬНЫЕ НАСТРОЙКИ

    #region ЛОКАЛЬНЫЕ НАСТРОЙКИ
    public class LocalSettings
    {
        public int CustomIntNailDamage = PlayerData.instance.nailDamage;
        public float CustomFloatNailDamage = 0.1f;
        public float ModifiedNailDamage = 0.0f;
        public int SoulGain = 11;
        public int ReserveSoulGain = 6;
        public int StoredNailDamage = 0;
        public bool HasStoredValue = false;
        public bool firstLoading = true;
    }
    #endregion ЛОКАЛЬНЫЕ НАСТРОЙКИ

    public class CustomizableNailDamage : Mod, ICustomMenuMod, IGlobalSettings<GlobalSettings>, ILocalSettings<LocalSettings>
    {
        #region SHIT HAPPENS
        public CustomizableNailDamage() : base("CustomizableNailDamage") { }
        public override string GetVersion() => "1.3.1.0";
        public static LocalSettings LS = new LocalSettings();
        public static GlobalSettings GS = new GlobalSettings();
        private Menu menuRef;
        private float damageRemainder = 0f; // ОСТАТОК ДЛЯ ДРОБНОГО ГВОЗДЯ
        private Dictionary<GameObject, int> enemyMaxHealth = new(); // СЛОВАРЬ В КОТОРОМ ХРАНЯТСЯ МАКС. ЗДОРОВЬЕ КАЖДОГО ВРАГА
        private Dictionary<GameObject, float> fractionalPool = new(); // СЛОВАРЬ В КОТОРОМ ХРАНЯТСЯ ОСТАТКИ ДРОБНОГО УРОНА ДЛЯ КАЖДОГО ВРАГА
        private bool isDamageFromThornsOfAgony = false;
        private bool isThornsOfAgonyDamagedOnce = false;
        Vector2 velocity;
        public bool ToggleButtonInsideMenu => true;

        private const int Charm_Strength = 25;
        private const int Charm_Fury = 6;
        private const int Charm_DashMaster = 31;
        #endregion SHIT HAPPENS

        #region ВКЛ/ВЫКЛ МОД
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
            IL.HeroController.SoulGain -= CustomSoulGain;

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
            IL.HeroController.SoulGain += CustomSoulGain;

            Log("Enabled");
        }
        #endregion ВКЛ/ВЫКЛ МОД

        #region СОХРАНЕНИЕ/ЗАГРУЗКА НАСТРОЕК
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
        #endregion СОХРАНЕНИЕ/ЗАГРУЗКА НАСТРОЕК

        #region ИЗМЕНЕНИЯ ПУЛА ДРОБНОГО ГВОЗДЯ
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
        #endregion ИЗМЕНЕНИЯ ПУЛА ДРОБНОГО ГВОЗДЯ

        #region ИЗМЕНЕНИЯ УРОНА ГВОЗДЯ
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
        #endregion ИЗМЕНЕНИЯ УРОНА ГВОЗДЯ

        #region ОТОБРАЖЕНИЕ УРОНА ГВОЗДЯ
        private string BuildDisplayText()
        {
            string baseDamage = "";
            if (!GS.EnableFloatNailDamage)
                baseDamage = $"Nail DMG: {LS.CustomIntNailDamage} ({LS.ModifiedNailDamage})";
            else
                baseDamage = $"Nail DMG: {LS.CustomFloatNailDamage} ({LS.ModifiedNailDamage})";

            return baseDamage;
        }
        #endregion ОТОБРАЖЕНИЕ УРОНА ГВОЗДЯ

        #region РАССЧЁТ УРОНА
        private float GetModifiedDamage(float baseDamage, string typeOfDamage)
        {
            float modified = baseDamage;

            switch (typeOfDamage)
            {
                case "nail":
                    if (PlayerData.instance.equippedCharms.Contains(Charm_Strength))
                        modified *= 1.5f;
                    if (PlayerData.instance.equippedCharms.Contains(Charm_Fury) && PlayerData.instance.health <= 1)
                        modified *= 1.75f;
                    break;

                case "artslash":
                    if (PlayerData.instance.equippedCharms.Contains(Charm_Fury) && PlayerData.instance.health <= 1)
                        modified *= 1.75f;
                    modified *= 2.5f;
                    break;

                case "artcyclone":
                    if (PlayerData.instance.equippedCharms.Contains(Charm_Fury) && PlayerData.instance.health <= 1)
                        modified *= 1.75f;
                    modified *= 1.25f;
                    break;

                case "sharpshadow":
                    if (PlayerData.instance.equippedCharms.Contains(Charm_DashMaster))
                        modified *= 1.5f;
                    break;

                case "beam":
                    modified *= 0.5f;
                    if (PlayerData.instance.equippedCharms.Contains(Charm_Strength))
                        modified *= 1.5f;
                    if (PlayerData.instance.equippedCharms.Contains(Charm_Fury) && PlayerData.instance.health <= 1)
                        modified *= 1.5f;
                    break;

                default:
                    inoLog.log("GetModifiedDamage: undefined type of damage");
                    break;
            }

            return modified;
        }
        #endregion РАССЧЁТ УРОНА

        #region МЕНЮ
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
                                LS.ModifiedNailDamage = GetModifiedDamage(LS.CustomFloatNailDamage, "nail");
                            else
                                LS.ModifiedNailDamage = Mathf.RoundToInt(GetModifiedDamage(LS.CustomIntNailDamage, "nail"));
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
                                LS.ModifiedNailDamage = Mathf.RoundToInt(GetModifiedDamage(vanillaDmg, "nail"));
                            }
                        },

                        loadSetting: () => GS.NailUpgrade
                    ),

                    Blueprints.IntInputField // УРОН ЦЕЛОГО ГВОЗДЯ
                    (
                        "Custom Nail Damage",
                        naildmg =>
                        {
                            naildmg = Mathf.Clamp(naildmg, -9999, 9999);
                            UpdateNailDamage(naildmg);
                            OnSaveLocal();

                            if (!GS.EnableFloatNailDamage)
                            {
                                LS.ModifiedNailDamage = Mathf.RoundToInt(GetModifiedDamage(LS.CustomIntNailDamage, "nail"));
                            }

                            var inputFieldElement = (InputField)menuRef.Find("NailDamage_Input");
                            if (inputFieldElement != null)
                            {
                                inputFieldElement.userInput = LS.CustomIntNailDamage.ToString();
                            }
                        },
                        () => LS.CustomIntNailDamage,
                        21,
                        "DMG",
                        5,
                        Id: "NailDamage_Input"
                    ),

                    new HundredthSlider // УРОН ДРОБНОГО ГВОЗДЯ
                    (
                        name: "Custom Float Nail Damage",
                        storeValue: val =>
                        {
                            float rounded = Mathf.Round(val * 100f) / 100f;
                            LS.CustomFloatNailDamage = rounded;
                            if (GS.EnableFloatNailDamage)
                                LS.ModifiedNailDamage = GetModifiedDamage(LS.CustomFloatNailDamage, "nail");
                        },

                        loadValue: () => LS.CustomFloatNailDamage,
                        minValue: 0.01f,
                        maxValue: 0.99f,
                        wholeNumbers: false
                    ),

                    Blueprints.IntInputField // КОЛИЧЕСТВО ДУШИ ПОЛУЧАЕМОЙ С УДАРА
                    (
                        "Main Vessel Soul Gain",
                        soulgain =>
                        {
                            soulgain = Mathf.Clamp(soulgain, 0, 198);

                            LS.SoulGain = soulgain;
                            OnSaveLocal();

                            var inputFieldElement = (InputField)menuRef.Find("SoulGain_Input");
                            if (inputFieldElement != null)
                            {
                                inputFieldElement.userInput = LS.SoulGain.ToString();
                            }
                        },
                        () => LS.SoulGain,
                        11,
                        "VAL",
                        3,
                        Id: "SoulGain_Input"
                    ),

                    Blueprints.IntInputField // КОЛИЧЕСТВО РЕЗЕРВНОЙ ДУШИ ПОЛУЧАЕМОЙ С УДАРА
                    (
                        "Reserve Vessel Soul Gain",
                        soulgain =>
                        {
                            soulgain = Mathf.Clamp(soulgain, 0, 198);

                            LS.ReserveSoulGain = soulgain;
                            OnSaveLocal();

                            var inputFieldElement = (InputField)menuRef.Find("ReserveSoulGain_Input");
                            if (inputFieldElement != null)
                            {
                                inputFieldElement.userInput = LS.ReserveSoulGain.ToString();
                            }
                        },
                        () => LS.ReserveSoulGain,
                        6,
                        "VAL",
                        3,
                        Id: "ReserveSoulGain_Input"
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
                    ),

                    new MenuButton
                    (
                        name: "Reset Defaults",
                        description: "Nail damage will be set to Pure (21)",
                        submitAction: (_) =>
                        {
                            if (GS.EnableMod)
                            {
                                GS.EnableFloatNailDamage = false;
                                GS.DisplayNailDamage = true;
                                LS.CustomFloatNailDamage = 0.1f;
                                GS.NailUpgrade = 0;
                                GS.HealEnemiesOverMaxHP = true;

                                LS.CustomIntNailDamage = 21;
                                LS.ModifiedNailDamage = GetModifiedDamage(LS.CustomIntNailDamage, "nail");
                                LS.SoulGain = 11;
                                LS.ReserveSoulGain = 6;

                                GS.keybinds.IncreaseNailDamage.ClearBindings();
                                GS.keybinds.DecreaseNailDamage.ClearBindings();
                                GS.keybinds.ToggleDisplay.ClearBindings();

                                GS.keybinds.IncreaseNailDamage.AddDefaultBinding(Key.U);
                                GS.keybinds.DecreaseNailDamage.AddDefaultBinding(Key.I);
                                GS.keybinds.ToggleDisplay.AddDefaultBinding(Key.O);

                                OnSaveGlobal();
                                OnSaveLocal();
                                menuRef?.Update();
                            }
                        }
                    )

                });
            }

            return menuRef.GetMenuScreen(modListMenu);
        }
        #endregion МЕНЮ

        #region ПОДКЛЮЧЕНИЕ ХУКОВ
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
            IL.HeroController.SoulGain += CustomSoulGain;

            Log("Initialized");
        }
        #endregion ПОДКЛЮЧЕНИЕ ХУКОВ

        #region ПОЛУЧАЕМОЕ КОЛИЧЕСТВО ДУШИ
        private void CustomSoulGain(ILContext il)
        {
            if (!GS.EnableMod) return;
            ILCursor cursor = new ILCursor(il).Goto(0);

            cursor.TryGotoNext(i => i.MatchLdcI4(11));
            cursor.GotoNext();
            cursor.EmitDelegate<Func<int, int>>(soul => LS.SoulGain);

            cursor.TryGotoNext(i => i.MatchLdcI4(6));
            cursor.GotoNext();
            cursor.EmitDelegate<Func<int, int>>(soul => LS.ReserveSoulGain);
        }
        #endregion ПОЛУЧАЕМОЕ КОЛИЧЕСТВО ДУШИ

        #region МЕНЯЮ МНОЖИТЕЛИ УРОНА ГВОЗДЯ У АМУЛЕТОВ, ДАБЫ ОНИ НЕ КОНФЛИКТОВАЛИ С СИСТЕМОЙ
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
        #endregion МЕНЯЮ МНОЖИТЕЛИ УРОНА ГВОЗДЯ У АМУЛЕТОВ, ДАБЫ ОНИ НЕ КОНФЛИКТОВАЛИ С СИСТЕМОЙ

        #region УРОН ЩИТА ГРЁЗ
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
                inoLog.log($"DreamShieldDamage: {self.hp}, {self.State.Name}, {self.Fsm.GameObject.name}", false);

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

                    UpdatePool(enemy.gameObject, LS.CustomFloatNailDamage);
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

                inoLog.log($"DreamShieldDamage, enemy hp: {hm.hp}, damage taken: {self.Fsm.Variables.GetFsmInt("Damage")}", false);
            }

            orig(self);
        }
        #endregion УРОН ЩИТА ГРЁЗ

        #region УРОН КОЛЮЧЕК СТРАДАНИЙ
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
        #endregion УРОН КОЛЮЧЕК СТРАДАНИЙ

        #region ПРИ СОХРАНЕНИИ/ЗАГРУЗКЕ
        private void OnSaveSaved(SaveGameData data)
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
                inoLog.log($"OnSaveLoaded, damage set to: {PlayerData.instance.nailDamage}");
            }
        }
        #endregion ПРИ СОХРАНЕНИИ/ЗАГРУЗКЕ

        #region ПРИ НАНЕСЕНИИ УРОНА ВРАГУ
        public void OnEnemyDamaged(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (!GS.EnableMod)
            {
                orig(self, hitInstance);
                return;
            }

            string attackerName = hitInstance.Source?.name ?? "";
            inoLog.log($"OnEnemyDamaged: {attackerName}, {hitInstance.Source}, {hitInstance.SpecialType}", false);
            inoLog.log($"OnEnemyDamaged, attack type: {hitInstance.AttackType}", false);

            if (hitInstance.AttackType == AttackTypes.SharpShadow)
            {
                var enemy = self.gameObject.GetComponent<HealthManager>();
                hitInstance.DamageDealt = 1;

                if (!GS.EnableFloatNailDamage) // УРОН +/- ТЕНИ
                {
                    int dashDamage = Mathf.RoundToInt(GetModifiedDamage(LS.CustomIntNailDamage, "sharpshadow"));
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
                    float dashDamage = GetModifiedDamage(LS.CustomFloatNailDamage, "sharpshadow");
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
                    int nailBeamDamage = Mathf.RoundToInt(GetModifiedDamage(LS.CustomIntNailDamage, "beam"));
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

                else // УРОН ДРОБНОЙ ВОЛНЫ ЭЛЕГИИ КУКОЛКИ
                {
                    float nailBeamDamage = GetModifiedDamage(LS.CustomFloatNailDamage, "beam");
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
                //HeroController.instance.SetMPCharge(0);
                //HeroController.instance.AddMPCharge(LS.SoulGain);
                if (attackerName == "Great Slash" || attackerName == "Dash Slash")
                {
                    var enemy = self.gameObject.GetComponent<HealthManager>();

                    if (!GS.EnableFloatNailDamage)
                    {
                        int nailArtDamage = Mathf.RoundToInt(GetModifiedDamage(LS.CustomIntNailDamage, "artslash"));
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
                        float nailArtDamage = GetModifiedDamage(LS.CustomFloatNailDamage, "artslash");
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
                        int nailArtDamage = Mathf.RoundToInt(GetModifiedDamage(LS.CustomIntNailDamage, "artcyclone"));
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
                        float nailArtDamage = GetModifiedDamage(LS.CustomFloatNailDamage, "artcyclone");
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
                        int nailDamage = Mathf.RoundToInt(GetModifiedDamage(LS.CustomIntNailDamage, "nail"));
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
                        float nailDamage = GetModifiedDamage(LS.CustomFloatNailDamage, "nail");
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
                        UpdatePool(enemy.gameObject, LS.CustomFloatNailDamage);
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
                        UpdatePool(enemy.gameObject, LS.CustomFloatNailDamage);
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

            inoLog.log($"OnEnemyDamaged, damage: {hitInstance.DamageDealt}", false);
            orig(self, hitInstance);
        }
        #endregion ПРИ НАНЕСЕНИИ УРОНА ВРАГУ

        #region ОБНОВЛЕНИЯ СЛОВАРЕЙ
        private void OnReceiveDeathEvent(EnemyDeathEffects enemyDeathEffects, bool eventAlreadyReceived, ref float? attackDirection, ref bool resetDeathEvent, ref bool spellBurn, ref bool isWatery)
        {
            if (!GS.EnableMod) return;
            if (enemyDeathEffects == null) return;

            GameObject corpse = enemyDeathEffects.gameObject;
            enemyMaxHealth.Remove(corpse);
            fractionalPool.Remove(corpse);

            inoLog.log($"OnReceiveDeathEvent: removed {corpse.name} from dictionaries — enemy is dead", false);
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
                inoLog.log($"OnEnableEnemy: {enemy.name} with max HP: {hm.hp}", false);
            }

            return isDead;
        }
        #endregion ОБНОВЛЕНИЯ СЛОВАРЕЙ

        #region ПРИ СМЕНЕ СЦЕНЫ
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
        #endregion ПРИ СМЕНЕ СЦЕНЫ

        #region ПРИ ИЗМЕНЕНИИ ГЕРОЯ
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
                    inoLog.log($"OnHeroUpdate, damage set to: {LS.StoredNailDamage}");
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
                    float modified = GetModifiedDamage(LS.CustomIntNailDamage, "nail");
                    LS.ModifiedNailDamage = Mathf.RoundToInt(modified);
                }
                else
                    LS.ModifiedNailDamage = GetModifiedDamage(LS.CustomFloatNailDamage, "nail");
                ModDisplay.Instance.Display(BuildDisplayText());
            }
            else
                ModDisplay.Instance?.Destroy();
            // УБОГАЯ ЛОГИКА ОТОБРАЖЕНИЯ ДИСПЛЕЯ -----

            if (GS.keybinds.IncreaseNailDamage.WasPressed)
            {
                IncreaseNailDamage(1);
                float modified = GetModifiedDamage(LS.CustomIntNailDamage, "nail");
                LS.ModifiedNailDamage = Mathf.RoundToInt(modified);
            }

            if (GS.keybinds.DecreaseNailDamage.WasPressed)
            {
                DecreaseNailDamage(1);
                float modified = GetModifiedDamage(LS.CustomIntNailDamage, "nail");
                LS.ModifiedNailDamage = Mathf.RoundToInt(modified);
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
        #endregion ПРИ ИЗМЕНЕНИИ ГЕРОЯ
    }
}