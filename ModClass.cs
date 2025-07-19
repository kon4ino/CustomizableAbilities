using GlobalEnums;
using HKMirror;
using HKMirror.Hooks.ILHooks;
using HKMirror.Hooks.OnHooks;
using HKMirror.Reflection.SingletonClasses;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using IL;
using InControl;
using Modding;
using Modding.Converters;
//using System.Reflection.Emit;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Newtonsoft.Json;
using On;
using On.HutongGames.PlayMaker;
using Satchel;
using Satchel.BetterMenus;
using Satchel.Futils;
using SFCore.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
//using UObject = UnityEngine.Object;

/* mod by ino_ (kon4ino), 1.3.3.1
 thank CharmChanger mod for some code */

namespace CustomizableAbilities
{

    #region ЛОГИРОВАНИЕ
    public static class inoLog
    {
        private static bool enablelog = false;
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
        public bool EnableDisplay = false;
        public bool DisplayNailDamage = false;
        public bool DisplayNailSoulGain = false;
        public bool DisplayNailCooldown = false;
        public bool DisplayDreamNailDamage = false;
        public bool DisplayDreamNailSoulGain = false;
        public bool DisplaySpellsDamage = false;
        public bool HealEnemiesOverMaxHP = true;

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
        public float CustomNailCooldown = 0.41f;
        public int NailUpgrade = 0;
        public int SoulGain = 11;
        public int ReserveSoulGain = 6;
        public int StoredNailDamage = 0;
        public bool HasStoredValue = false;
        public bool firstLoading = true;
        public int CustomVengefulSpiritDamage = 15;
        public int CustomDesolateDiveDamage = 35;
        public int CustomHowlingWraithsDamage = 39;
        public int CustomShadeSoulDamage = 30;
        public int CustomDescendingDarkDamage = 60; // ИЛИ 65
        public int CustomAbyssShriekDamage = 80;
        public float CustomDreamNailDamage = 0f;
        public int CustomDreamNailSoulGain = 33;
    }
    #endregion ЛОКАЛЬНЫЕ НАСТРОЙКИ

    public class CustomizableAbilities : Mod, ICustomMenuMod, IGlobalSettings<GlobalSettings>, ILocalSettings<LocalSettings>
    {
        #region SHIT HAPPENS
        public CustomizableAbilities() : base("CustomizableAbilities") { }
        public override string GetVersion() => "1.3.3.1";
        public static LocalSettings LS = new LocalSettings();
        public static GlobalSettings GS = new GlobalSettings();
        private Menu menuRef;
        private Menu nailMenu;
        private Menu dreamNailMenu;
        private Menu spellsMenu;
        private float damageRemainder = 0f; // ОСТАТОК ДЛЯ ДРОБНОГО ГВОЗДЯ
        private Dictionary<GameObject, int> enemyMaxHealth = new(); // СЛОВАРЬ В КОТОРОМ ХРАНЯТСЯ МАКС. ЗДОРОВЬЕ КАЖДОГО ВРАГА
        private Dictionary<GameObject, float> fractionalPool = new(); // СЛОВАРЬ В КОТОРОМ ХРАНЯТСЯ ОСТАТКИ ДРОБНОГО УРОНА ДЛЯ КАЖДОГО ВРАГА
        private bool isDamageFromThornsOfAgony = false;
        private bool isThornsOfAgonyDamagedOnce = false;
        Vector2 velocity;
        public bool ToggleButtonInsideMenu => true;

        #region КОНСТАНТЫ
        private const int Charm_Strength = 25;
        private const int Charm_Fury = 6;
        private const int Charm_DashMaster = 31;
        private const int Charm_ShamanStone = 19;
        private const int Charm_DreamWielder = 30;
        private const int Charm_SoulCatcher = 20;
        private const int Charm_SoulEater = 21;
        private const int Charm_DefendersCrest = 10;
        private const int Charm_Flukenest = 11;

        private const float Percentage_Dive_Desolate_Dive_Damage = 0.43f;
        private const float Percentage_Shockwave_Desolate_Dive_Damage = 0.57f;
        private const float Percentage_Howling_Wraiths_Damage_Per_Hit = 0.33f;
        private const float Percentage_Dive_Descending_Dark_Damage = 0.25f;
        private const float Percentage_Shockwave_1_Descending_Dark_Damage = 0.5f;
        private const float Percentage_Shockwave_2_Descending_Dark_Damage = 0.25f;
        private const float Percentage_Abyss_Shriek_Damage_Per_Hit = 0.25f;

        private const float Percentage_Fireball_Flukenest_Scaling = 0.27f;
        private const float Percentage_Fireball2_Flukenest_Scaling = 0.13f;
        private const float Percentage_Fireball_Flukenest_Shaman_Stone_Scaling = 1.25f;
        private const float Percentage_Fireball_Shaman_Stone_Scaling = 1.33f;
        private const float Percentage_DDive_Shaman_Stone_Scaling = 1.51f;
        private const float Percentage_DDark_Shaman_Stone_Scaling = 1.47f;
        private const float Percentage_SHeads_Shaman_Stone_Scaling = 1.5f;
        private const float Percentage_Fireball_Flukenest_Defenders_Crest_Scaling = 0.72f;
        private const float Percentage_Fireball2_Flukenest_Defenders_Crest_Scaling = 0.41f;

        private const float Percentage_Flukenest_Defenders_Crest_Shaman_Stone_Scaling = 0.75f;
        private const float Percentage_Quick_Slash_Nail_Cooldown_Scaling = 0.61f;
        private const float Percentage_Dream_Wielder_Soul_Gain_Scaling = 2f;

