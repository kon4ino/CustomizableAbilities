using Modding;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CustomizableAbilities
{
    internal class ModDisplay
    {
        internal static ModDisplay Instance;

        #region ОФОРМЛЕНИЕ
        private const float PanelWidth = 340f;

        private static readonly Color BackgroundColor = new(0.04f, 0.05f, 0.08f, 0.82f);
        private static readonly Color BorderColor = new(0.34f, 0.36f, 0.44f, 0.95f); // РАМКА: СВЕТЛЕЕ ФОНА И НЕ ПРОЗРАЧНАЯ
        private static readonly Color TitleColor = new(0.91f, 0.85f, 0.66f);
        private static readonly Color SectionColor = new(0.62f, 0.71f, 0.85f);
        private static readonly Color LabelColor = new(0.85f, 0.85f, 0.85f);
        private static readonly Color ValueColor = new(1f, 0.86f, 0.45f);
        private static readonly Color NegativeColor = new(1f, 0.5f, 0.5f);
        private static readonly Color DimColor = new(0.55f, 0.55f, 0.58f);
        private static readonly Color SeparatorColor = new(1f, 1f, 1f, 0.12f);
        #endregion ОФОРМЛЕНИЕ

        private string displayText = "";
        private GameObject canvas;
        private GameObject content; // СЮДА ПЕРЕСТРАИВАЮТСЯ РАЗДЕЛЫ ПРИ ИЗМЕНЕНИИ ЗНАЧЕНИЙ
        private Font labelFont;
        private Font titleFont;

        private static Sprite panelSprite;

        public ModDisplay()
        {
            CreateUI();
        }

        #region СОЗДАНИЕ ПАНЕЛИ
        private void CreateUI()
        {
            if (canvas != null) return;

            labelFont = CanvasUtil.GetFont("Perpetua") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleFont = CanvasUtil.TrajanBold ?? labelFont;

            canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
            Object.DontDestroyOnLoad(canvas);
            canvas.GetComponent<Canvas>().sortingOrder = 100;

            var canvasGroup = canvas.GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // ФОН ПАНЕЛИ: ЗАКРУГЛЁННЫЙ ПРЯМОУГОЛЬНИК, РАСТЯГИВАЕТСЯ ПОД СОДЕРЖИМОЕ
            var panel = CreateUIObject("Panel", canvas.transform);
            var panelRect = panel.GetComponent<RectTransform>();
            Vector2 anchor = new(CustomizableAbilities.GS.DisplayAnchorX, 1f - CustomizableAbilities.GS.DisplayAnchorY);
            panelRect.anchorMin = panelRect.anchorMax = anchor;
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(PanelWidth, 0f);

            var background = panel.AddComponent<Image>();
            background.sprite = GetPanelSprite();
            background.type = Image.Type.Sliced;
            background.raycastTarget = true;

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 12, 14);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = panel.AddComponent<ContentSizeFitter>();
            panel.AddComponent<DisplayDragHandler>().Init(canvasGroup);
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ЗАГОЛОВОК
            CreateText(panel.transform, "CUSTOMIZABLE ABILITIES", titleFont, 17, TitleColor, TextAnchor.MiddleCenter);
            CreateSeparator(panel.transform);

            // КОНТЕЙНЕР РАЗДЕЛОВ
            content = CreateUIObject("Content", panel.transform);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 2f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
        }

        public void Destroy()
        {
            if (canvas != null)
            {
                canvas.SetActive(false);
                Object.Destroy(canvas);
                canvas = null;
                content = null;
                Instance = null;
            }
        }
        #endregion СОЗДАНИЕ ПАНЕЛИ

        #region ОБНОВЛЕНИЕ
        public void Display(string text)
        {
            string trimmed = text.Trim();
            if (displayText == trimmed) return;

            displayText = trimmed;
            Update();
        }

        public void Update()
        {
            if (canvas == null || content == null) return;

            for (int i = content.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = content.transform.GetChild(i).gameObject;
                child.SetActive(false);
                Object.Destroy(child);
            }

            List<Section> sections = ParseSections(displayText);
            for (int i = 0; i < sections.Count; i++)
            {
                if (i > 0)
                    CreateSpacer(content.transform, 6f);

                CreateText(content.transform, sections[i].Title.ToUpperInvariant(), titleFont, 13, SectionColor, TextAnchor.MiddleLeft);
                foreach (Row row in sections[i].Rows)
                    CreateRow(content.transform, row);
            }

            canvas.SetActive(true);
        }
        #endregion ОБНОВЛЕНИЕ

        #region РАЗБОР ТЕКСТА ИЗ BuildDisplayText
        private class Row
        {
            public string Label;
            public string Value;
            public bool Dim; // ПРИГЛУШЁННАЯ СТРОКА (НАПРИМЕР, ЗАКЛИНАНИЕ ЕЩЁ НЕ ПОЛУЧЕНО)
        }

        private class Section
        {
            public string Title;
            public List<Row> Rows = new();
        }

        private static readonly string[] SpellNames =
        {
            "vengeful spirit", "shade soul", "desolate dive", "descending dark", "howling wraiths", "abyss shriek"
        };

        private static List<Section> ParseSections(string text)
        {
            var sections = new List<Section>();
            var byTitle = new Dictionary<string, Section>();

            foreach (string rawLine in text.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("[") || line.StartsWith("CustomizableAbilities"))
                    continue;

                (string title, Row row) = ParseLine(line);

                if (!byTitle.TryGetValue(title, out Section section))
                {
                    section = new Section { Title = title };
                    byTitle[title] = section;
                    sections.Add(section);
                }
                section.Rows.Add(row);
            }

            return sections;
        }

        private static (string title, Row row) ParseLine(string line)
        {
            // «not enable fireball» И Т.П. — ЗАКЛИНАНИЕ ЕЩЁ НЕ ПОЛУЧЕНО
            if (line.StartsWith("not enable "))
            {
                string spell = line.Substring("not enable ".Length) switch
                {
                    "fireball" => "Vengeful Spirit",
                    "quake" => "Desolate Dive",
                    "scream" => "Howling Wraiths",
                    string other => Capitalize(other)
                };
                return ("Spells", new Row { Label = spell, Value = "not obtained", Dim = true });
            }

            int colon = line.IndexOf(':');
            if (colon < 0)
                return ("Other", new Row { Label = line, Value = "", Dim = true });

            string name = line.Substring(0, colon).Trim();
            string value = line.Substring(colon + 1).Trim();

            if (name.StartsWith("dream nail "))
                return ("Dream Nail", new Row { Label = Capitalize(name.Substring("dream nail ".Length)), Value = value });

            if (name.StartsWith("nail "))
            {
                string label = name.Substring("nail ".Length);
                if (label == "soul gain")
                    return ("Nail", new Row { Label = "Soul (main / reserve)", Value = value.Replace(", ", " / ") });
                if (label == "cooldown")
                    return ("Nail", new Row { Label = "Cooldown", Value = value + " s" });
                return ("Nail", new Row { Label = Capitalize(label), Value = value });
            }

            if (name.StartsWith("crystal dash "))
                return ("Crystal Dash", new Row { Label = Capitalize(name.Substring("crystal dash ".Length)), Value = value });

            if (name.StartsWith("focus "))
            {
                string label = name.Substring("focus ".Length);
                if (label == "time multiplier" || label == "time")
                    return ("Focus", new Row { Label = "Time", Value = "x" + value });
                return ("Focus", new Row { Label = Capitalize(label), Value = value });
            }

            foreach (string spell in SpellNames)
            {
                if (name.StartsWith(spell))
                    return ("Spells", new Row { Label = TitleCase(spell), Value = value });
            }

            return ("Other", new Row { Label = Capitalize(name), Value = value });
        }

        private static string Capitalize(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        private static string TitleCase(string s) =>
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s);
        #endregion РАЗБОР ТЕКСТА ИЗ BuildDisplayText

        #region ЭЛЕМЕНТЫ ИНТЕРФЕЙСА
        private void CreateRow(Transform parent, Row row)
        {
            var rowObject = CreateUIObject("Row", parent);
            var layout = rowObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Text label = CreateText(rowObject.transform, row.Label, labelFont, 19, row.Dim ? DimColor : LabelColor, TextAnchor.MiddleLeft);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            Color valueColor = row.Dim ? DimColor : row.Value.StartsWith("-") ? NegativeColor : ValueColor;
            Text value = CreateText(rowObject.transform, FormatValue(row.Value), labelFont, 19, valueColor, TextAnchor.MiddleRight);
            value.gameObject.AddComponent<LayoutElement>().flexibleWidth = 0f;
        }

        private static string FormatValue(string value)
        {
            int bracket = value.IndexOf('(');
            if (bracket < 0)
                return value;

            string dimHex = ColorUtility.ToHtmlStringRGB(DimColor);
            return value.Substring(0, bracket).TrimEnd() + $"  <color=#{dimHex}>{value.Substring(bracket)}</color>";
        }

        private static Text CreateText(Transform parent, string content, Font font, int size, Color color, TextAnchor anchor)
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

        private static void CreateSeparator(Transform parent)
        {
            var go = CreateUIObject("Separator", parent);
            var image = go.AddComponent<Image>();
            image.color = SeparatorColor;
            image.raycastTarget = false;
            go.AddComponent<LayoutElement>().preferredHeight = 1f;
        }

        private static void CreateSpacer(Transform parent, float height)
        {
            var go = CreateUIObject("Spacer", parent);
            go.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        // ТЕКСТУРА ФОНА ГЕНЕРИРУЕТСЯ В КОДЕ: ЗАКРУГЛЁННЫЕ УГЛЫ + ТОНКАЯ СВЕТЛАЯ РАМКА.
        // 9-SLICE: УГЛЫ НЕ РАСТЯГИВАЮТСЯ, ПОЭТОМУ ПАНЕЛЬ ЛЮБОГО РАЗМЕРА ВЫГЛЯДИТ ОДИНАКОВО
        private static Sprite GetPanelSprite()
        {
            if (panelSprite != null)
                return panelSprite;

            const int size = 32;
            const float radius = 10f;

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

                    float coverage = Mathf.Clamp01(0.5f - outside); // СГЛАЖИВАНИЕ КРАЯ
                    Color color = outside > -1.5f ? BorderColor : BackgroundColor;
                    color.a *= coverage;
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();

            panelSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return panelSprite;
        }
        #endregion ЭЛЕМЕНТЫ ИНТЕРФЕЙСА
    }

    // ПЕРЕТАСКИВАНИЕ ДИСПЛЕЯ МЫШЬЮ НА ПАУЗЕ
    internal class DisplayDragHandler : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        private RectTransform rect;
        private RectTransform parentRect;
        private CanvasGroup group;
        private Canvas rootCanvas;

        public void Init(CanvasGroup canvasGroup)
        {
            group = canvasGroup;
            rect = GetComponent<RectTransform>();
            parentRect = (RectTransform)rect.parent;
            rootCanvas = GetComponentInParent<Canvas>();
        }

        private void Update()
        {
            // ЛОВИМ МЫШЬ ТОЛЬКО НА ПАУЗЕ, ВО ВРЕМЯ ИГРЫ ПАНЕЛЬ НЕ МЕШАЕТ
            group.blocksRaycasts = GameManager.instance != null && GameManager.instance.isPaused;

            // ДЕРЖИМ ПАНЕЛЬ В ПРЕДЕЛАХ ЭКРАНА (ЕЁ ВЫСОТА МЕНЯЕТСЯ, КОГДА МЕНЯЮТСЯ СТРОКИ)
            ClampToScreen();
        }

        public void OnDrag(PointerEventData eventData)
        {
            // delta — В ПИКСЕЛЯХ МОНИТОРА: ПЕРЕВОДИМ В ЕДИНИЦЫ КАНВАСА, А ПОТОМ В ДОЛИ ЭКРАНА
            Vector2 size = parentRect.rect.size;
            Vector2 deltaFraction = eventData.delta / rootCanvas.scaleFactor / size;
            rect.anchorMin = rect.anchorMax = rect.anchorMin + deltaFraction;
            ClampToScreen();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            CustomizableAbilities.GS.DisplayAnchorX = rect.anchorMin.x;
            CustomizableAbilities.GS.DisplayAnchorY = 1f - rect.anchorMin.y;
        }

        private void ClampToScreen()
        {
            Vector2 size = parentRect.rect.size;
            if (size.x <= 0f || size.y <= 0f)
                return; // КАНВАС ЕЩЁ НЕ ПОЛУЧИЛ РАЗМЕР (ПЕРВЫЙ КАДР)

            // РАЗМЕР ПАНЕЛИ В ДОЛЯХ ЭКРАНА
            float w = Mathf.Min(rect.rect.width / size.x, 1f);
            float h = Mathf.Min(rect.rect.height / size.y, 1f);

            Vector2 anchor = rect.anchorMin;
            anchor.x = Mathf.Clamp(anchor.x, 0f, 1f - w);
            anchor.y = Mathf.Clamp(anchor.y, h, 1f); // ЯКОРЬ — ВЕРХНИЙ ЛЕВЫЙ УГОЛ, ПАНЕЛЬ ВИСИТ ВНИЗ ОТ НЕГО
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
        }
    }

    public partial class CustomizableAbilities
    {
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

            if (GS.DisplayFocusCost)
            {
                displayText += $"focus cost: {LS.CustomFocusCost}\n";
            }
            if (GS.DisplayFocusTimeMultiplier)
            {
                displayText += $"focus time: {LS.CustomFocusTimeMultiplier} ({GetFocusHealSeconds().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} s)\n";
            }
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
            if (GS.DisplaySuperDashDamage)
            {
                displayText += $"crystal dash damage: {LS.CustomSuperDashDamage}\n";
            }

            displayText += "[--------------------------]";
            return displayText;
        }
        #endregion ОТОБРАЖЕНИЕ ДИСПЛЕЯ

        #region ОБНОВЛЕНИЕ ДИСПЛЕЯ (ВЫЗЫВАЕТСЯ ИЗ OnHeroUpdate)
        private void UpdateDisplayBeforeInput()
        {
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
        }

        private void RefreshDisplay()
        {
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
        }
        #endregion ОБНОВЛЕНИЕ ДИСПЛЕЯ (ВЫЗЫВАЕТСЯ ИЗ OnHeroUpdate)
    }
}