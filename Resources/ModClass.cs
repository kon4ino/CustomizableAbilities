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

/* mod by ino_ (kon4ino), 1.5.0.0
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
        public PlayerAction ToggleSettingsPanel;

        public KeyBinds()
        {
            ToggleFloatMode = CreatePlayerAction("Toggle Float Mode");
            IncreaseNailDamage = CreatePlayerAction("Increase Nail Damage");
            DecreaseNailDamage = CreatePlayerAction("Decrease Nail Damage");
            IncreaseNailDamage.AddDefaultBinding(Key.None);
            DecreaseNailDamage.AddDefaultBinding(Key.None);
            ToggleDisplay = CreatePlayerAction("Toggle Display");
            ToggleDisplay.AddDefaultBinding(Key.F5);
            ToggleSettingsPanel = CreatePlayerAction("Toggle Settings Panel");
            ToggleSettingsPanel.AddDefaultBinding(Key.F3);
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
        public bool DisplaySuperDashDamage = false;
        public bool DisplayFocusCost = false;
        public bool DisplayFocusTimeMultiplier = false;
        public bool HealEnemiesOverMaxHP = true;
        public float DisplayAnchorX = 0.02f;
        public float DisplayAnchorY = 0.23f;

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
        public bool IgnoreNailBinding = false;
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
        public float CustomSuperDashDamage = 10f;
        public int CustomFocusCost = 33;
        public float CustomFocusTimeMultiplier = 1f;
    }
    #endregion ЛОКАЛЬНЫЕ НАСТРОЙКИ

    public partial class CustomizableAbilities : Mod, ICustomMenuMod, IGlobalSettings<GlobalSettings>, ILocalSettings<LocalSettings>
    {
        #region SHIT HAPPENS
        public CustomizableAbilities() : base("CustomizableAbilities") { }
        public override string GetVersion() => "1.5.0.0";
        public static LocalSettings LS = new LocalSettings();
        public static GlobalSettings GS = new GlobalSettings();
        private Menu menuRef;
        private float damageRemainder = 0f; // ОСТАТОК ДЛЯ ДРОБНОГО ГВОЗДЯ
        private Dictionary<GameObject, int> enemyMaxHealth = new(); // СЛОВАРЬ В КОТОРОМ ХРАНЯТСЯ МАКС. ЗДОРОВЬЕ КАЖДОГО ВРАГА
        private Dictionary<GameObject, float> fractionalPool = new(); // СЛОВАРЬ В КОТОРОМ ХРАНЯТСЯ ОСТАТКИ ДРОБНОГО УРОНА ДЛЯ КАЖДОГО ВРАГА
        private bool isDamageFromThornsOfAgony = false;
        private bool isThornsOfAgonyDamagedOnce = false;
        private bool isNailDamageChanging = false;
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
        // ФЛАГ: ПОДПИСАНЫ ЛИ СЕЙЧАС ХУКИ (ЗАЩИТА ОТ ДВОЙНОЙ ПОДПИСКИ)
        private bool hooksSubscribed = false;

        private void SubscribeHooks()
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
            On.PlayMakerFSM.BroadcastEvent_FsmEvent += OnDebugModNailDamageChanging;
            On.HeroController.StartMPDrain += OnStartMPDrain;
            On.HeroController.CanFocus += OnCanFocus;
            ModHooks.GetPlayerIntHook += OnGetPlayerInt;
            On.HeroController.TakeMP += OnTakeMP;
            hooksSubscribed = true;
        }

        private void UnsubscribeHooks()
        {
            ModHooks.BeforeSavegameSaveHook -= OnSaveSaved;
            ModHooks.AfterSavegameLoadHook -= OnSaveLoaded;
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
            On.PlayMakerFSM.BroadcastEvent_FsmEvent -= OnDebugModNailDamageChanging;
            On.HeroController.StartMPDrain -= OnStartMPDrain;
            On.HeroController.CanFocus -= OnCanFocus;
            ModHooks.GetPlayerIntHook -= OnGetPlayerInt;
            On.HeroController.TakeMP -= OnTakeMP;
            hooksSubscribed = false;
        }

        public void DisableMod()
        {
            ModDisplay.Instance?.Destroy();
            if (hooksSubscribed)
                UnsubscribeHooks();

            Log("Disabled");
        }
        public void EnableMod()
        {
            // ЕСЛИ ХУКИ УЖЕ ВИСЯТ (НАПРИМЕР, МОД БЫЛ ВЫКЛЮЧЕН ПРИ ЗАПУСКЕ ИГРЫ),
            // СНАЧАЛА СНИМАЕМ ИХ, ЧТОБЫ НЕ ПОДПИСАТЬСЯ ДВАЖДЫ
            // IL-ХУКИ ПРИ ЭТОМ ПЕРЕПРИМЕНЯЮТСЯ УЖЕ С ВКЛЮЧЁННЫМ МОДОМ
            if (hooksSubscribed)
                UnsubscribeHooks();
            SubscribeHooks();

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

        #region ВРЕМЯ ФОКУСА ДЛЯ ДИСПЛЕЯ
        private const int Charm_QuickFocus = 7;
        private const int Charm_DeepFocus = 34;
        private const float Focus_Start_Time = 0.25f; // СТАРТОВАЯ АНИМАЦИЯ ФОКУСА, НЕ ЗАВИСИТ ОТ АМУЛЕТОВ И МНОЖИТЕЛЯ
        private static float deepFocusScaling = -1f; 

        // ВРЕМЯ ЛЕЧЕНИЯ (ОДИН ЦИКЛ ФОКУСА) В СЕКУНДАХ:
        // СТАРТ + 33 × ВРЕМЯ_НА_1_ДУШУ × DEEP_FOCUS × МНОЖИТЕЛЬ. ЦЕНА ФОКУСА НА ВРЕМЯ НЕ ВЛИЯЕТ
        private float GetFocusHealSeconds()
        {
            bool quickFocus = PlayerData.instance.equippedCharms.Contains(Charm_QuickFocus);
            bool deepFocus = PlayerData.instance.equippedCharms.Contains(Charm_DeepFocus);

            float timePerMP = quickFocus
                ? GetSpellControlFloat("Time Per MP Drain CH", 0.018f)
                : GetSpellControlFloat("Time Per MP Drain UnCH", 0.027f);

            if (deepFocus)
                timePerMP *= GetDeepFocusScaling();

            float multiplier = Mathf.Max(LS.CustomFocusTimeMultiplier, 0.01f);
            return Focus_Start_Time + Vanilla_Focus_Cost * timePerMP * multiplier;
        }

        private static float GetSpellControlFloat(string name, float fallback)
        {
            PlayMakerFSM fsm = HeroController.instance != null ? HeroController.instance.spellControl : null;
            var variable = fsm != null ? fsm.FsmVariables.GetFsmFloat(name) : null;
            return variable != null && variable.Value > 0f ? variable.Value : fallback;
        }

        private static float GetDeepFocusScaling()
        {
            if (deepFocusScaling > 0f)
                return deepFocusScaling;

            PlayMakerFSM fsm = HeroController.instance != null ? HeroController.instance.spellControl : null;
            if (fsm != null)
            {
                foreach (var state in fsm.FsmStates)
                {
                    if (state.Name != "Deep Focus Speed")
                        continue;

                    foreach (var action in state.Actions)
                    {
                        if (action is HutongGames.PlayMaker.Actions.FloatMultiply multiply && multiply.multiplyBy.Value > 0f)
                            return deepFocusScaling = multiply.multiplyBy.Value;
                    }
                }
            }

            return 1.65f;
        }
        #endregion ВРЕМЯ ФОКУСА ДЛЯ ДИСПЛЕЯ

        #region ИЗМЕНЕНИЯ ФОКУСА
        private const int Vanilla_Focus_Cost = 33;
        private bool freeFocusActive = false;
        private bool paidFocusActive = false;

        private int GetFocusCost() => Mathf.Clamp(LS.CustomFocusCost, 0, 999);
        private int GetTotalSoul() => PlayerDataAccess.MPCharge + PlayerDataAccess.MPReserve;

        private int OnGetPlayerInt(string name, int orig)
        {
            if (name == "focusMP_amount" && GS.EnableMod && GetFocusCost() != Vanilla_Focus_Cost)
                return Mathf.Min(GetFocusCost(), PlayerDataAccess.MPCharge);
            return orig;
        }

        private bool OnCanFocus(On.HeroController.orig_CanFocus orig, HeroController self)
        {
            bool canFocus = orig(self);
            if (!canFocus || !GS.EnableMod || HeroControllerR.drainMP)
                return canFocus;

            return GetTotalSoul() >= GetFocusCost();
        }

        private void OnStartMPDrain(On.HeroController.orig_StartMPDrain orig, HeroController self, float time)
        {
            if (!GS.EnableMod)
            {
                orig(self, time);
                return;
            }

            freeFocusActive = false;
            paidFocusActive = false;

            int cost = GetFocusCost();
            float multiplier = Mathf.Max(LS.CustomFocusTimeMultiplier, 0.01f);

            if (cost == Vanilla_Focus_Cost && multiplier == 1f)
            {
                orig(self, time);
                return;
            }

            int drainCost = Mathf.Max(cost, 1);

            float timePerMP = time * Vanilla_Focus_Cost / drainCost * multiplier;

            orig(self, timePerMP);
            HeroControllerR.focusMP_amount = drainCost;

            if (cost == 0)
            {
                freeFocusActive = true;
            }
            else if (GetTotalSoul() < cost)
            {
                self.StopMPDrain();
                inoLog.log($"Focus blocked: soul={GetTotalSoul()}, cost={cost}");
                return;
            }
            else
            {
                paidFocusActive = true;
            }

            inoLog.log($"Focus: time={time}, timePerMP={timePerMP}, cost={cost}");
        }

        private void OnTakeMP(On.HeroController.orig_TakeMP orig, HeroController self, int amount)
        {
            if (!GS.EnableMod || !HeroControllerR.drainMP)
            {
                orig(self, amount);
                return;
            }

            if (freeFocusActive)
                return;

            if (!paidFocusActive)
            {
                orig(self, amount);
                return;
            }

            int fromMain = Mathf.Min(amount, PlayerDataAccess.MPCharge);
            int fromReserve = Mathf.Min(amount - fromMain, PlayerDataAccess.MPReserve);

            if (fromMain > 0)
                orig(self, fromMain);
            if (fromReserve > 0)
                self.TakeReserveMP(fromReserve);
        }

        private void SettleFreeFocusRefund()
        {
            freeFocusActive = false;
        }
        #endregion ИЗМЕНЕНИЯ ФОКУСА

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
            // GetPool ВЕРНЁТ 0, ЕСЛИ ВРАГА ЕЩЁ НЕТ В СЛОВАРЕ
            fractionalPool[enemyName] = GetPool(enemyName) + value;
        }
        #endregion ИЗМЕНЕНИЯ ПУЛА ДРОБНОГО ГВОЗДЯ

        #region ИЗМЕНЕНИЯ УРОНА ГВОЗДЯ
        private void UpdateNailDamage(int newValue) // ОБНОВЛЕНИЕ УРОНА ГВОЗДЯ
        {
            isNailDamageChanging = true;
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
            isNailDamageChanging = false;
        }
        private void IncreaseNailDamage(int newValue) // УВЕЛИЧЕНИЕ УРОНА ГВОЗДЯ
        {
            isNailDamageChanging = true;
            if (!(LS.CustomIntNailDamage < 1))
            {
                PlayerData.instance.nailDamage += newValue;
            }
            LS.CustomIntNailDamage += newValue;
            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
            isNailDamageChanging = false;
        }
        private void DecreaseNailDamage(int newValue) // УМЕНЬШЕНИЕ УРОНА ГВОЗДЯ
        {
            isNailDamageChanging = true;
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
            isNailDamageChanging = false;
        }
        #endregion ИЗМЕНЕНИЯ УРОНА ГВОЗДЯ

        #region РАССЧЁТ УРОНА
        private float GetModifiedDamage(float baseDamage, string typeOfDamage)
        {
            float modified = baseDamage;

            switch (typeOfDamage)
            {
                case "nail":
                    if (isNailBindingActive() && !LS.IgnoreNailBinding)
                    {
                        modified *= 0.8f;
                        if (modified > 13)
                            modified = 13;
                    }
                    if (PlayerData.instance.equippedCharms.Contains(Charm_Strength))
                        if (!(isNailBindingActive() && !LS.IgnoreNailBinding))
                            modified *= 1.5f;
                    if (PlayerData.instance.equippedCharms.Contains(Charm_Fury) && PlayerData.instance.health <= 1)
                        modified *= 1.75f;
                    break;

                case "artslash":
                    if (isNailBindingActive() && !LS.IgnoreNailBinding)
                    {
                        modified *= 0.8f;
                        if (modified > 13)
                            modified = 13;
                    }
                    if (PlayerData.instance.equippedCharms.Contains(Charm_Fury) && PlayerData.instance.health <= 1)
                        modified *= 1.75f;
                    modified *= 2.5f;
                    break;

                case "artcyclone":
                    if (isNailBindingActive() && !LS.IgnoreNailBinding)
                    {
                        modified *= 0.8f;
                        if (modified > 13)
                            modified = 13;
                    }
                    if (PlayerData.instance.equippedCharms.Contains(Charm_Fury) && PlayerData.instance.health <= 1)
                        modified *= 1.75f;
                    modified *= 1.25f;
                    break;

                case "sharpshadow":
                    if (PlayerData.instance.equippedCharms.Contains(Charm_DashMaster))
                        modified *= 1.5f;
                    break;

                case "beam":
                    if (isNailBindingActive() && !LS.IgnoreNailBinding)
                    {
                        modified *= 0.8f;
                        if (modified > 13)
                            modified = 13;
                    }
                    modified *= 0.5f;
                    if (PlayerData.instance.equippedCharms.Contains(Charm_Strength))
                        if (!(isNailBindingActive() && !LS.IgnoreNailBinding))
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
            float scaling = (float)customDamage / baseDamage;
            int totalFlukeDamage = Mathf.RoundToInt(26 * scaling);
            int cloudDamage = Mathf.Max(totalFlukeDamage - 3, 1);
            float cloudDPS = cloudDamage / 2.3f;
            return 1f / cloudDPS;
        }
        #endregion РАССЧЁТ УРОНА

        #region АКТИВНА ЛИ ВЕРИГА ГВОЗДЯ
        private bool isNailBindingActive()
        {
            return BossSequenceController.BoundNail;
        }
        #endregion АКТИВНА ЛИ ВЕРИГА ГВОЗДЯ

        #region МЕНЮ
        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            if (menuRef == null)
            {
                menuRef = new Menu("CustomizableAbilities", new Element[]
                {
                    new TextPanel("All settings are in the settings panel:\npause the game and press the Settings Panel key (F3 by default)", 1200f, 26),

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

                    new KeyBind
                    (
                        name: "Settings Panel Key",
                        playerAction: GS.keybinds.ToggleSettingsPanel
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
                        name: "Show/Hide Display Key",
                        playerAction: GS.keybinds.ToggleDisplay
                    ),

                    new MenuButton
                    (
                        name: "Reset Defaults",
                        description: "Reset global settings and key binds (Settings Panel = F3, Display = F5)",
                        submitAction: (_) => ResetGlobalSettings()
                    )
                });
            }

            return menuRef.GetMenuScreen(modListMenu);
        }
        #endregion МЕНЮ

        #region ПОДКЛЮЧЕНИЕ ХУКОВ
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing...");

            base.Initialize(preloadedObjects);
            SubscribeHooks();
            ModSettingsPanel.Create(BuildSettingsTabs);
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;

            Log("Initialized.");
        }
        #endregion ПОДКЛЮЧЕНИЕ ХУКОВ

        #region СОГЛАСОВАНИЕ ИЗМЕНЕНИЯ УРОНА ГВОЗДЯ
        private void OnDebugModNailDamageChanging(On.PlayMakerFSM.orig_BroadcastEvent_FsmEvent orig, HutongGames.PlayMaker.FsmEvent fsmEvent)
        {
            if (!GS.EnableMod)
            {
                orig(fsmEvent);
                return;
            }
            orig(fsmEvent);
            if (fsmEvent.Name == "UPDATE NAIL DAMAGE" && !isNailDamageChanging)
            {
                if (PlayerData.instance.nailDamage != LS.CustomIntNailDamage)
                {
                    UpdateNailDamage(PlayerData.instance.nailDamage);
                    if (GS.EnableFloatNailDamage)
                    {
                        LS.ModifiedNailDamage = GetModifiedDamage(PlayerData.instance.nailDamage, "nail");
                    }
                }
                if (PlayerData.instance.nailDamage == 0)
                {
                    UpdateNailDamage(1);
                }
            }
        }
        #endregion СОГЛАСОВАНИЕ ИЗМЕНЕНИЯ УРОНА ГВОЗДЯ

        #region УРОН И КОЛИЧЕСТВО ПОЛУЧАЕМОЙ ДУШИ С ГВОЗДЯ ГРЁЗ
        private void DreamNailDamage(On.EnemyDreamnailReaction.orig_RecieveDreamImpact orig, EnemyDreamnailReaction self)
        {
            if (!GS.EnableMod || LS.CustomDreamNailDamage == 0f)
            {
                orig(self);
                return;
            }

            var enemy = self.gameObject.GetComponent<HealthManager>();
            int damageToDeal = TakeSignedWholeDamage(enemy, LS.CustomDreamNailDamage);
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

            if (!cursor.TryGotoNext(i => i.MatchLdcI4(33)))
            {
                LogError("DreamNailSoulGain: pattern (33) not found, patch skipped");
                return;
            }
            cursor.GotoNext();
            cursor.EmitDelegate<Func<int, int>>(soul => LS.CustomDreamNailSoulGain);

            if (!cursor.TryGotoNext(i => i.MatchLdcI4(66)))
            {
                LogError("DreamNailSoulGain: pattern (66) not found, patch skipped");
                return;
            }
            cursor.GotoNext();
            cursor.EmitDelegate<Func<int, int>>(soul => Mathf.RoundToInt(LS.CustomDreamNailSoulGain * Percentage_Dream_Wielder_Soul_Gain_Scaling));
        }
        #endregion УРОН И КОЛИЧЕСТВО ПОЛУЧАЕМОЙ ДУШИ С ГВОЗДЯ ГРЁЗ

        #region ПЕРЕЗАРЯДКА ГВОЗДЯ
        private void CustomNailCooldown(ILContext il)
        {
            if (!GS.EnableMod) return;
            ILCursor cursor = new ILCursor(il).Goto(0);
            if (!cursor.TryGotoNext(i => i.MatchLdfld<HeroController>("ATTACK_COOLDOWN_TIME_CH")))
            {
                LogError("CustomNailCooldown: pattern (ATTACK_COOLDOWN_TIME_CH) not found, patch skipped");
                return;
            }
            cursor.GotoNext();
            cursor.EmitDelegate<Func<float, float>>(time => LS.CustomNailCooldown * Percentage_Quick_Slash_Nail_Cooldown_Scaling);

            if (!cursor.TryGotoNext(i => i.MatchLdfld<HeroController>("ATTACK_COOLDOWN_TIME")))
            {
                LogError("CustomNailCooldown: pattern (ATTACK_COOLDOWN_TIME) not found, patch skipped");
                return;
            }
            cursor.GotoNext();
            cursor.EmitDelegate<Func<float, float>>(time => LS.CustomNailCooldown);
        }

        private void CustomNailDuration(ILContext il)
        {
            if (!GS.EnableMod) return;
            ILCursor cursor = new ILCursor(il).Goto(0);
            if (!cursor.TryGotoNext(i => i.MatchLdfld<HeroController>("ATTACK_DURATION_CH")))
            {
                LogError("CustomNailDuration: pattern (ATTACK_DURATION_CH) not found, patch skipped");
                return;
            }
            cursor.GotoNext();
            cursor.EmitDelegate<Func<float, float>>(time => LS.CustomNailCooldown * Percentage_Quick_Slash_Nail_Cooldown_Scaling);

            if (!cursor.TryGotoNext(i => i.MatchLdfld<HeroController>("ATTACK_DURATION")))
            {
                LogError("CustomNailDuration: pattern (ATTACK_DURATION) not found, patch skipped");
                return;
            }
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

            if (!cursor.TryGotoNext(i => i.MatchLdcI4(11)))
            {
                LogError("CustomSoulGain: pattern (11) not found, patch skipped");
                return;
            }
            cursor.GotoNext();
            cursor.EmitDelegate<Func<int, int>>(soul => LS.SoulGain < 0 ? 0 : LS.SoulGain);

            if (!cursor.TryGotoNext(i => i.MatchLdcI4(6)))
            {
                LogError("CustomSoulGain: pattern (6) not found, reserve soul patch skipped");
                return;
            }
            cursor.GotoNext();
            // ДУША, КОГДА ОСНОВНОЙ СОСУД ПОЛОН (ИДЁТ В РЕЗЕРВНЫЕ СОСУДЫ)
            cursor.EmitDelegate<Func<int, int>>(soul => LS.ReserveSoulGain < 0 ? 0 : LS.ReserveSoulGain);
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

                                if (appliedHeal != 0)
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
            inoLog.log($"OnEnemyDamaged: {attackerName}, {hitInstance.Source}, {hitInstance.SpecialType}");
            inoLog.log($"OnEnemyDamaged, attack type: {hitInstance.AttackType}");

            var enemy = self.gameObject.GetComponent<HealthManager>();

            if (hitInstance.AttackType == AttackTypes.SharpShadow)
            {
                ApplyNailDamage(enemy, ref hitInstance, "sharpshadow");
            }

            else if (hitInstance.AttackType == AttackTypes.NailBeam)
            {
                ApplyNailDamage(enemy, ref hitInstance, "beam");
            }

            else if (hitInstance.AttackType == AttackTypes.Nail)
            {
                if (attackerName == "Great Slash" || attackerName == "Dash Slash")
                    ApplyNailDamage(enemy, ref hitInstance, "artslash");
                else if (attackerName == "Hit L" || attackerName == "Hit R")
                    ApplyNailDamage(enemy, ref hitInstance, "artcyclone");
                else
                    ApplyNailDamage(enemy, ref hitInstance, "nail");
            }

            else
            {
                if (isThornsOfAgonyDamagedOnce) // ВТОРОЙ УДАР КОЛЮЧЕК СТРАДАНИЙ
                {
                    isThornsOfAgonyDamagedOnce = false;
                    ApplyThornsOfAgonyDamage(enemy, ref hitInstance);
                }

                else if (isDamageFromThornsOfAgony) // ПЕРВЫЙ УДАР КОЛЮЧЕК СТРАДАНИЙ
                {
                    isDamageFromThornsOfAgony = false;
                    isThornsOfAgonyDamagedOnce = true;
                    ApplyThornsOfAgonyDamage(enemy, ref hitInstance);
                }

                else if (attackerName == "SuperDash Damage")
                {
                    int damageToDeal = TakeSignedWholeDamage(enemy, LS.CustomSuperDashDamage);
                    enemy.hp -= damageToDeal - 1;
                    hitInstance.DamageDealt = 1;
                }
            }

            inoLog.log($"OnEnemyDamaged, damage: {hitInstance.DamageDealt}");
            orig(self, hitInstance);
        }

        // УРОН ГВОЗДЯ (ОБЫЧНЫЙ УДАР, ТЕХНИКИ, ЭЛЕГИЯ, ТЕНЕВОЙ РЫВОК)
        private void ApplyNailDamage(HealthManager enemy, ref HitInstance hitInstance, string typeOfDamage)
        {
            hitInstance.DamageDealt = 1;

            if (!GS.EnableFloatNailDamage)
            {
                int damage = Mathf.RoundToInt(GetModifiedDamage(LS.CustomIntNailDamage, typeOfDamage));
                if (LS.CustomIntNailDamage <= 0)
                    HealEnemy(enemy, damage);
                else
                    hitInstance.DamageDealt = damage;
            }
            else
            {
                AddFractionalDamage(enemy, ref hitInstance, GetModifiedDamage(LS.CustomFloatNailDamage, typeOfDamage));
            }
        }

        // УРОН КОЛЮЧЕК СТРАДАНИЙ. СЮДА ПОПАДАЕМ ТОЛЬКО ПРИ УРОНЕ < 1 ИЛИ В ДРОБНОМ РЕЖИМЕ:
        // В ЭТИХ СЛУЧАЯХ FSM КОЛЮЧЕК УЖЕ ВЫСТАВИЛ УРОН УДАРА 1
        private void ApplyThornsOfAgonyDamage(HealthManager enemy, ref HitInstance hitInstance)
        {
            if (!GS.EnableFloatNailDamage)
                HealEnemy(enemy, LS.CustomIntNailDamage);
            else
                AddFractionalDamage(enemy, ref hitInstance, LS.CustomFloatNailDamage);
        }

        // ОТРИЦАТЕЛЬНЫЙ УРОН: ЛЕЧИМ ВРАГА (С УЧЁТОМ НАСТРОЙКИ ЛЕЧЕНИЯ СВЕРХ МАКС. ХП)
        // И ВОЗВРАЩАЕМ 1 ХП, КОТОРЫЙ СНИМЕТ САМ УДАР
        private void HealEnemy(HealthManager enemy, int damage)
        {
            int totalHeal = Mathf.Abs(damage);

            if (GS.HealEnemiesOverMaxHP)
            {
                enemy.hp += totalHeal;
            }
            else if (enemyMaxHealth.TryGetValue(enemy.gameObject, out int maxHp))
            {
                int healable = Mathf.Max(0, maxHp - enemy.hp);
                int appliedHeal = Mathf.Min(totalHeal, healable);
                if (appliedHeal != 0)
                    enemy.hp += appliedHeal;
            }

            enemy.hp++;
        }

        // ДРОБНЫЙ УРОН: КОПИМ В ПУЛЕ ВРАГА, ЦЕЛУЮ ЧАСТЬ ДОБАВЛЯЕМ К УДАРУ,
        // И ВОЗВРАЩАЕМ 1 ХП, КОТОРЫЙ СНИМЕТ САМ УДАР
        private void AddFractionalDamage(HealthManager enemy, ref HitInstance hitInstance, float damage)
        {
            UpdatePool(enemy.gameObject, damage);
            damageRemainder = GetPool(enemy.gameObject);
            int damageToDeal = 0;

            while (damageRemainder >= 1)
            {
                damageToDeal++;
                damageRemainder--;
            }

            hitInstance.DamageDealt += damageToDeal;
            SetPool(enemy.gameObject, damageRemainder);
            enemy.hp++;
        }

        // ДРОБНЫЙ УРОН СО ЗНАКОМ (ГВОЗДЬ ГРЁЗ, КРИСТАЛЬНЫЙ РЫВОК): КОПИМ В ПУЛЕ ВРАГА
        // И ВОЗВРАЩАЕМ ЦЕЛУЮ ЧАСТЬ. ОТРИЦАТЕЛЬНОЕ ЗНАЧЕНИЕ — ЛЕЧЕНИЕ; ЕСЛИ ЛЕЧИТЬ СВЕРХ
        // МАКС. ХП НЕЛЬЗЯ, ЛЕЧЕНИЕ УРЕЗАЕТСЯ ДО МАКСИМУМА
        private int TakeSignedWholeDamage(HealthManager enemy, float damage)
        {
            UpdatePool(enemy.gameObject, damage);
            damageRemainder = GetPool(enemy.gameObject);
            int damageToDeal = 0;

            if (damage > 0f)
            {
                while (damageRemainder >= 1)
                {
                    damageToDeal++;
                    damageRemainder--;
                }
            }
            else
            {
                while (damageRemainder <= -1)
                {
                    damageToDeal--;
                    damageRemainder++;
                }
            }

            SetPool(enemy.gameObject, damageRemainder);
            if (damageToDeal < 0 && !GS.HealEnemiesOverMaxHP && enemyMaxHealth.TryGetValue(enemy.gameObject, out int maxHp))
            {
                if (enemy.hp - damageToDeal > maxHp)
                    damageToDeal += (enemy.hp - damageToDeal - maxHp);
            }

            return damageToDeal;
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

            UpdateDisplayBeforeInput(); // ДИСПЛЕЙ: СМ. Display.cs

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

            RefreshDisplay(); // ДИСПЛЕЙ: СМ. Display.cs
        }
        #endregion ПРИ ИЗМЕНЕНИИ ГЕРОЯ
    }
}