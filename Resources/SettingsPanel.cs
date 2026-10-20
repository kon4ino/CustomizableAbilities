using Modding;
using System;
using Modding;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace CustomizableAbilities
{
    #region МОДЕЛЬ НАСТРОЕК (ЧТО ПОКАЗЫВАТЬ В ОКНЕ)
    internal abstract class SettingItem
    {
        public string Label = "";
    }

    // ПЕРЕКЛЮЧАТЕЛЬ ON/OFF
    internal sealed class ToggleItem : SettingItem
    {
        public Func<bool> Get = () => false;
        public Action<bool> Set = _ => { };
    }

    // ЧИСЛО С КНОПКАМИ −/+ И ПОЛЕМ ВВОДА
    internal sealed class NumberItem : SettingItem
    {
        public Func<float> Get = () => 0f;
        public Action<float> Set = _ => { };
        public float Step = 1f;
        public float Min = float.MinValue;
        public float Max = float.MaxValue;
        public bool Integer = true;
        public string Format = "0.##";
    }

    // ВЫБОР ИЗ СПИСКА (СТРЕЛКИ < >)
    internal sealed class ChoiceItem : SettingItem
    {
        public string[] Options = Array.Empty<string>();
        public Func<int> Get = () => 0;
        public Action<int> Set = _ => { };
    }

    // КНОПКА ДЕЙСТВИЯ (НАПРИМЕР, СБРОС)
    internal sealed class ButtonItem : SettingItem
    {
        public Action Click = () => { };
    }

    // БИНД КЛАВИШИ: КЛИК — ЖДЁМ НАЖАТИЯ НОВОЙ КЛАВИШИ, BACKSPACE/DELETE — СНЯТЬ БИНД
    internal sealed class KeyBindItem : SettingItem
    {
        public Func<InControl.PlayerAction> GetAction = () => null;
        public Action Changed = () => { };
    }

    internal sealed class SettingsTab
    {
        public string Title = "";
        public List<SettingItem> Items = new();
    }
    #endregion МОДЕЛЬ НАСТРОЕК (ЧТО ПОКАЗЫВАТЬ В ОКНЕ)

    internal class ModSettingsPanel : MonoBehaviour
    {
        private static ModSettingsPanel instance;

        #region ОФОРМЛЕНИЕ
        private const float PanelWidth = 620f;
        private const float ControlWidth = 230f;
        private const float RowHeight = 30f;
        private static readonly Vector2 PanelAnchor = new(0.5f, 0.03f);

        private static readonly Color BackgroundColor = new(0.04f, 0.05f, 0.08f, 0.92f);
        private static readonly Color BorderColor = new(0.34f, 0.36f, 0.44f, 0.95f);
        private static readonly Color TitleColor = new(0.91f, 0.85f, 0.66f);
        private static readonly Color LabelColor = new(0.85f, 0.85f, 0.85f);
        private static readonly Color ValueColor = new(1f, 0.86f, 0.45f);
        private static readonly Color DimColor = new(0.55f, 0.55f, 0.58f);
        private static readonly Color ButtonColor = new(0.16f, 0.18f, 0.24f, 1f);
        private static readonly Color ButtonActiveColor = new(0.32f, 0.29f, 0.18f, 1f); // АКТИВНАЯ ВКЛАДКА / ВКЛЮЧЁННЫЙ ПЕРЕКЛЮЧАТЕЛЬ
        private static readonly Color FieldColor = new(0.09f, 0.10f, 0.14f, 1f);
        private static readonly Color SeparatorColor = new(1f, 1f, 1f, 0.12f);
        #endregion ОФОРМЛЕНИЕ

        private Func<List<SettingsTab>> buildTabs = () => new List<SettingsTab>();
        private int currentTab;

        private KeyBindItem listeningItem;
        private static KeyCode[] allKeyCodes;

        private GameObject canvas;
        private GameObject panel;
        private Font labelFont;
        private Font titleFont;

        #region СОЗДАНИЕ / ОТКРЫТИЕ / ЗАКРЫТИЕ
        public static void Create(Func<List<SettingsTab>> tabsProvider)
        {
            if (instance != null)
                return;

            var go = new GameObject("CustomizableAbilities SettingsPanel");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ModSettingsPanel>();
            instance.buildTabs = tabsProvider;
        }

        public static bool IsOpen => instance != null && instance.canvas != null && instance.canvas.activeSelf;

        private void Update()
        {
            bool paused = GameManager.instance != null && GameManager.instance.isPaused;

            if (!paused)
            {
                if (IsOpen)
                    Hide();
                return;
            }

            if (listeningItem != null)
            {
                ListenForKey();
                return;
            }

            if (CustomizableAbilities.GS.keybinds.ToggleSettingsPanel.WasPressed)
            {
                if (IsOpen)
                    Hide();
                else
                    Show();
            }
        }

        private void Show()
        {
            if (canvas == null)
                CreateCanvas();

            canvas.SetActive(true);
            Rebuild();
        }

        private void Hide()
        {
            listeningItem = null;
            if (canvas != null)
                canvas.SetActive(false);
        }

        private void CreateCanvas()
        {
            labelFont = CanvasUtil.GetFont("Perpetua") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleFont = CanvasUtil.TrajanBold ?? labelFont;

            canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
            canvas.name = "CustomizableAbilities SettingsCanvas";
            DontDestroyOnLoad(canvas);
            canvas.GetComponent<Canvas>().sortingOrder = 101; // ПОВЕРХ МЕНЮ ПАУЗЫ И ДИСПЛЕЯ

            var group = canvas.GetComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            panel = UiKit.CreateUIObject("Panel", canvas.transform);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = PanelAnchor;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(PanelWidth, 0f);

            var background = panel.AddComponent<Image>();
            background.sprite = UiKit.GetRoundedSprite(BackgroundColor, BorderColor);
            background.type = Image.Type.Sliced;

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 12, 14);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            panel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        #endregion СОЗДАНИЕ / ОТКРЫТИЕ / ЗАКРЫТИЕ

        #region ПОСТРОЕНИЕ СОДЕРЖИМОГО
        private void Rebuild()
        {
            if (panel == null)
                return;

            for (int i = panel.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = panel.transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            List<SettingsTab> tabs = buildTabs();
            if (tabs.Count == 0)
                return;
            currentTab = Mathf.Clamp(currentTab, 0, tabs.Count - 1);

            // ЗАГОЛОВОК
            UiKit.CreateText(panel.transform, "CUSTOMIZABLE ABILITIES — SETTINGS", titleFont, 17, TitleColor, TextAnchor.MiddleCenter);

            // ВКЛАДКИ
            var tabsRow = UiKit.CreateUIObject("Tabs", panel.transform);
            var tabsLayout = tabsRow.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.spacing = 4f;
            tabsLayout.childControlWidth = true;
            tabsLayout.childControlHeight = true;
            tabsLayout.childForceExpandWidth = true;
            tabsLayout.childForceExpandHeight = false;

            for (int i = 0; i < tabs.Count; i++)
            {
                int index = i;
                bool active = i == currentTab;
                UiKit.CreateButton(tabsRow.transform, tabs[i].Title.ToUpperInvariant(), titleFont, 12,
                    active ? TitleColor : LabelColor, active ? ButtonActiveColor : ButtonColor, 0f, 28f,
                    () => { currentTab = index; Rebuild(); });
            }

            UiKit.CreateSeparator(panel.transform, SeparatorColor);

            // НАСТРОЙКИ ТЕКУЩЕЙ ВКЛАДКИ
            foreach (SettingItem item in tabs[currentTab].Items)
                CreateItemRow(item);

            UiKit.CreateSeparator(panel.transform, SeparatorColor);

            // НИЖНЯЯ СТРОКА: ПОДСКАЗКА И КНОПКА «ЗАКРЫТЬ»
            var footer = CreateRowObject(panel.transform);
            Text hint = UiKit.CreateText(footer.transform, "Changes apply immediately", labelFont, 16, DimColor, TextAnchor.MiddleLeft);
            hint.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            UiKit.CreateButton(footer.transform, "CLOSE", titleFont, 12, LabelColor, ButtonColor, 110f, 28f, Hide);
        }

        private void CreateItemRow(SettingItem item)
        {
            if (item is ButtonItem button)
            {
                UiKit.CreateButton(panel.transform, button.Label, labelFont, 18, LabelColor, ButtonColor, 0f, RowHeight,
                    () => { button.Click(); Rebuild(); });
                return;
            }

            var row = CreateRowObject(panel.transform);
            Text label = UiKit.CreateText(row.transform, item.Label, labelFont, 19, LabelColor, TextAnchor.MiddleLeft);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            // ПРАВАЯ КОЛОНКА С ЭЛЕМЕНТОМ УПРАВЛЕНИЯ
            var control = UiKit.CreateUIObject("Control", row.transform);
            var controlLayout = control.AddComponent<HorizontalLayoutGroup>();
            controlLayout.spacing = 6f;
            controlLayout.childAlignment = TextAnchor.MiddleRight;
            controlLayout.childControlWidth = true;
            controlLayout.childControlHeight = true;
            controlLayout.childForceExpandWidth = false;
            controlLayout.childForceExpandHeight = false;
            control.AddComponent<LayoutElement>().preferredWidth = ControlWidth;

            switch (item)
            {
                case ToggleItem toggle:
                    bool on = toggle.Get();
                    UiKit.CreateButton(control.transform, on ? "ON" : "OFF", titleFont, 12,
                        on ? ValueColor : DimColor, on ? ButtonActiveColor : ButtonColor, 90f, RowHeight - 2f,
                        () => { toggle.Set(!toggle.Get()); Rebuild(); });
                    break;

                case NumberItem number:
                    CreateNumberControl(control.transform, number);
                    break;

                case ChoiceItem choice:
                    CreateChoiceControl(control.transform, choice);
                    break;

                case KeyBindItem keyBind:
                    CreateKeyBindControl(control.transform, keyBind);
                    break;
            }
        }

        private void CreateNumberControl(Transform parent, NumberItem number)
        {
            UiKit.CreateButton(parent, "−", labelFont, 22, LabelColor, ButtonColor, 32f, RowHeight - 2f,
                () => { ApplyNumber(number, number.Get() - number.Step); Rebuild(); });

            InputField field = UiKit.CreateInputField(parent, labelFont, 19, ValueColor, FieldColor, 100f, RowHeight - 2f,
                number.Integer ? InputField.ContentType.IntegerNumber : InputField.ContentType.DecimalNumber);
            field.text = FormatNumber(number, number.Get());
            field.onEndEdit.AddListener(text =>
            {
                if (float.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                    ApplyNumber(number, value);
                Rebuild();
            });

            UiKit.CreateButton(parent, "+", labelFont, 22, LabelColor, ButtonColor, 32f, RowHeight - 2f,
                () => { ApplyNumber(number, number.Get() + number.Step); Rebuild(); });
        }

        private void CreateChoiceControl(Transform parent, ChoiceItem choice)
        {
            int count = Mathf.Max(choice.Options.Length, 1);
            int current = Mathf.Clamp(choice.Get(), 0, count - 1);

            UiKit.CreateButton(parent, "<", labelFont, 20, LabelColor, ButtonColor, 32f, RowHeight - 2f,
                () => { choice.Set((current - 1 + count) % count); Rebuild(); });

            Text value = UiKit.CreateText(parent, choice.Options.Length > 0 ? choice.Options[current] : "", labelFont, 19, ValueColor, TextAnchor.MiddleCenter);
            value.gameObject.AddComponent<LayoutElement>().preferredWidth = 130f;

            UiKit.CreateButton(parent, ">", labelFont, 20, LabelColor, ButtonColor, 32f, RowHeight - 2f,
                () => { choice.Set((current + 1) % count); Rebuild(); });
        }

        #region БИНДЫ
        private void CreateKeyBindControl(Transform parent, KeyBindItem keyBind)
        {
            bool listening = listeningItem != null && listeningItem.Label == keyBind.Label;
            string caption = listening ? "PRESS A KEY..." : GetBindingName(keyBind.GetAction());

            UiKit.CreateButton(parent, caption, listening ? titleFont : labelFont, listening ? 12 : 19,
                listening ? TitleColor : ValueColor, listening ? ButtonActiveColor : ButtonColor, 170f, RowHeight - 2f,
                () =>
                {
                    listeningItem = listening ? null : keyBind;
                    Rebuild();
                });
        }

        private static string GetBindingName(InControl.PlayerAction action)
        {
            if (action == null || action.Bindings.Count == 0)
                return "None";
            return action.Bindings[0].Name;
        }

        private void ListenForKey()
        {
            allKeyCodes ??= (KeyCode[])Enum.GetValues(typeof(KeyCode));

            foreach (KeyCode code in allKeyCodes)
            {
                if (code >= KeyCode.Mouse0 || !Input.GetKeyDown(code))
                    continue;

                InControl.PlayerAction action = listeningItem.GetAction();
                if (action == null)
                    break;

                if (code == KeyCode.Backspace || code == KeyCode.Delete)
                {
                    action.ClearBindings(); // СНЯТЬ БИНД
                }
                else if (TryConvertKey(code, out InControl.Key key))
                {
                    action.ClearBindings();
                    action.AddBinding(new InControl.KeyBindingSource(key));
                }
                else
                {
                    continue;
                }

                listeningItem.Changed();
                listeningItem = null;
                Rebuild();
                return;
            }
        }

        private static bool TryConvertKey(KeyCode code, out InControl.Key key)
        {
            string name = code.ToString();
            if (name.StartsWith("Alpha"))
                name = "Key" + name.Substring("Alpha".Length);
            else if (name.StartsWith("Keypad"))
                name = "Pad" + name.Substring("Keypad".Length);

            return Enum.TryParse(name, out key) && key != InControl.Key.None;
        }
        #endregion БИНДЫ

        private static void ApplyNumber(NumberItem number, float value)
        {
            value = Mathf.Clamp(value, number.Min, number.Max);
            if (number.Integer)
                value = Mathf.Round(value);
            number.Set(value);
        }

        private static string FormatNumber(NumberItem number, float value) =>
            number.Integer
                ? Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture)
                : value.ToString(number.Format, CultureInfo.InvariantCulture);

        private static GameObject CreateRowObject(Transform parent)
        {
            var row = UiKit.CreateUIObject("Row", parent);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            row.AddComponent<LayoutElement>().minHeight = RowHeight;
            return row;
        }
        #endregion ПОСТРОЕНИЕ СОДЕРЖИМОГО
    }

    #region ОБЩИЕ ЭЛЕМЕНТЫ ИНТЕРФЕЙСА
    internal static class UiKit
    {
        private static readonly Dictionary<string, Sprite> spriteCache = new();

        public static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static Text CreateText(Transform parent, string content, Font font, int size, Color color, TextAnchor anchor)
        {
            var go = CreateUIObject("Text", parent);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.text = content;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        // КНОПКА: ЗАКРУГЛЁННЫЙ ФОН + ТЕКСТ
        public static Button CreateButton(Transform parent, string label, Font font, int fontSize, Color textColor,
            Color backgroundColor, float width, float height, Action onClick)
        {
            var go = CreateUIObject("Button", parent);
            var image = go.AddComponent<Image>();
            image.sprite = GetRoundedSprite(Color.white, Color.white, 6f);
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = backgroundColor;
            colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.15f); // ПОДСВЕТКА ПРИ НАВЕДЕНИИ
            colors.pressedColor = Color.Lerp(backgroundColor, Color.black, 0.25f);
            colors.selectedColor = backgroundColor;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.onClick.AddListener(() => onClick());

            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            if (width > 0f)
            {
                element.preferredWidth = width;
                element.minWidth = width;
            }

            Text text = CreateText(go.transform, label, font, fontSize, textColor, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        public static InputField CreateInputField(Transform parent, Font font, int fontSize, Color textColor,
            Color backgroundColor, float width, float height, InputField.ContentType contentType)
        {
            var go = CreateUIObject("Input", parent);
            var image = go.AddComponent<Image>();
            image.sprite = GetRoundedSprite(Color.white, Color.white, 6f);
            image.type = Image.Type.Sliced;
            image.color = backgroundColor;

            var element = go.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.minWidth = width;
            element.preferredHeight = height;
            element.minHeight = height;

            var textObject = CreateUIObject("Text", go.transform);
            var text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = textColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            Stretch(text.rectTransform, 6f);

            var input = go.AddComponent<InputField>();
            input.textComponent = text;
            input.targetGraphic = image;
            input.contentType = contentType;
            input.customCaretColor = true;
            input.caretColor = textColor;
            return input;
        }

        public static void CreateSeparator(Transform parent, Color color)
        {
            var go = CreateUIObject("Separator", parent);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            go.AddComponent<LayoutElement>().preferredHeight = 1f;
        }

        public static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, 0f);
            rect.offsetMax = new Vector2(-padding, 0f);
        }

        public static Sprite GetRoundedSprite(Color fill, Color border, float radius = 10f)
        {
            string key = $"{fill}|{border}|{radius}";
            if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new(x + 0.5f, y + 0.5f);
                    Vector2 c = new(Mathf.Clamp(p.x, radius, size - radius), Mathf.Clamp(p.y, radius, size - radius));
                    float outside = Vector2.Distance(p, c) - radius;

                    Color color = outside > -1.5f ? border : fill;
                    color.a *= Mathf.Clamp01(0.5f - outside);
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            spriteCache[key] = sprite;
            return sprite;
        }
    }
    #endregion ОБЩИЕ ЭЛЕМЕНТЫ ИНТЕРФЕЙСА

    public partial class CustomizableAbilities
    {
        #region ВКЛАДКИ ОКНА НАСТРОЕК
        private List<SettingsTab> BuildSettingsTabs()
        {
            return new List<SettingsTab>
            {
                new SettingsTab
                {
                    Title = "General",
                    Items = new List<SettingItem>
                    {
                        new ToggleItem
                        {
                            Label = "Enable Mod",
                            Get = () => GS.EnableMod,
                            Set = value =>
                            {
                                GS.EnableMod = value;
                                OnSaveGlobal();
                                if (!GS.EnableMod)
                                    DisableMod();
                                else
                                    EnableMod();
                                menuRef?.Update();
                            }
                        },
                        new ToggleItem
                        {
                            Label = "Heal Enemies Over Their Max HP",
                            Get = () => GS.HealEnemiesOverMaxHP,
                            Set = value => { GS.HealEnemiesOverMaxHP = value; OnSaveGlobal(); menuRef?.Update(); }
                        },
                        new ToggleItem
                        {
                            Label = "Show Display",
                            Get = () => GS.EnableDisplay,
                            Set = value => { GS.EnableDisplay = value; OnSaveGlobal(); menuRef?.Update(); }
                        },
                        new ButtonItem
                        {
                            Label = "Reset Display Position",
                            Click = () =>
                            {
                                GS.DisplayAnchorX = 0.02f;
                                GS.DisplayAnchorY = 0.23f;
                                ModDisplay.Instance?.Destroy();
                            }
                        },
                        new ButtonItem { Label = "Reset Global Settings To Defaults", Click = ResetGlobalSettings }
                    }
                },

                new SettingsTab
                {
                    Title = "Focus",
                    Items = new List<SettingItem>
                    {
                        DisplayToggle("Show Focus Cost", () => GS.DisplayFocusCost, v => GS.DisplayFocusCost = v),
                        DisplayToggle("Show Focus Time", () => GS.DisplayFocusTimeMultiplier, v => GS.DisplayFocusTimeMultiplier = v),
                        new NumberItem
                        {
                            Label = "Focus Cost (SOUL)",
                            Get = () => LS.CustomFocusCost,
                            Set = v => { LS.CustomFocusCost = Mathf.Clamp(Mathf.RoundToInt(v), 0, 999); OnSaveLocal(); },
                            Min = 0f, Max = 999f, Step = 1f
                        },
                        new NumberItem
                        {
                            Label = "Focus Time Multiplier",
                            Get = () => LS.CustomFocusTimeMultiplier,
                            Set = v => { LS.CustomFocusTimeMultiplier = Mathf.Clamp(v, 0.01f, 99f); OnSaveLocal(); },
                            Min = 0.01f, Max = 99f, Step = 0.1f, Integer = false, Format = "0.##"
                        },
                        new ButtonItem { Label = "Reset Focus Settings To Defaults", Click = ResetFocusSettings }
                    }
                },

                new SettingsTab
                {
                    Title = "Nail",
                    Items = new List<SettingItem>
                    {
                        DisplayToggle("Show Nail Damage", () => GS.DisplayNailDamage, v => GS.DisplayNailDamage = v),
                        DisplayToggle("Show Nail Soul Gain", () => GS.DisplayNailSoulGain, v => GS.DisplayNailSoulGain = v),
                        DisplayToggle("Show Nail Cooldown", () => GS.DisplayNailCooldown, v => GS.DisplayNailCooldown = v),
                        new ToggleItem
                        {
                            Label = "Ignore Nail Binding",
                            Get = () => LS.IgnoreNailBinding,
                            Set = v => { LS.IgnoreNailBinding = v; OnSaveLocal(); }
                        },
                        new ToggleItem
                        {
                            Label = "Enable Float Damage",
                            Get = () => GS.EnableFloatNailDamage,
                            Set = v =>
                            {
                                GS.EnableFloatNailDamage = v;
                                OnSaveGlobal();
                                if (GS.EnableFloatNailDamage)
                                    LS.ModifiedNailDamage = GetModifiedDamage(LS.CustomFloatNailDamage, "nail");
                                else
                                    LS.ModifiedNailDamage = Mathf.RoundToInt(GetModifiedDamage(LS.CustomIntNailDamage, "nail"));
                            }
                        },
                        new ChoiceItem
                        {
                            Label = "Vanilla Nail Damage",
                            Options = new[] { "Old", "Sharpened", "Channelled", "Coiled", "Pure", "-Old", "-Sharpened", "-Channelled", "-Coiled", "-Pure" },
                            Get = () => LS.NailUpgrade,
                            Set = index =>
                            {
                                // ТО ЖЕ, ЧТО В МЕНЮ: РАБОТАЕТ ТОЛЬКО В ЦЕЛОМ РЕЖИМЕ
                                if (!GS.EnableFloatNailDamage)
                                {
                                    int vanillaDmg = 5 * (index + 1) - (index + 1) + 1;
                                    if (index > 4 && index <= 9)
                                        vanillaDmg = -1 * (5 * (index - 4) - (index - 4) + 1);
                                    UpdateNailDamage(vanillaDmg);
                                    LS.ModifiedNailDamage = Mathf.RoundToInt(GetModifiedDamage(vanillaDmg, "nail"));
                                    LS.NailUpgrade = index;
                                    OnSaveLocal();
                                }
                            }
                        },
                        new NumberItem
                        {
                            Label = "Nail Damage",
                            Get = () => LS.CustomIntNailDamage,
                            Set = v =>
                            {
                                int naildmg = Mathf.Clamp(Mathf.RoundToInt(v), -9999, 9999);
                                UpdateNailDamage(naildmg);
                                OnSaveLocal();
                                if (!GS.EnableFloatNailDamage)
                                    LS.ModifiedNailDamage = Mathf.RoundToInt(GetModifiedDamage(LS.CustomIntNailDamage, "nail"));
                            },
                            Min = -9999f, Max = 9999f, Step = 1f
                        },
                        new NumberItem
                        {
                            Label = "Float Nail Damage",
                            Get = () => LS.CustomFloatNailDamage,
                            Set = v =>
                            {
                                LS.CustomFloatNailDamage = Mathf.Round(Mathf.Clamp(v, 0.01f, 0.99f) * 100f) / 100f;
                                if (GS.EnableFloatNailDamage)
                                    LS.ModifiedNailDamage = GetModifiedDamage(LS.CustomFloatNailDamage, "nail");
                            },
                            Min = 0.01f, Max = 0.99f, Step = 0.01f, Integer = false, Format = "0.00"
                        },
                        new NumberItem
                        {
                            Label = "Main Vessel Soul Gain",
                            Get = () => LS.SoulGain,
                            Set = v => { LS.SoulGain = Mathf.Clamp(Mathf.RoundToInt(v), 0, 198); OnSaveLocal(); },
                            Min = 0f, Max = 198f, Step = 1f
                        },
                        new NumberItem
                        {
                            Label = "Reserve Vessel Soul Gain",
                            Get = () => LS.ReserveSoulGain,
                            Set = v => { LS.ReserveSoulGain = Mathf.Clamp(Mathf.RoundToInt(v), 0, 198); OnSaveLocal(); },
                            Min = 0f, Max = 198f, Step = 1f
                        },
                        new NumberItem
                        {
                            Label = "Nail Cooldown (SEC)",
                            Get = () => LS.CustomNailCooldown,
                            Set = v => { LS.CustomNailCooldown = Mathf.Clamp(v, -198f, 198f); OnSaveLocal(); },
                            Min = -198f, Max = 198f, Step = 0.05f, Integer = false, Format = "0.###"
                        },
                        new ButtonItem { Label = "Reset Nail Settings To Defaults", Click = ResetNailSettings }
                    }
                },

                new SettingsTab
                {
                    Title = "Dream Nail",
                    Items = new List<SettingItem>
                    {
                        DisplayToggle("Show Dream Nail Soul Gain", () => GS.DisplayDreamNailSoulGain, v => GS.DisplayDreamNailSoulGain = v),
                        DisplayToggle("Show Dream Nail Damage", () => GS.DisplayDreamNailDamage, v => GS.DisplayDreamNailDamage = v),
                        new NumberItem
                        {
                            Label = "Dream Nail Soul Gain",
                            Get = () => LS.CustomDreamNailSoulGain,
                            Set = v => { LS.CustomDreamNailSoulGain = Mathf.Clamp(Mathf.RoundToInt(v), 0, 198); OnSaveLocal(); },
                            Min = 0f, Max = 198f, Step = 1f
                        },
                        new NumberItem
                        {
                            Label = "Dream Nail Damage",
                            Get = () => LS.CustomDreamNailDamage,
                            Set = v => { LS.CustomDreamNailDamage = Mathf.Clamp(v, -9999f, 9999f); OnSaveLocal(); },
                            Min = -9999f, Max = 9999f, Step = 1f, Integer = false, Format = "0.##"
                        },
                        new ButtonItem { Label = "Reset Dream Nail Settings To Defaults", Click = ResetDreamNailSettings }
                    }
                },

                new SettingsTab
                {
                    Title = "Spells",
                    Items = new List<SettingItem>
                    {
                        DisplayToggle("Show Spells Damage", () => GS.DisplaySpellsDamage, v => GS.DisplaySpellsDamage = v),
                        SpellDamage("Vengeful Spirit Damage", () => LS.CustomVengefulSpiritDamage, v => LS.CustomVengefulSpiritDamage = v),
                        SpellDamage("Desolate Dive Damage", () => LS.CustomDesolateDiveDamage, v => LS.CustomDesolateDiveDamage = v),
                        SpellDamage("Howling Wraiths Damage", () => LS.CustomHowlingWraithsDamage, v => LS.CustomHowlingWraithsDamage = v),
                        SpellDamage("Shade Soul Damage", () => LS.CustomShadeSoulDamage, v => LS.CustomShadeSoulDamage = v),
                        SpellDamage("Descending Dark Damage", () => LS.CustomDescendingDarkDamage, v => LS.CustomDescendingDarkDamage = v),
                        SpellDamage("Abyss Shriek Damage", () => LS.CustomAbyssShriekDamage, v => LS.CustomAbyssShriekDamage = v),
                        new ButtonItem { Label = "Reset Spells Settings To Defaults", Click = ResetSpellsSettings }
                    }
                },

                new SettingsTab
                {
                    Title = "Crystal Dash",
                    Items = new List<SettingItem>
                    {
                        DisplayToggle("Show Crystal Dash Damage", () => GS.DisplaySuperDashDamage, v => GS.DisplaySuperDashDamage = v),
                        new NumberItem
                        {
                            Label = "Crystal Dash Damage",
                            Get = () => LS.CustomSuperDashDamage,
                            Set = v => { LS.CustomSuperDashDamage = Mathf.Clamp(v, -9999f, 9999f); OnSaveLocal(); },
                            Min = -9999f, Max = 9999f, Step = 1f, Integer = false, Format = "0.##"
                        },
                        new ButtonItem { Label = "Reset Crystal Dash Settings To Defaults", Click = ResetSuperDashSettings }
                    }
                },

                new SettingsTab
                {
                    Title = "Keys",
                    Items = new List<SettingItem>
                    {
                        KeyBinding("Increase Nail Damage", () => GS.keybinds.IncreaseNailDamage),
                        KeyBinding("Decrease Nail Damage", () => GS.keybinds.DecreaseNailDamage),
                        KeyBinding("Show/Hide Display", () => GS.keybinds.ToggleDisplay),
                        KeyBinding("Show/Hide Settings Panel", () => GS.keybinds.ToggleSettingsPanel)
                    }
                }
            };
        }

        private ToggleItem DisplayToggle(string label, Func<bool> get, Action<bool> set) => new ToggleItem
        {
            Label = label,
            Get = get,
            Set = v => { set(v); OnSaveGlobal(); }
        };

        private NumberItem SpellDamage(string label, Func<int> get, Action<int> set) => new NumberItem
        {
            Label = label,
            Get = () => get(),
            Set = v => { set(Mathf.Clamp(Mathf.RoundToInt(v), 1, 9999)); OnSaveLocal(); },
            Min = 1f,
            Max = 9999f,
            Step = 1f
        };

        private KeyBindItem KeyBinding(string label, Func<InControl.PlayerAction> action) => new KeyBindItem
        {
            Label = label,
            GetAction = action,
            Changed = () => { OnSaveGlobal(); menuRef?.Update(); }
        };
        #endregion ВКЛАДКИ ОКНА НАСТРОЕК

        #region СБРОС НАСТРОЕК (ОБЩИЙ ДЛЯ ОКНА И МЕНЮ)
        private void ResetGlobalSettings()
        {
            if (!GS.EnableMod) return;

            GS.HealEnemiesOverMaxHP = true;

            GS.keybinds.IncreaseNailDamage.ClearBindings();
            GS.keybinds.DecreaseNailDamage.ClearBindings();
            GS.keybinds.ToggleDisplay.ClearBindings();
            GS.keybinds.ToggleSettingsPanel.ClearBindings();
            GS.keybinds.ToggleDisplay.AddBinding(new InControl.KeyBindingSource(InControl.Key.F5));
            GS.keybinds.ToggleSettingsPanel.AddBinding(new InControl.KeyBindingSource(InControl.Key.F3));

            GS.DisplayAnchorX = 0.02f;
            GS.DisplayAnchorY = 0.23f;
            ModDisplay.Instance?.Destroy();

            OnSaveGlobal();
            OnSaveLocal();
            menuRef?.Update();
        }

        private void ResetFocusSettings()
        {
            if (!GS.EnableMod) return;

            GS.DisplayFocusCost = true;
            GS.DisplayFocusTimeMultiplier = true;
            LS.CustomFocusCost = 33;
            LS.CustomFocusTimeMultiplier = 1f;

            OnSaveGlobal();
            OnSaveLocal();
        }

        private void ResetNailSettings()
        {
            if (!GS.EnableMod) return;

            GS.EnableFloatNailDamage = false;
            GS.DisplayNailDamage = true;
            GS.DisplayNailSoulGain = true;
            GS.DisplayNailCooldown = true;

            LS.IgnoreNailBinding = false;
            LS.CustomFloatNailDamage = 0.1f;
            LS.CustomNailCooldown = 0.41f;
            LS.NailUpgrade = 0;
            UpdateNailDamage(21);
            LS.ModifiedNailDamage = GetModifiedDamage(LS.CustomIntNailDamage, "nail");
            LS.SoulGain = 11;
            LS.ReserveSoulGain = 6;

            OnSaveGlobal();
            OnSaveLocal();
        }

        private void ResetDreamNailSettings()
        {
            if (!GS.EnableMod) return;

            GS.DisplayDreamNailSoulGain = true;
            GS.DisplayDreamNailDamage = true;
            LS.CustomDreamNailSoulGain = 33;
            LS.CustomDreamNailDamage = 0f;

            OnSaveGlobal();
            OnSaveLocal();
        }

        private void ResetSpellsSettings()
        {
            if (!GS.EnableMod) return;

            GS.DisplaySpellsDamage = true;
            LS.CustomVengefulSpiritDamage = 15;
            LS.CustomDesolateDiveDamage = 35;
            LS.CustomHowlingWraithsDamage = 39;
            LS.CustomShadeSoulDamage = 30;
            LS.CustomDescendingDarkDamage = 60;
            LS.CustomAbyssShriekDamage = 80;

            OnSaveGlobal();
            OnSaveLocal();
        }

        private void ResetSuperDashSettings()
        {
            if (!GS.EnableMod) return;

            GS.DisplaySuperDashDamage = true;
            LS.CustomSuperDashDamage = 10f;

            OnSaveGlobal();
            OnSaveLocal();
        }
        #endregion СБРОС НАСТРОЕК (ОБЩИЙ ДЛЯ ОКНА И МЕНЮ)
    }
}