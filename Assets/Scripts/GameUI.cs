using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FloeForward
{
    public sealed class GameUI : MonoBehaviour
    {
        [System.Serializable]
        private struct ChoiceText
        {
            public string title;
            [TextArea(2, 4)] public string effect;
            [TextArea(2, 5)] public string meaning;
        }

        internal readonly struct StaminaPopup
        {
            internal readonly float Amount;
            private readonly float startedAt;

            internal StaminaPopup(float amount)
            {
                Amount = amount;
                startedAt = Time.unscaledTime;
            }

            internal float Progress => Mathf.Clamp01(Time.unscaledTime - startedAt);
            internal float Rise => Progress * 180f;
            internal string Text => Amount.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture);
            internal Color Tint
            {
                get
                {
                    Color color = Amount > 0f ? new Color(.25f, 1f, .4f) : new Color(1f, .25f, .25f);
                    color.a = 1f - Progress;
                    return color;
                }
            }
        }

        // Text is authored on the GameUI component of the Game object in Journey.unity.
        // Fields containing {0}, {1}, ... are format strings.
        [Header("Shared")]
        [SerializeField] private string gameName;

        [Header("Text sizes")]
        [SerializeField, Min(1)] private int titleFontSize = 52;
        [SerializeField, Min(1)] private int headingFontSize = 36;
        [SerializeField, Min(1)] private int titleInstructionsFontSize = 36;
        [SerializeField, Min(1)] private int bodyFontSize = 36;
        [SerializeField, Min(1)] private int smallFontSize = 36;
        [SerializeField, Min(1)] private int buttonFontSize = 22;
        [SerializeField, Min(1)] private int popupFontSize = 30;

        [Header("Title screen")]
        [SerializeField, TextArea] private string titleSubtitle;
        [SerializeField, TextArea(3, 6)] private string titleInstructions;
        [SerializeField] private string startButton;
        [SerializeField, TextArea] private string titleDisclaimer;

        [Header("HUD")]
        [Tooltip("{0} = scenario year")]
        [SerializeField] private string hudScenario;
        [Tooltip("{0} = current stamina")]
        [SerializeField] private string hudStamina;
        [Tooltip("{0} = seconds remaining")]
        [SerializeField] private string hudTime;
        [SerializeField] private string statusPreparing;
        [SerializeField] private string statusExhausted;
        [SerializeField] private string statusArrived;
        [SerializeField] private string statusOnIce;
        [SerializeField] private string statusSwimming;
        [Tooltip("{0} = scenario year")]
        [SerializeField] private string prepareHeading;
        [Tooltip("{0} = seconds until the journey begins")]
        [SerializeField, TextArea] private string prepareBody;
        [SerializeField] private string arrivalHeading;

        [Header("Choice screen")]
        [SerializeField] private string choiceTitle;
        [Tooltip("{0} = cleared scenario year, {1} = current stamina")]
        [SerializeField, TextArea] private string choiceSummary;
        [SerializeField, TextArea] private string choicePrompt;
        [Tooltip("{0} = option number, {1} = choice title")]
        [SerializeField] private string choiceOption;
        [Tooltip("{0} = option number")]
        [SerializeField] private string choiceButton;
        [Tooltip("Entries are ordered by stage, then option: 2016 options 1-3, followed by 2026 options 1-3.")]
        [SerializeField] private ChoiceText[] choiceTexts = new ChoiceText[6];

        [Header("Ending screen")]
        [SerializeField] private string victoryTitle;
        [SerializeField] private string defeatTitle;
        [SerializeField, TextArea] private string victoryMessage;
        [SerializeField, TextArea] private string defeatMessage;
        [Tooltip("{0} = stage reached, {1} = stages cleared")]
        [SerializeField, TextArea] private string endingProgress;
        [SerializeField] private string choicesHeading;
        [SerializeField] private string noChoices;
        [Tooltip("{0} = scenario year, {1} = choice title, {2} = choice meaning")]
        [SerializeField, TextArea] private string endingChoice;
        [SerializeField] private string actionsHeading;
        [SerializeField, TextArea(2, 4)] private string actionsBody;
        [SerializeField, TextArea(2, 4)] private string factsText;
        [SerializeField] private string playAgainButton;

        private GameController game;
        private Texture2D white;
        private GUIStyle title;
        private GUIStyle heading;
        private GUIStyle titleInstructionsStyle;
        private GUIStyle body;
        private GUIStyle small;
        private GUIStyle button;
        private GUIStyle popupStyle;
        private readonly List<StaminaPopup> staminaPopups = new List<StaminaPopup>();
        private float pendingDrain;
        private int pressedButton = -1;
        private GamePhase pressedPhase;

        private static Rect StartButtonRect => new Rect(140, 615, 380, 70);
        private static Rect ChoiceButtonRect(int index) => new Rect(119 + index * 480, 700, 402, 58);
        private static Rect PlayAgainButtonRect => new Rect(70, 770, 380, 64);

        internal IReadOnlyList<StaminaPopup> StaminaPopups => staminaPopups;

        private ChoiceText ChoiceTextAt(int stage, int index) => choiceTexts[stage * 3 + index];

        internal void ShowStaminaChange(float amount, bool continuous = false)
        {
            if (Mathf.Approximately(amount, 0f)) return;
            if (continuous && amount < 0f)
            {
                pendingDrain += amount;
                if (pendingDrain > -1f) return;
                amount = -Mathf.Floor(-pendingDrain);
                pendingDrain -= amount;
            }
            staminaPopups.Add(new StaminaPopup(amount));
        }

        internal void ClearStaminaPopups()
        {
            staminaPopups.Clear();
            pendingDrain = 0f;
        }

        private void Update()
        {
            for (int i = staminaPopups.Count - 1; i >= 0; i--)
                if (staminaPopups[i].Progress >= 1f) staminaPopups.RemoveAt(i);
            HandleMouse();
        }

        private void HandleMouse()
        {
            Mouse mouse = Mouse.current;
            if (game == null || mouse == null)
            {
                pressedButton = -1;
                return;
            }

            Vector2 position = mouse.position.ReadValue();
            position = new Vector2(position.x * 1600f / Screen.width, (Screen.height - position.y) * 900f / Screen.height);
            if (mouse.leftButton.wasPressedThisFrame)
            {
                pressedPhase = game.Phase;
                pressedButton = ButtonAt(position);
            }
            if (game.Phase != pressedPhase) pressedButton = -1;
            if (!mouse.leftButton.wasReleasedThisFrame) return;

            int clickedButton = ButtonAt(position);
            bool clicked = pressedButton >= 0 && clickedButton == pressedButton;
            pressedButton = -1;
            if (!clicked) return;
            if (game.Phase == GamePhase.Choice) game.SelectChoice(clickedButton);
            else game.StartRun();
        }

        private int ButtonAt(Vector2 position)
        {
            if (game.Phase == GamePhase.Title) return StartButtonRect.Contains(position) ? 0 : -1;
            if (game.Phase == GamePhase.Victory || game.Phase == GamePhase.Failure)
                return PlayAgainButtonRect.Contains(position) ? 0 : -1;
            if (game.Phase == GamePhase.Choice)
                for (int i = 0; i < 3; i++)
                    if (ChoiceButtonRect(i).Contains(position)) return i;
            return -1;
        }

        public void Initialize(GameController controller)
        {
            game = controller;
            white = new Texture2D(1, 1);
            white.SetPixel(0, 0, Color.white);
            white.Apply();
        }

        private void Styles()
        {
            title = Style(titleFontSize, FontStyle.Bold, new Color(.93f, .98f, .99f));
            heading = Style(headingFontSize, FontStyle.Bold, new Color(.92f, .97f, .98f));
            titleInstructionsStyle = Style(titleInstructionsFontSize, FontStyle.Normal, new Color(.86f, .94f, .95f));
            body = Style(bodyFontSize, FontStyle.Normal, new Color(.86f, .94f, .95f));
            small = Style(smallFontSize, FontStyle.Normal, new Color(.76f, .89f, .91f));
            button = Style(buttonFontSize, FontStyle.Bold, Color.white);
            button.alignment = TextAnchor.MiddleCenter;
            button.normal.background = white;
            button.hover.background = white;
            button.active.background = white;
            popupStyle = Style(popupFontSize, FontStyle.Bold, Color.white);
            popupStyle.alignment = TextAnchor.MiddleCenter;
            popupStyle.wordWrap = false;
        }

        private static GUIStyle Style(int size, FontStyle weight, Color color)
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = size;
            style.fontStyle = weight;
            style.wordWrap = true;
            style.normal.textColor = color;
            return style;
        }

        private void OnGUI()
        {
            if (game == null) return;
            Styles();
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1600f, Screen.height / 900f, 1));
            if (game.Phase == GamePhase.Preparing || game.Phase == GamePhase.Playing ||
                game.Phase == GamePhase.Arrival || game.Phase == GamePhase.Sinking)
            {
                DrawHud();
                DrawStaminaPopups();
            }
            else if (game.Phase == GamePhase.Title) DrawTitle();
            else if (game.Phase == GamePhase.Choice) DrawChoice();
            else DrawEnding();
            GUI.matrix = oldMatrix;
        }

        private void DrawStaminaPopups()
        {
            Vector3 point = Camera.main.WorldToViewportPoint(game.Player.HeadPosition + Vector3.up * .6f);
            if (point.z <= 0f) return;
            foreach (StaminaPopup popup in staminaPopups)
            {
                float x = Mathf.Clamp(point.x * 1600f + (popup.Amount > 0f ? 38f : -38f), 100f, 1500f);
                float y = (1f - point.y) * 900f - 35f - popup.Rise;
                Color color = popup.Tint;
                popupStyle.normal.textColor = new Color(0f, .05f, .08f, color.a);
                Text(x - 98f, y + 2f, 200f, 44f, popup.Text, popupStyle);
                popupStyle.normal.textColor = color;
                Text(x - 100f, y, 200f, 44f, popup.Text, popupStyle);
            }
        }

        private void Rect(float x, float y, float w, float h, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, w, h), white);
            GUI.color = previous;
        }

        private void Text(float x, float y, float w, float h, string text, GUIStyle style)
        {
            GUI.Label(new Rect(x, y, w, h), text, style);
        }

        private static string Format(string template, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, template ?? string.Empty, args);
        }

        private void Button(Rect rect, string text)
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = new Color(.12f, .5f, .55f);
            GUI.Box(rect, text, button);
            GUI.backgroundColor = previous;
        }

        private void Overlay()
        {
            Rect(0, 0, 1600, 900, new Color(.015f, .09f, .13f, .96f));
        }

        private void DrawTitle()
        {
            Overlay();
            Text(135, 80, 1200, 85, gameName, title);
            Text(140, 180, 1180, 80, titleSubtitle, heading);
            Text(140, 285, 1230, 170, titleInstructions, titleInstructionsStyle);
            Button(StartButtonRect, startButton);
            Text(140, 735, 1250, 75, titleDisclaimer, small);
        }

        private void DrawHud()
        {
            Rect(18, 18, 310, 278, new Color(.015f, .10f, .14f, .9f));
            Text(36, 34, 275, 43, gameName, heading);
            Text(36, 80, 270, 35, Format(hudScenario, JourneyData.Years[game.Stage]), body);
            Text(36, 120, 270, 35, Format(hudStamina, Mathf.CeilToInt(game.Stamina)), body);
            Rect(36, 161, 265, 22, new Color(.22f, .32f, .34f));
            Rect(36, 161, 265 * game.Stamina / 100f, 22, new Color(.48f, .86f, .72f));
            Text(36, 194, 270, 34, Format(hudTime, Mathf.CeilToInt(Mathf.Max(0, JourneyData.StageDuration - game.Elapsed))), body);
            Text(36, 233, 270, 35, game.Phase == GamePhase.Preparing ? statusPreparing :
                game.Phase == GamePhase.Sinking ? statusExhausted :
                game.Phase == GamePhase.Arrival ? statusArrived :
                game.Player.OnIce ? statusOnIce : statusSwimming, heading);

            if (game.Phase == GamePhase.Preparing)
            {
                Rect(475, 55, 650, 165, new Color(.015f, .10f, .14f, .9f));
                Text(505, 75, 590, 45, Format(prepareHeading, JourneyData.Years[game.Stage]), heading);
                Text(505, 127, 590, 75, Format(prepareBody, Mathf.CeilToInt(game.PreparationRemaining)), body);
            }
            else if (game.Phase == GamePhase.Arrival)
            {
                Rect(475, 55, 650, 78, new Color(.015f, .10f, .14f, .9f));
                Text(505, 75, 590, 45, arrivalHeading, heading);
            }
        }

        private void DrawChoice()
        {
            Overlay();
            Text(95, 55, 1420, 70, choiceTitle, title);
            Text(100, 145, 1350, 75,
                Format(choiceSummary, JourneyData.Years[game.Stage], Mathf.CeilToInt(game.Stamina)), body);
            Text(100, 215, 1350, 65, choicePrompt, heading);
            for (int i = 0; i < 3; i++)
            {
                float x = 95 + i * 480;
                ChoiceText choice = ChoiceTextAt(game.Stage, i);
                Rect(x, 300, 450, 390, new Color(.08f, .25f, .29f));
                Text(x + 24, 325, 402, 104, Format(choiceOption, i + 1, choice.title), heading);
                Text(x + 24, 435, 402, 95, choice.effect, body);
                Text(x + 24, 540, 402, 112, choice.meaning, small);
                Button(ChoiceButtonRect(i), Format(choiceButton, i + 1));
            }
        }

        private void DrawEnding()
        {
            Overlay();
            bool won = game.Phase == GamePhase.Victory;
            Text(65, 30, 1450, 68, won ? victoryTitle : defeatTitle, title);
            Text(70, 105, 1450, 70, (won ? victoryMessage : defeatMessage) +
                Format(endingProgress, JourneyData.Years[game.Stage], game.CompletedStages), body);
            Text(70, 183, 900, 35, choicesHeading, heading);
            if (game.SelectedChoices.Count == 0)
                Text(70, 227, 1400, 32, noChoices, body);
            for (int i = 0; i < game.SelectedChoices.Count; i++)
            {
                ChoiceText choice = ChoiceTextAt(i, game.SelectedChoiceIndices[i]);
                Text(70, 225 + i * 78, 1400, 72,
                    Format(endingChoice, JourneyData.Years[i], choice.title, choice.meaning), body);
            }
            Text(70, 394, 1450, 45, actionsHeading, heading);
            Text(70, 440, 1450, 72, actionsBody, body);
            Text(70, 525, 1450, 78, factsText, small);
            Button(PlayAgainButtonRect, playAgainButton);
        }
    }
}