        private const int Fireball_Flukes_Amount = 9;
        private const int Fireball2_Flukes_Amount = 16;
        #endregion КОНСТАНТЫ
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
            On.HutongGames.PlayMaker.Actions.FloatMultiply.OnEnter -= SetStrengthDamage;
            On.HutongGames.PlayMaker.Actions.SetFsmFloat.OnEnter -= FuryNailScaling;
            On.HutongGames.PlayMaker.Actions.FloatMultiply.OnEnter -= FuryNailArtScaling;
            IL.HeroController.SoulGain -= CustomSoulGain;
            On.HutongGames.PlayMaker.Actions.SetFsmInt.OnEnter -= SpellsDamage;
            On.HutongGames.PlayMaker.Actions.FloatCompare.OnEnter -= QMegaDamage;
            On.SpellFluke.DoDamage -= OnFlukeDamage;
            On.HutongGames.PlayMaker.Actions.Wait.OnEnter -= FlukenestDefendersCrestDamage;
            ILHeroController.orig_DoAttack -= CustomNailCooldown;
            IL.HeroController.Attack -= CustomNailDuration;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= DreamNailDamage;
            IL.EnemyDreamnailReaction.RecieveDreamImpact -= DreamNailSoulGain;

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
            On.HutongGames.PlayMaker.Actions.FloatMultiply.OnEnter += SetStrengthDamage;
            On.HutongGames.PlayMaker.Actions.SetFsmFloat.OnEnter += FuryNailScaling;
            On.HutongGames.PlayMaker.Actions.FloatMultiply.OnEnter += FuryNailArtScaling;
            IL.HeroController.SoulGain += CustomSoulGain;
            On.HutongGames.PlayMaker.Actions.SetFsmInt.OnEnter += SpellsDamage;
            On.HutongGames.PlayMaker.Actions.FloatCompare.OnEnter += QMegaDamage;
            On.SpellFluke.DoDamage += OnFlukeDamage;
            On.HutongGames.PlayMaker.Actions.Wait.OnEnter += FlukenestDefendersCrestDamage;
            ILHeroController.orig_DoAttack += CustomNailCooldown;
            IL.HeroController.Attack += CustomNailDuration;
            On.EnemyDreamnailReaction.RecieveDreamImpact += DreamNailDamage;
            IL.EnemyDreamnailReaction.RecieveDreamImpact += DreamNailSoulGain;

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
            }
            else
            {
                PlayerData.instance.nailDamage = 1;
            }
            LS.CustomIntNailDamage = newValue;
            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        }
        private void IncreaseNailDamage(int newValue) // УВЕЛИЧЕНИЕ УРОНА ГВОЗДЯ
        {
            if (!(LS.CustomIntNailDamage < 1))
            {
                PlayerData.instance.nailDamage += newValue;
            }
            LS.CustomIntNailDamage += newValue;
            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        }
        private void DecreaseNailDamage(int newValue) // УМЕНЬШЕНИЕ УРОНА ГВОЗДЯ
        {
            if (PlayerData.instance.nailDamage - newValue > 0)
            {
                PlayerData.instance.nailDamage -= newValue;
            }
            else
            {
                PlayerData.instance.nailDamage = 1;
            }
            LS.CustomIntNailDamage -= newValue;
            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        }
        #endregion ИЗМЕНЕНИЯ УРОНА ГВОЗДЯ

        #region ОТОБРАЖЕНИЕ ДИСПЛЕЯ
        private int CalculateRoundedDamage(float baseDamage, params float[] multipliers)
        {
            float currentDamage = baseDamage;
            foreach (float multiplier in multipliers)
            {
                currentDamage = Mathf.RoundToInt(currentDamage * multiplier);
            }
            return Mathf.RoundToInt(currentDamage);
        }

        private string BuildDisplayText()
        {
            string displayText = "CustomizableAbilities:\n[--------------------------]\n";

            if (GS.DisplayNailDamage)
            {
                if (!GS.EnableFloatNailDamage)
                    displayText += $"nail damage: {LS.CustomIntNailDamage} ({LS.ModifiedNailDamage})\n";
                else
                    displayText += $"nail damage: {LS.CustomFloatNailDamage} ({LS.ModifiedNailDamage})\n";
            }
            if (GS.DisplayNailSoulGain)
            {
                displayText += $"nail soul gain: {LS.SoulGain}, {LS.ReserveSoulGain}\n";
            }
            if (GS.DisplayNailCooldown)
            {
                displayText += $"nail cooldown: {LS.CustomNailCooldown}\n";
            }
            if (GS.DisplayDreamNailSoulGain)
            {
                displayText += $"dream nail soul gain: {LS.CustomDreamNailSoulGain}\n";
            }
            if (GS.DisplayDreamNailDamage)
            {
                displayText += $"dream nail damage: {LS.CustomDreamNailDamage}\n";
            }
            if (GS.DisplaySpellsDamage)
            {
                if (PlayerData.instance.fireballLevel == 1)
                {
                    if (PlayerData.instance.equippedCharms.Contains(Charm_Flukenest))
                    {
                        if (PlayerData.instance.equippedCharms.Contains(Charm_DefendersCrest))
                        {
                            if (PlayerData.instance.equippedCharms.Contains(Charm_ShamanStone))
                            {
                                displayText += $"vengeful spirit damage: {CalculateRoundedDamage(LS.CustomVengefulSpiritDamage * Percentage_Fireball_Flukenest_Scaling * Fireball_Flukes_Amount * Percentage_Fireball_Flukenest_Shaman_Stone_Scaling * Percentage_Fireball_Flukenest_Defenders_Crest_Scaling)}\n";
                            }
                            else
                            {
                                displayText += $"vengeful spirit damage: {CalculateRoundedDamage(LS.CustomVengefulSpiritDamage * Percentage_Fireball_Flukenest_Scaling * Fireball_Flukes_Amount * Percentage_Fireball_Flukenest_Defenders_Crest_Scaling)}\n";
                            }
                        }

                        else
                        {
                            if (PlayerData.instance.equippedCharms.Contains(Charm_ShamanStone))
                            {
                                displayText += $"vengeful spirit damage: {CalculateRoundedDamage(LS.CustomVengefulSpiritDamage * Percentage_Fireball_Flukenest_Scaling * Percentage_Fireball_Flukenest_Shaman_Stone_Scaling * Fireball_Flukes_Amount)}\n";
                            }
                            else
                            {
                                displayText += $"vengeful spirit damage: {CalculateRoundedDamage(LS.CustomVengefulSpiritDamage * Percentage_Fireball_Flukenest_Scaling * Fireball_Flukes_Amount)}\n";
                            }
                        }
                    }

                    else
                    {
                        if (PlayerData.instance.equippedCharms.Contains(Charm_ShamanStone))
                        {
                            displayText += $"vengeful spirit damage: {CalculateRoundedDamage(LS.CustomVengefulSpiritDamage * Percentage_Fireball_Shaman_Stone_Scaling)}\n";
                        }
                        else
                        {
                            displayText += $"vengeful spirit damage: {LS.CustomVengefulSpiritDamage}\n";
                        }
                    }
                }
                else if (PlayerData.instance.fireballLevel == 2)
                {
                    if (PlayerData.instance.equippedCharms.Contains(Charm_Flukenest))
                    {
                        if (PlayerData.instance.equippedCharms.Contains(Charm_DefendersCrest))
                        {
                            if (PlayerData.instance.equippedCharms.Contains(Charm_ShamanStone))
                            {
                                displayText += $"shade soul damage: {CalculateRoundedDamage(LS.CustomShadeSoulDamage * Percentage_Fireball2_Flukenest_Scaling * Fireball2_Flukes_Amount * Percentage_Fireball_Flukenest_Shaman_Stone_Scaling * Percentage_Fireball2_Flukenest_Defenders_Crest_Scaling)}\n";
                            }
                            else
                            {
                                displayText += $"shade soul damage: {CalculateRoundedDamage(LS.CustomShadeSoulDamage * Percentage_Fireball2_Flukenest_Scaling * Fireball2_Flukes_Amount * Percentage_Fireball2_Flukenest_Defenders_Crest_Scaling)}\n";
                            }
                        }

                        else
                        {
                            if (PlayerData.instance.equippedCharms.Contains(Charm_ShamanStone))
                            {
                                displayText += $"shade soul damage: {CalculateRoundedDamage(LS.CustomShadeSoulDamage * Percentage_Fireball2_Flukenest_Scaling * Percentage_Fireball_Flukenest_Shaman_Stone_Scaling * Fireball2_Flukes_Amount)}\n";
                            }
                            else
                            {
                                displayText += $"shade soul damage: {CalculateRoundedDamage(LS.CustomShadeSoulDamage * Percentage_Fireball2_Flukenest_Scaling * Fireball2_Flukes_Amount)}\n";
                            }
                        }
                    }

                    else
                    {
                        if (PlayerData.instance.equippedCharms.Contains(Charm_ShamanStone))
                        {
                            displayText += $"shade soul damage: {CalculateRoundedDamage(LS.CustomShadeSoulDamage * Percentage_Fireball_Shaman_Stone_Scaling)}\n";
                        }
                        else
                        {
                            displayText += $"shade soul damage: {LS.CustomShadeSoulDamage}\n";
                        }
                    }
                }
                else
                {
                    displayText += "not enable fireball\n";
                }

                if (PlayerData.instance.quakeLevel == 1)
                {
                    if (PlayerData.instance.equippedCharms.Contains(Charm_ShamanStone))
                    {
                        displayText += $"desolate dive damage: {CalculateRoundedDamage(LS.CustomDesolateDiveDamage * Percentage_DDive_Shaman_Stone_Scaling)}\n";
                    }
                    else
                    {
                        displayText += $"desolate dive damage: {LS.CustomDesolateDiveDamage}\n";
                    }
                }
                else if (PlayerData.instance.quakeLevel == 2)
                {
                    if (PlayerData.instance.equippedCharms.Contains(Charm_ShamanStone))
                    {
                        displayText += $"descending dark damage: {CalculateRoundedDamage(LS.CustomDescendingDarkDamage * Percentage_DDark_Shaman_Stone_Scaling)}\n";
                    }
                    else
                    {
                        displayText += $"descending dark damage: {LS.CustomDescendingDarkDamage}\n";
                    }
                }
                else
                {
                    displayText += "not enable quake\n";
                }

                if (PlayerData.instance.screamLevel == 1)
                {
                    if (PlayerData.instance.equippedCharms.Contains(Charm_ShamanStone))
                    {
                        displayText += $"howling wraiths damage: {CalculateRoundedDamage(LS.CustomHowlingWraithsDamage * Percentage_SHeads_Shaman_Stone_Scaling)}\n";
                    }
                    else
                    {
                        displayText += $"howling wraiths damage: {LS.CustomHowlingWraithsDamage}\n";
                    }
                }
                else if (PlayerData.instance.screamLevel == 2)
                {
                    if (PlayerData.instance.equippedCharms.Contains(Charm_ShamanStone))
                    {
                        displayText += $"abyss shriek damage: {CalculateRoundedDamage(LS.CustomAbyssShriekDamage * Percentage_SHeads_Shaman_Stone_Scaling)}\n";
                    }
                    else
                    {
                        displayText += $"abyss shriek damage: {LS.CustomAbyssShriekDamage}\n";
                    }
                }
                else
                {
                    displayText += "not enable scream\n";
                }
            }

            displayText += "[--------------------------]";
            return displayText;
        }
        #endregion ОТОБРАЖЕНИЕ ДИСПЛЕЯ

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

        private float CalculateFlukeDamageInterval(int baseDamage, int customDamage)
        {
            float scaling = customDamage / baseDamage;
            int totalFlukeDamage = Mathf.RoundToInt(26 * scaling);
            int cloudDamage = Mathf.Max(totalFlukeDamage - 3, 1);
            float cloudDPS = cloudDamage / 2.3f;
            return 1f / cloudDPS;
        }
        #endregion РАССЧЁТ УРОНА

        #region МЕНЮ
        private Menu CreateNailMenu(MenuScreen parentMenu)
        {
            return nailMenu = new Menu("Nail Settings", new Element[]
            {
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

                new HorizontalOption // ВКЛ/ВЫКЛ ОТОБРАЖЕНИЕ ПОЛУЧАЕМОЙ МАНЫ
                    (
                        name: "Show Nail Soul Gain",
                        description: "Toggle display On/Off",
                        values: new[] { "Off", "On" },
                        applySetting: index =>
                        {
                            GS.DisplayNailSoulGain = index == 1;
                            OnSaveGlobal();
                        },

                        loadSetting: () => GS.DisplayNailSoulGain ? 1 : 0
                    ),

                new HorizontalOption // ВКЛ/ВЫКЛ ОТОБРАЖЕНИЕ ПЕРЕЗАРЯДКИ ГВОЗДЯ
                    (
                        name: "Show Nail Cooldown",
                        description: "Toggle display On/Off",
                        values: new[] { "Off", "On" },
                        applySetting: index =>
                        {
                            GS.DisplayNailCooldown = index == 1;
                            OnSaveGlobal();
                        },

                        loadSetting: () => GS.DisplayNailCooldown ? 1 : 0
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
                                OnSaveLocal();
                            }
                        },

                        loadSetting: () => LS.NailUpgrade
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
                        },
                        () => LS.CustomIntNailDamage,
                        21,
                        "DMG",
                        5
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
                            soulgain = Mathf.Max(0, Mathf.Clamp(soulgain, 0, 198));

                            LS.SoulGain = soulgain;
                            OnSaveLocal();
                        },
                        () => LS.SoulGain,
                        11,
                        "VAL",
                        4
                    ),

                    Blueprints.IntInputField // КОЛИЧЕСТВО РЕЗЕРВНОЙ ДУШИ ПОЛУЧАЕМОЙ С УДАРА
                    (
                        "Reserve Vessel Soul Gain",
                        ressoulgain =>
                        {
                            ressoulgain = Mathf.Max(0, Mathf.Clamp(ressoulgain, 0, 198));

                            LS.ReserveSoulGain = ressoulgain;
                            OnSaveLocal();
                        },
                        () => LS.ReserveSoulGain,
                        6,
                        "VAL",
                        4
                    ),

                    Blueprints.FloatInputField // ВРЕМЯ ПЕРЕЗАРЯДКИ ГВОЗДЯ
                    (
                        "Nail Cooldown (SEC)",
                        nailcd =>
                        {
                            nailcd = Mathf.Clamp(nailcd, -198f, 198f);

                            LS.CustomNailCooldown = nailcd;
                            OnSaveLocal();
                        },
                        () => LS.CustomNailCooldown,
                        0.41f,
                        "VAL",
                        4
                    ),

                    new MenuButton
                    (
                        name: "Reset Nail Settings To Defaults",
                        description: "Nail damage will be set to Pure (21)",
                        submitAction: (_) =>
                        {
                            if (GS.EnableMod)
                            {
                                GS.EnableFloatNailDamage = false;
                                GS.DisplayNailDamage = true;
                                GS.DisplayNailSoulGain = true;
                                GS.DisplayNailCooldown = true;

                                LS.CustomFloatNailDamage = 0.1f;
                                LS.CustomNailCooldown = 0.41f;
                                LS.NailUpgrade = 0;
                                UpdateNailDamage(21);
                                LS.ModifiedNailDamage = GetModifiedDamage(LS.CustomIntNailDamage, "nail");
                                LS.SoulGain = 11;
                                LS.ReserveSoulGain = 6;

                                OnSaveGlobal();
                                OnSaveLocal();
                                nailMenu?.Update();
                            }
                        }
                    )
            });
        }
        private Menu CreateDreamNailMenu(MenuScreen parentMenu)
        {
            return dreamNailMenu = new Menu("Dream Nail Settings", new Element[]
            {
                new HorizontalOption // ВКЛ/ВЫКЛ ОТОБРАЖЕНИЕ КОЛИЧЕСТВА ДУШИ ПОЛУЧАЕМОЙ С УДАРА ГВОЗДЯ ГРЁЗ
                    (
                        name: "Show Dream Nail Soul Gain",
                        description: "Toggle display On/Off",
                        values: new[] { "Off", "On" },
                        applySetting: index =>
                        {
                            GS.DisplayDreamNailSoulGain = index == 1;
                            OnSaveGlobal();
                        },

                        loadSetting: () => GS.DisplayDreamNailSoulGain ? 1 : 0
                    ),

                new HorizontalOption // ВКЛ/ВЫКЛ ОТОБРАЖЕНИЕ УРОНА ГВОЗДЯ ГРЁЗ
                    (
                        name: "Show Dream Nail Damage",
                        description: "Toggle display On/Off",
                        values: new[] { "Off", "On" },
                        applySetting: index =>
                        {
                            GS.DisplayDreamNailDamage = index == 1;
                            OnSaveGlobal();
                        },

                        loadSetting: () => GS.DisplayDreamNailDamage ? 1 : 0
                    ),

                Blueprints.IntInputField // КОЛИЧЕСТВО ДУШИ ПОЛУЧАЕМОЙ С УДАРА ГВОЗДЯ ГРЁЗ
                    (
                        "Dream Nail Soul Gain",
                        dnsoulgain =>
                        {
                            dnsoulgain = Mathf.Max(0, Mathf.Clamp(dnsoulgain, 0, 198));

                            LS.CustomDreamNailSoulGain = dnsoulgain;
                            OnSaveLocal();
                        },
                        () => LS.CustomDreamNailSoulGain,
                        33,
                        "VAL",
                        4
                    ),

                    Blueprints.FloatInputField // УРОН ГВОЗДЯ ГРЁЗ
                    (
                        "Dream Nail Damage",
                        dndamage =>
                        {
                            dndamage = Mathf.Clamp(dndamage, -9999f, 9999f);

                            LS.CustomDreamNailDamage = dndamage;
                            OnSaveLocal();
                        },
                        () => LS.CustomDreamNailDamage,
                        0f,
                        "DMG",
                        5
                    ),

                    new MenuButton
                    (
                        name: "Reset Dream Nail Settings To Defaults",
                        description: "",
                        submitAction: (_) =>
                        {
                            if (GS.EnableMod)
                            {
                                GS.DisplayDreamNailSoulGain = true;
                                GS.DisplayDreamNailDamage = true;

                                LS.CustomDreamNailSoulGain = 33;
                                LS.CustomDreamNailDamage = 0f;

                                OnSaveGlobal();
                                OnSaveLocal();
                                dreamNailMenu?.Update();
                            }
                        }
                    )
            });
        }
        private Menu CreateSpellsMenu(MenuScreen parentMenu)
        {
            return spellsMenu = new Menu("Spells Settings", new Element[]
            {
                new HorizontalOption // ВКЛ/ВЫКЛ ОТОБРАЖЕНИЕ УРОНА ЗАКЛИНАНИЙ
                    (
                        name: "Show Spells Damage",
                        description: "Toggle display On/Off",
                        values: new[] { "Off", "On" },
                        applySetting: index =>
                        {
                            GS.DisplaySpellsDamage = index == 1;
                            OnSaveGlobal();
                        },

                        loadSetting: () => GS.DisplaySpellsDamage ? 1 : 0
                    ),

                Blueprints.IntInputField // УРОН МСТИТЕЛЬНОГО ДУХА
                    (
                        "Vengeful Spirit Damage",
                        vspiritdmg =>
                        {
                            vspiritdmg = Mathf.Clamp(vspiritdmg, 1, 9999);
                            LS.CustomVengefulSpiritDamage = vspiritdmg;
                            OnSaveLocal();
                        },
                        () => LS.CustomVengefulSpiritDamage,
                        15,
                        "DMG",
                        5
                    ),
                Blueprints.IntInputField // УРОН ОПУСТОШАЮЩЕГО ПИКЕ
                    (
                        "Desolate Dive Damage",
                        ddivedmg =>
                        {
                            ddivedmg = Mathf.Clamp(ddivedmg, 1, 9999);
                            LS.CustomDesolateDiveDamage = ddivedmg;
                            OnSaveLocal();
                        },
                        () => LS.CustomDesolateDiveDamage,
                        35,
                        "DMG",
                        5
                    ),
                Blueprints.IntInputField // УРОН ВОЮЩИХ ДУХОВ
                    (
                        "Howling Wraiths Damage",
                        hwraithsdmg =>
                        {
                            hwraithsdmg = Mathf.Clamp(hwraithsdmg, 1, 9999);
                            LS.CustomHowlingWraithsDamage = hwraithsdmg;
                            OnSaveLocal();
                        },
                        () => LS.CustomHowlingWraithsDamage,
                        39,
                        "DMG",
                        5
                    ),

                Blueprints.IntInputField // УРОН ТЕНЕВОЙ ДУШИ
                    (
                        "Shade Soul Damage",
                        ssouldmg =>
                        {
                            ssouldmg = Mathf.Clamp(ssouldmg, 1, 9999);
                            LS.CustomShadeSoulDamage = ssouldmg;
                            OnSaveLocal();
                        },
                        () => LS.CustomShadeSoulDamage,
                        30,
                        "DMG",
                        5
                    ),
                Blueprints.IntInputField // УРОН НИСХОДЯЩЕЙ ТЬМЫ
                    (
                        "Descending Dark Damage",
                        ddarkdmg =>
                        {
                            ddarkdmg = Mathf.Clamp(ddarkdmg, 1, 9999);
                            LS.CustomDescendingDarkDamage = ddarkdmg;
                            OnSaveLocal();
                        },
                        () => LS.CustomDescendingDarkDamage,
                        60,
                        "DMG",
                        5
                    ),
                Blueprints.IntInputField // УРОН ВОПЛЯ БЕЗДНЫ
                    (
                        "Abyss Shriek Damage",
                        ashriekdmg =>
                        {
                            ashriekdmg = Mathf.Clamp(ashriekdmg, 1, 9999);
                            LS.CustomAbyssShriekDamage = ashriekdmg;
                            OnSaveLocal();
                        },
                        () => LS.CustomAbyssShriekDamage,
                        80,
                        "DMG",
                        5
                    ),

                new MenuButton
                    (
                        name: "Reset Spells Settings To Defaults",
                        description: "",
                        submitAction: (_) =>
                        {
                            if (GS.EnableMod)
                            {
                                GS.DisplaySpellsDamage = true;

                                LS.CustomVengefulSpiritDamage = 15;
                                LS.CustomDesolateDiveDamage = 30;
                                LS.CustomHowlingWraithsDamage = 39;
                                LS.CustomShadeSoulDamage = 30;
                                LS.CustomDescendingDarkDamage = 60;
                                LS.CustomAbyssShriekDamage = 80;

                                OnSaveGlobal();
                                OnSaveLocal();
                                spellsMenu?.Update();
                            }
                        }
                    )
            });
        }

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            if (menuRef == null)
            {
                menuRef = new Menu("CustomizableAbilities", new Element[]
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

                    new HorizontalOption // ХИЛИТЬ ВРАГОВ ПОВЕРХ ИХ МАКСИМАЛЬНОГО ХП
                    (
                        name: "Heal Enemies Over Their Max HP",
                        description: "Toggle healing (negative) damage logic",
                        values: new[] { "Off", "On" },
                        applySetting: index =>
                        {
                            GS.HealEnemiesOverMaxHP = index == 1;
                            OnSaveGlobal();
                        },

                        loadSetting: () => GS.HealEnemiesOverMaxHP ? 1 : 0
                    ),

                    Blueprints.NavigateToMenu("Nail Settings", "", () => CreateNailMenu(modListMenu).GetMenuScreen(menuRef.GetMenuScreen(modListMenu))),
                    Blueprints.NavigateToMenu("Dream Nail Settings", "", () => CreateDreamNailMenu(modListMenu).GetMenuScreen(menuRef.GetMenuScreen(modListMenu))),
                    Blueprints.NavigateToMenu("Spells Settings", "", () => CreateSpellsMenu(modListMenu).GetMenuScreen(menuRef.GetMenuScreen(modListMenu))),

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
                        name: "Show/Hide Display Key",
                        playerAction: GS.keybinds.ToggleDisplay
                    ),

                    new MenuButton
                    (
                        name: "Reset Defaults",
                        description: "",
                        submitAction: (_) =>
                        {
                            if (GS.EnableMod)
                            {
                                GS.HealEnemiesOverMaxHP = true;

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
            Log("Initializing");

            base.Initialize(preloadedObjects);
            ModHooks.BeforeSavegameSaveHook += OnSaveSaved;
            ModHooks.AfterSavegameLoadHook += OnSaveLoaded;
            ModHooks.HeroUpdateHook += OnHeroUpdate;
            ModHooks.OnEnableEnemyHook += OnEnableEnemy;
            ModHooks.OnReceiveDeathEventHook += OnReceiveDeathEvent;
            On.HealthManager.TakeDamage += OnEnemyDamaged;
            On.SetHP.OnEnter += DreamShieldDamage;
            On.HutongGames.PlayMaker.Actions.SetFsmInt.OnEnter += ThornsOfAgonyDamage;
            On.HutongGames.PlayMaker.Actions.FloatMultiply.OnEnter += SetStrengthDamage;
            On.HutongGames.PlayMaker.Actions.SetFsmFloat.OnEnter += FuryNailScaling;
            On.HutongGames.PlayMaker.Actions.FloatMultiply.OnEnter += FuryNailArtScaling;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
            IL.HeroController.SoulGain += CustomSoulGain;
            On.HutongGames.PlayMaker.Actions.SetFsmInt.OnEnter += SpellsDamage;
            On.HutongGames.PlayMaker.Actions.FloatCompare.OnEnter += QMegaDamage;
            On.SpellFluke.DoDamage += OnFlukeDamage;
            On.HutongGames.PlayMaker.Actions.Wait.OnEnter += FlukenestDefendersCrestDamage;
            ILHeroController.orig_DoAttack += CustomNailCooldown;
            IL.HeroController.Attack += CustomNailDuration;
            On.EnemyDreamnailReaction.RecieveDreamImpact += DreamNailDamage;
            IL.EnemyDreamnailReaction.RecieveDreamImpact += DreamNailSoulGain;

            Log("Initialized");
        }
        #endregion ПОДКЛЮЧЕНИЕ ХУКОВ

        #region УРОН И КОЛИЧЕСТВО ПОЛУЧАЕМОЙ ДУШИ С ГВОЗДЯ ГРЁЗ
        private void DreamNailDamage(On.EnemyDreamnailReaction.orig_RecieveDreamImpact orig, EnemyDreamnailReaction self)
        {
            if (!GS.EnableMod || LS.CustomDreamNailDamage == 0f)
            {
                orig(self);
                return;
            }

            var enemy = self.gameObject.GetComponent<HealthManager>();
            UpdatePool(enemy.gameObject, LS.CustomDreamNailDamage);
            damageRemainder = GetPool(enemy.gameObject);
            int damageToDeal = 0;

            if (LS.CustomDreamNailDamage > 0f)
            {
                if (damageRemainder >= 1)
                {
                    while (damageRemainder >= 1)
                    {
                        damageToDeal++;
                        damageRemainder--;
                    }
                }
            }
            else
            {
                if (damageRemainder <= -1)
                {
                    while (damageRemainder <= -1)
                    {
                        damageToDeal--;
                        damageRemainder++;
                    }
                }
            }

            SetPool(enemy.gameObject, damageRemainder);
            if (damageToDeal < 0 && !GS.HealEnemiesOverMaxHP)
            {
                if (enemyMaxHealth.TryGetValue(enemy.gameObject, out int maxHp))
                {
                    if (enemy.hp - damageToDeal > maxHp)
                    {
                        damageToDeal += (enemy.hp - damageToDeal - maxHp);
                    }
                }
            }
            enemy.hp -= damageToDeal;
            if (enemy.hp <= 0)
            {
                enemy.Die(0, AttackTypes.Generic, true);
            }

            orig(self);
        }

        private void DreamNailSoulGain(ILContext il)
        {
            if (!GS.EnableMod) return;
            ILCursor cursor = new ILCursor(il).Goto(0);

            cursor.TryGotoNext(i => i.MatchLdcI4(33));
            cursor.GotoNext();
            cursor.EmitDelegate<Func<int, int>>(soul => LS.CustomDreamNailSoulGain);

            cursor.TryGotoNext(i => i.MatchLdcI4(66));
            cursor.GotoNext();
            cursor.EmitDelegate<Func<int, int>>(soul => Mathf.RoundToInt(LS.CustomDreamNailSoulGain * Percentage_Dream_Wielder_Soul_Gain_Scaling));
        }
        #endregion УРОН И КОЛИЧЕСТВО ПОЛУЧАЕМОЙ ДУШИ С ГВОЗДЯ ГРЁЗ

        #region ПЕРЕЗАРЯДКА ГВОЗДЯ
        private void CustomNailCooldown(ILContext il)
        {
            if (!GS.EnableMod) return;
            ILCursor cursor = new ILCursor(il).Goto(0);
            cursor.TryGotoNext(i => i.MatchLdfld<HeroController>("ATTACK_COOLDOWN_TIME_CH"));
            cursor.GotoNext();
            cursor.EmitDelegate<Func<float, float>>(time => LS.CustomNailCooldown * Percentage_Quick_Slash_Nail_Cooldown_Scaling);

            cursor.TryGotoNext(i => i.MatchLdfld<HeroController>("ATTACK_COOLDOWN_TIME"));
            cursor.GotoNext();
            cursor.EmitDelegate<Func<float, float>>(time => LS.CustomNailCooldown);
        }

        private void CustomNailDuration(ILContext il)
        {
            if (!GS.EnableMod) return;
            ILCursor cursor = new ILCursor(il).Goto(0);
            cursor.TryGotoNext(i => i.MatchLdfld<HeroController>("ATTACK_DURATION_CH"));
            cursor.GotoNext();
            cursor.EmitDelegate<Func<float, float>>(time => LS.CustomNailCooldown * Percentage_Quick_Slash_Nail_Cooldown_Scaling);

            cursor.TryGotoNext(i => i.MatchLdfld<HeroController>("ATTACK_DURATION"));
            cursor.GotoNext();
            cursor.EmitDelegate<Func<float, float>>(time => LS.CustomNailCooldown);
        }
        #endregion ПЕРЕЗАРЯДКА ГВОЗДЯ

        #region УРОН ТРЕМОГНЕЗДА
        private void OnFlukeDamage(On.SpellFluke.orig_DoDamage orig, SpellFluke self, GameObject obj, int upwardRecursionAmount, bool burst)
        {
            if (!GS.EnableMod)
            {
                orig(self, obj, upwardRecursionAmount, burst);
                return;
            }

            var enemy = obj.gameObject.GetComponent<HealthManager>();
            inoLog.log($"OnFlukeDamage, enemy hp: {enemy.hp}");
            if (PlayerData.instance.fireballLevel == 1)
            {
                if (PlayerData.instance.equippedCharms.Contains(Charm_ShamanStone))
                {
                    if (Mathf.RoundToInt(LS.CustomVengefulSpiritDamage * Percentage_Fireball_Flukenest_Scaling * Percentage_Fireball_Flukenest_Shaman_Stone_Scaling) != 5)
                    {
                        int totaldamage = Mathf.RoundToInt(LS.CustomVengefulSpiritDamage * Percentage_Fireball_Flukenest_Scaling * Percentage_Fireball_Flukenest_Shaman_Stone_Scaling) - 5;
                        enemy.hp -= totaldamage;
                    }
                }
                else
                {
                    if (Mathf.RoundToInt(LS.CustomVengefulSpiritDamage * Percentage_Fireball_Flukenest_Scaling) != 4)
                    {
                        int totaldamage = Mathf.RoundToInt(LS.CustomVengefulSpiritDamage * Percentage_Fireball_Flukenest_Scaling) - 4;
                        enemy.hp -= totaldamage;
                    }
                }
            }
            else if (PlayerData.instance.fireballLevel == 2)
            {
                if (PlayerData.instance.equippedCharms.Contains(Charm_ShamanStone))
                {
                    if (Mathf.RoundToInt(LS.CustomShadeSoulDamage * Percentage_Fireball2_Flukenest_Scaling * Percentage_Fireball_Flukenest_Shaman_Stone_Scaling) != 5)
                    {
                        int totaldamage = Mathf.RoundToInt(LS.CustomShadeSoulDamage * Percentage_Fireball2_Flukenest_Scaling * Percentage_Fireball_Flukenest_Shaman_Stone_Scaling) - 5;
                        enemy.hp -= totaldamage;
                    }
                }
                else
                {
                    if (Mathf.RoundToInt(LS.CustomShadeSoulDamage * Percentage_Fireball2_Flukenest_Scaling) != 4)
                    {
                        int totaldamage = Mathf.RoundToInt(LS.CustomShadeSoulDamage * Percentage_Fireball2_Flukenest_Scaling) - 4;
                        enemy.hp -= totaldamage;
                    }
                }
            }
            orig(self, obj, upwardRecursionAmount, burst);
        }

        private void FlukenestDefendersCrestDamage(On.HutongGames.PlayMaker.Actions.Wait.orig_OnEnter orig, Wait self)
        {
            if (!GS.EnableMod)
            {
                orig(self);
                return;
            }

            if (self.Fsm.GameObject.name == "Knight Dung Cloud" && self.Fsm.Name == "Control" && self.State.Name == "Collider On")
            {
                float interval = -1;

                if (PlayerData.instance.fireballLevel == 1 && LS.CustomVengefulSpiritDamage != 15)
                    interval = CalculateFlukeDamageInterval(15, LS.CustomVengefulSpiritDamage);
                else if (PlayerData.instance.fireballLevel == 2 && LS.CustomShadeSoulDamage != 30)
                    interval = CalculateFlukeDamageInterval(30, LS.CustomShadeSoulDamage);

                if (interval > 0f)
                {
                    if (PlayerDataAccess.equippedCharm_19)
                        interval *= Percentage_Flukenest_Defenders_Crest_Shaman_Stone_Scaling;

                    self.Fsm.GameObject.GetComponent<DamageEffectTicker>().SetDamageInterval(interval);
                }
            }

            orig(self);
        }
        #endregion УРОН ТРЕМОГНЕЗДА

        #region УРОН СПЕЛЛОВ
        private void SpellsDamage(On.HutongGames.PlayMaker.Actions.SetFsmInt.orig_OnEnter orig, SetFsmInt self)
        {
            if (!GS.EnableMod)
            {
                orig(self);
                return;
            }
            if (self.Fsm.GameObject.name == "Fireball(Clone)" && self.Fsm.Name == "Fireball Control" && self.State.Name == "Set Damage")
            {
                if (self.State.ActiveActionIndex == 2)
                {
                    self.setValue.Value = LS.CustomVengefulSpiritDamage;
                }
                else if (self.State.ActiveActionIndex == 4)
                {
                    self.setValue.Value = Mathf.RoundToInt(LS.CustomVengefulSpiritDamage * Percentage_Fireball_Shaman_Stone_Scaling);
                }
            }
            else if (self.Fsm.GameObject.name == "Fireball2 Spiral(Clone)" && self.Fsm.Name == "Fireball Control" && self.State.Name == "Set Damage")
            {
                if (self.State.ActiveActionIndex == 3)
                {
                    self.setValue.Value = LS.CustomShadeSoulDamage;
                }
                else if (self.State.ActiveActionIndex == 5)
                {
                    self.setValue.Value = Mathf.RoundToInt(LS.CustomShadeSoulDamage * Percentage_Fireball_Shaman_Stone_Scaling);
                }
            }
            else if (self.Fsm.Name == "Set Damage" && self.State.Name == "Set Damage")
            {
                if (self.Fsm.GameObject.transform.parent.gameObject.name == "Scr Heads")
                {
                    if (self.State.ActiveActionIndex == 0)
                    {
                        self.setValue.Value = Mathf.RoundToInt(LS.CustomHowlingWraithsDamage * Percentage_Howling_Wraiths_Damage_Per_Hit);
                    }
                    else if (self.State.ActiveActionIndex == 2)
                    {
                        self.setValue.Value = Mathf.RoundToInt(LS.CustomHowlingWraithsDamage * Percentage_Howling_Wraiths_Damage_Per_Hit * Percentage_SHeads_Shaman_Stone_Scaling);
                    }
                }
                else if (self.Fsm.GameObject.transform.parent.gameObject.name == "Scr Heads 2")
                {
                    if (self.State.ActiveActionIndex == 0)
                    {
                        self.setValue.Value = Mathf.RoundToInt(LS.CustomAbyssShriekDamage * Percentage_Abyss_Shriek_Damage_Per_Hit);
                    }
                    else if (self.State.ActiveActionIndex == 2)
                    {
                        self.setValue.Value = Mathf.RoundToInt(LS.CustomAbyssShriekDamage * Percentage_Abyss_Shriek_Damage_Per_Hit * Percentage_SHeads_Shaman_Stone_Scaling);
                    }
                }
                else if (self.Fsm.GameObject.transform.parent.gameObject.name == "Q Slam")
                {
                    if (self.State.ActiveActionIndex == 0)
                    {
                        self.setValue.Value = Mathf.RoundToInt(LS.CustomDesolateDiveDamage * Percentage_Shockwave_Desolate_Dive_Damage);
                    }
                    else if (self.State.ActiveActionIndex == 2)
                    {
                        self.setValue.Value = Mathf.RoundToInt(LS.CustomDesolateDiveDamage * Percentage_Shockwave_Desolate_Dive_Damage * Percentage_DDive_Shaman_Stone_Scaling);
                    }
                }
                else if (self.Fsm.GameObject.transform.parent.gameObject.name == "Q Slam 2")
                {
                    if (self.Fsm.GameObject.name == "Hit L")
                    {
                        if (self.State.ActiveActionIndex == 0)
                        {
                            self.setValue.Value = Mathf.RoundToInt(LS.CustomDescendingDarkDamage * Percentage_Shockwave_1_Descending_Dark_Damage) + 5;
                        }
                        else if (self.State.ActiveActionIndex == 2)
                        {
                            self.setValue.Value = Mathf.RoundToInt(LS.CustomDescendingDarkDamage * Percentage_Shockwave_1_Descending_Dark_Damage * Percentage_DDark_Shaman_Stone_Scaling) + 5;
                        }
                    }
                    else if (self.Fsm.GameObject.name == "Hit R")
                    {
                        if (self.State.ActiveActionIndex == 0)
                        {
                            self.setValue.Value = Mathf.RoundToInt(LS.CustomDescendingDarkDamage * Percentage_Shockwave_1_Descending_Dark_Damage);
                        }
                        else if (self.State.ActiveActionIndex == 2)
                        {
                            self.setValue.Value = Mathf.RoundToInt(LS.CustomDescendingDarkDamage * Percentage_Shockwave_1_Descending_Dark_Damage * Percentage_DDark_Shaman_Stone_Scaling);
                        }
                    }
                }
                else if (self.Fsm.GameObject.name == "Q Fall Damage")
                {
                    if (PlayerData.instance.quakeLevel == 1)
                    {
                        if (self.State.ActiveActionIndex == 0)
                        {
                            self.setValue.Value = Mathf.RoundToInt(LS.CustomDesolateDiveDamage * Percentage_Dive_Desolate_Dive_Damage);
                        }
                        else if (self.State.ActiveActionIndex == 2)
                        {
                            self.setValue.Value = Mathf.RoundToInt(LS.CustomDesolateDiveDamage * Percentage_Dive_Desolate_Dive_Damage * Percentage_DDive_Shaman_Stone_Scaling);
                        }
                    }
                    else if (PlayerData.instance.quakeLevel == 2)
                    {
                        if (self.State.ActiveActionIndex == 0)
                        {
                            self.setValue.Value = Mathf.RoundToInt(LS.CustomDescendingDarkDamage * Percentage_Dive_Descending_Dark_Damage);
                        }
                        else if (self.State.ActiveActionIndex == 2)
                        {
                            self.setValue.Value = Mathf.RoundToInt(LS.CustomDescendingDarkDamage * Percentage_Dive_Descending_Dark_Damage * Percentage_DDark_Shaman_Stone_Scaling);
                        }
                    }
                }
            }

            orig(self);
        }

        private void QMegaDamage(On.HutongGames.PlayMaker.Actions.FloatCompare.orig_OnEnter orig, FloatCompare self)
        {
            if (!GS.EnableMod)
            {
                orig(self);
                return;
            }
            if (self.Fsm.GameObject.name == "Q Mega" && self.Fsm.Name == "Hit Box Control" && self.State.Name == "Check Scale")
            {
                self.Fsm.GameObject.transform.Find("Hit L").gameObject.LocateMyFSM("damages_enemy").GetFsmIntVariable("damageDealt").Value = PlayerDataAccess.equippedCharm_19 ? Mathf.RoundToInt(LS.CustomDescendingDarkDamage * Percentage_Shockwave_2_Descending_Dark_Damage * Percentage_DDark_Shaman_Stone_Scaling) : Mathf.RoundToInt(LS.CustomDescendingDarkDamage * Percentage_Shockwave_2_Descending_Dark_Damage);
                self.Fsm.GameObject.transform.Find("Hit R").gameObject.LocateMyFSM("damages_enemy").GetFsmIntVariable("damageDealt").Value = PlayerDataAccess.equippedCharm_19 ? Mathf.RoundToInt(LS.CustomDescendingDarkDamage * Percentage_Shockwave_2_Descending_Dark_Damage * Percentage_DDark_Shaman_Stone_Scaling) : Mathf.RoundToInt(LS.CustomDescendingDarkDamage * Percentage_Shockwave_2_Descending_Dark_Damage);
            }

            orig(self);
        }
        #endregion УРОН СПЕЛЛОВ

        #region ПОЛУЧАЕМОЕ КОЛИЧЕСТВО ДУШИ
        private void CustomSoulGain(ILContext il)
        {
            if (!GS.EnableMod) return;
            ILCursor cursor = new ILCursor(il).Goto(0);

            cursor.TryGotoNext(i => i.MatchLdcI4(11));
            cursor.GotoNext();
            cursor.EmitDelegate<Func<int, int>>(soul => LS.SoulGain < 0 ? 0 : LS.SoulGain);

            cursor.TryGotoNext(i => i.MatchLdcI4(6));
            cursor.GotoNext();
            cursor.EmitDelegate<Func<int, int>>(soul => LS.SoulGain < 0 ? 0 : LS.SoulGain);
        }
        #endregion ПОЛУЧАЕМОЕ КОЛИЧЕСТВО ДУШИ

        #region МЕНЯЮ МНОЖИТЕЛИ УРОНА ГВОЗДЯ У АМУЛЕТОВ, ДАБЫ ОНИ НЕ КОНФЛИКТОВАЛИ С СИСТЕМОЙ
        private void SetStrengthDamage(On.HutongGames.PlayMaker.Actions.FloatMultiply.orig_OnEnter orig, FloatMultiply self)
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
        private void FuryNailScaling(On.HutongGames.PlayMaker.Actions.SetFsmFloat.orig_OnEnter orig, SetFsmFloat self)
        {
            if (!GS.EnableMod)
            {
                orig(self);
                return;
            }
            inoLog.log($"FuryNailScaling: {self.Fsm.GameObject.name}, {self.Fsm.Name}, {self.State.Name}", false);
            if (self.Fsm.GameObject.name == "Charm Effects" && self.Fsm.Name == "Fury" && self.State.Name == "Activate")
            {
                self.setValue.Value = 1f;
            }

            orig(self);
        }
        private void FuryNailArtScaling(On.HutongGames.PlayMaker.Actions.FloatMultiply.orig_OnEnter orig, FloatMultiply self)
        {
            if (!GS.EnableMod)
            {
                orig(self);
                return;
            }
            inoLog.log($"FuryNailArtScaling: {self.Fsm.GameObject.name}, {self.Fsm.Name}, {self.State.Name}", false);
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
            inoLog.log($"ThornsOfAgonyDamage: {self.Fsm.GameObject.name}, {self.Fsm.Name}, {self.State.Name}", false);
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

            inoLog.log($"OnEnemyDamaged, damage: {hitInstance.DamageDealt}");
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
                GS.EnableDisplay = !GS.EnableDisplay;
                OnSaveGlobal();

                if (!GS.EnableDisplay)
                    ModDisplay.Instance?.Destroy();
                else
                {
                    if (ModDisplay.Instance == null)
                        ModDisplay.Instance = new ModDisplay();
                    ModDisplay.Instance.Display(BuildDisplayText());
                }
            }

            if (GS.EnableDisplay)
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
            if (GS.EnableDisplay)
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