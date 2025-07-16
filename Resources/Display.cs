using Modding;
using System;
using UnityEngine;

namespace CustomizableAbilities
{
    internal class ModDisplay
    {
        internal static ModDisplay Instance;

        private string displayText = "";
        private GameObject canvas;
        private UnityEngine.UI.Text textUI;

        private readonly Vector2 textSize = new(800, 500);
        private readonly Vector2 textPosition = new(0.22f, 0.243f);

        public ModDisplay()
        {
            CreateUI();
        }

        private void CreateUI()
        {
            if (canvas != null) return;

            canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
            UnityEngine.Object.DontDestroyOnLoad(canvas);

            var canvasGroup = canvas.GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            textUI = CanvasUtil.CreateTextPanel(
                canvas, "", 26, TextAnchor.UpperLeft,
                new CanvasUtil.RectData(textSize, Vector2.zero, textPosition, textPosition),
                CanvasUtil.GetFont("Perpetua")
            ).GetComponent<UnityEngine.UI.Text>();
        }

        public void Destroy()
        {
            if (canvas != null)
            {
                canvas.SetActive(false);
                UnityEngine.Object.Destroy(canvas);
                canvas = null;
                textUI = null;
                Instance = null;
            }
        }

        public void Display(string text)
        {
            string trimmed = text.Trim();
            if (displayText == trimmed) return;

            displayText = trimmed;
            Update();
        }

        public void Update()
        {
            if (textUI == null || canvas == null) return;

            textUI.text = displayText;
            canvas.SetActive(true);
        }
    }
}