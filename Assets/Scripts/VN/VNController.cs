using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Verden.Story;

namespace Verden
{
    public sealed class VNController : MonoBehaviour
    {
        const float ReferenceWidth = 1920f;
        const float ReferenceHeight = 1080f;
        const float LettersPerSecond = 42f;

        Font font;
        StorySession session;
        ChapterInfo chapter;

        Image background;
        Image portrait;
        GameObject dialogueRoot;
        GameObject namePlate;
        Text nameText;
        Text bodyText;
        Text hintText;
        Text chapterMark;
        GameObject toastRoot;
        Text toastText;
        CanvasGroup toastGroup;
        RectTransform choiceColumn;
        GameObject advanceButton;
        GameObject titleRoot;
        Text titleNote;
        GameObject endRoot;
        Text endHeading;
        Text endBody;

        readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        readonly List<GameObject> choiceButtons = new List<GameObject>();

        Coroutine typing;
        Coroutine toastFade;
        string fullLine = "";
        bool lineReady;
        string shownBackground = "";
        string shownPortrait = "";

        static readonly Color Ink = new Color(0.07f, 0.045f, 0.035f, 0.94f);
        static readonly Color Parchment = new Color(0.95f, 0.91f, 0.84f, 1f);
        static readonly Color Gold = new Color(0.78f, 0.66f, 0.42f, 1f);
        static readonly Color Muted = new Color(0.72f, 0.64f, 0.54f, 1f);
        static readonly Color ButtonFace = new Color(0.16f, 0.1f, 0.08f, 0.96f);

        void Awake()
        {
            font = Resources.Load<Font>("Fonts/NotoSans-Regular");
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(
                    new[] { "DejaVu Sans", "Liberation Sans", "Noto Sans", "Arial" },
                    32);
            }

            EnsureEventSystem();
            BuildCanvas();
            ShowTitle();
        }

        void Update()
        {
            if (!AdvanceAllowed()) return;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                OnAdvance();
        }

        bool AdvanceAllowed()
        {
            return dialogueRoot != null
                && dialogueRoot.activeSelf
                && advanceButton != null
                && advanceButton.activeSelf
                && (titleRoot == null || !titleRoot.activeSelf)
                && (endRoot == null || !endRoot.activeSelf);
        }

        void EnsureEventSystem()
        {
#if UNITY_2023_1_OR_NEWER
            EventSystem existing = Object.FindFirstObjectByType<EventSystem>();
#else
            EventSystem existing = Object.FindObjectOfType<EventSystem>();
#endif
            if (existing != null) return;
            GameObject systems = new GameObject("EventSystem");
            systems.AddComponent<EventSystem>();
            systems.AddComponent<StandaloneInputModule>();
        }

        void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("NovelCanvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            background = CreateImage("Background", canvasObject.transform, Color.black);
            Stretch(background.rectTransform);
            background.raycastTarget = false;

            portrait = CreateImage("Portrait", canvasObject.transform, Color.white);
            RectTransform portraitRect = portrait.rectTransform;
            portraitRect.anchorMin = new Vector2(1f, 0f);
            portraitRect.anchorMax = new Vector2(1f, 0f);
            portraitRect.pivot = new Vector2(1f, 0f);
            portraitRect.anchoredPosition = new Vector2(-12f, 0f);
            portraitRect.sizeDelta = new Vector2(700f, 1040f);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portrait.gameObject.SetActive(false);

            dialogueRoot = CreateFull("Dialogue", canvasObject.transform);
            Image panel = CreateImage("Panel", dialogueRoot.transform, Ink);
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.offsetMin = new Vector2(36f, 22f);
            panelRect.offsetMax = new Vector2(-36f, 272f);

            Image rule = CreateImage("Rule", panel.transform, Gold);
            RectTransform ruleRect = rule.rectTransform;
            ruleRect.anchorMin = new Vector2(0f, 1f);
            ruleRect.anchorMax = new Vector2(1f, 1f);
            ruleRect.pivot = new Vector2(0.5f, 1f);
            ruleRect.sizeDelta = new Vector2(0f, 2f);
            ruleRect.anchoredPosition = Vector2.zero;
            rule.raycastTarget = false;

            Image nameBack = CreateImage("NameBack", dialogueRoot.transform, new Color(0.12f, 0.07f, 0.05f, 0.98f));
            namePlate = nameBack.gameObject;
            RectTransform nameRect = nameBack.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(0f, 0f);
            nameRect.pivot = new Vector2(0f, 0f);
            nameRect.anchoredPosition = new Vector2(56f, 262f);
            nameRect.sizeDelta = new Vector2(480f, 54f);
            nameText = CreateText("Name", nameBack.transform, 30, FontStyle.Normal, Gold, TextAnchor.MiddleLeft);
            Stretch(nameText.rectTransform, 18f, 4f, 12f, 4f);

            bodyText = CreateText("Body", panel.transform, 30, FontStyle.Normal, Parchment, TextAnchor.UpperLeft);
            Stretch(bodyText.rectTransform, 28f, 28f, 700f, 22f);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Truncate;

            hintText = CreateText("Hint", panel.transform, 22, FontStyle.Normal, Muted, TextAnchor.LowerRight);
            RectTransform hintRect = hintText.rectTransform;
            hintRect.anchorMin = new Vector2(1f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(1f, 0f);
            hintRect.anchoredPosition = new Vector2(-716f, 12f);
            hintRect.sizeDelta = new Vector2(200f, 32f);

            advanceButton = CreateButton("Advance", dialogueRoot.transform, "", Ink, OnAdvance).gameObject;
            Image advanceImage = advanceButton.GetComponent<Image>();
            advanceImage.color = new Color(0f, 0f, 0f, 0f);
            Stretch(advanceButton.GetComponent<RectTransform>());

            chapterMark = CreateText("ChapterMark", canvasObject.transform, 22, FontStyle.Normal, Gold, TextAnchor.MiddleLeft);
            RectTransform markRect = chapterMark.rectTransform;
            markRect.anchorMin = new Vector2(0f, 1f);
            markRect.anchorMax = new Vector2(0f, 1f);
            markRect.pivot = new Vector2(0f, 1f);
            markRect.anchoredPosition = new Vector2(40f, -24f);
            markRect.sizeDelta = new Vector2(760f, 36f);
            chapterMark.raycastTarget = false;

            choiceColumn = CreateRect("Choices", canvasObject.transform);
            choiceColumn.anchorMin = new Vector2(0f, 0f);
            choiceColumn.anchorMax = new Vector2(0f, 0f);
            choiceColumn.pivot = new Vector2(0f, 0f);
            choiceColumn.anchoredPosition = new Vector2(56f, 310f);
            choiceColumn.sizeDelta = new Vector2(1040f, 340f);
            VerticalLayoutGroup layout = choiceColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            toastRoot = CreateFrame("StatFrame", canvasObject.transform, out toastText, 32);
            RectTransform toastRect = toastRoot.GetComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 0.5f);
            toastRect.anchorMax = new Vector2(0.5f, 0.5f);
            toastRect.pivot = new Vector2(0.5f, 0.5f);
            toastRect.anchoredPosition = new Vector2(0f, 70f);
            toastRect.sizeDelta = new Vector2(640f, 120f);
            toastGroup = toastRoot.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
            toastGroup.interactable = false;

            titleRoot = BuildTitle(canvasObject.transform);
            endRoot = BuildEnd(canvasObject.transform);
        }

        GameObject BuildTitle(Transform parent)
        {
            GameObject root = CreateFull("Title", parent);
            Image dim = CreateImage("Dim", root.transform, new Color(0.03f, 0.02f, 0.015f, 0.45f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            Text kicker = CreateText("Kicker", root.transform, 22, FontStyle.Normal, Gold, TextAnchor.MiddleCenter);
            Place(kicker.rectTransform, 0f, 250f, 900f, 36f);
            kicker.text = "КОРОЛЕВСТВО ВЕРДЕН";

            Text title = CreateText("TitleText", root.transform, 78, FontStyle.Normal, Parchment, TextAnchor.MiddleCenter);
            Place(title.rectTransform, 0f, 160f, 1200f, 100f);
            title.text = "Цена герба";

            Text sub = CreateText("Sub", root.transform, 28, FontStyle.Italic, Muted, TextAnchor.MiddleCenter);
            Place(sub.rectTransform, 0f, 90f, 900f, 40f);
            sub.text = "Дворцовый детектив";

            ChapterInfo[] chapters = StoryCatalog.Chapters();
            for (int i = 0; i < chapters.Length; i++)
            {
                ChapterInfo info = chapters[i];
                float y = 10f - i * 78f;
                string label = "Глава " + Roman(info.Number) + ".  " + info.Title;
                if (!info.Playable) label += "    ·    скоро";
                int captured = i;
                Button button = CreateButton("Chapter" + info.Number, root.transform, label, ButtonFace, delegate { OnChapterPressed(captured); });
                RectTransform rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, y);
                rect.sizeDelta = new Vector2(820f, 68f);
                if (!info.Playable)
                {
                    ColorBlock colors = button.colors;
                    colors.normalColor = new Color(0.1f, 0.07f, 0.06f, 0.85f);
                    colors.highlightedColor = new Color(0.14f, 0.1f, 0.08f, 0.9f);
                    button.colors = colors;
                }
            }

            titleNote = CreateText("Note", root.transform, 22, FontStyle.Italic, Muted, TextAnchor.MiddleCenter);
            Place(titleNote.rectTransform, 0f, -400f, 1100f, 36f);
            titleNote.text = "Глава занимает около пятнадцати минут.";
            return root;
        }

        GameObject BuildEnd(Transform parent)
        {
            GameObject root = CreateFull("End", parent);
            Image dim = CreateImage("Dim", root.transform, new Color(0.04f, 0.025f, 0.02f, 0.82f));
            Stretch(dim.rectTransform);

            endHeading = CreateText("Heading", root.transform, 42, FontStyle.Normal, Parchment, TextAnchor.MiddleCenter);
            Place(endHeading.rectTransform, 0f, 250f, 1000f, 60f);
            endHeading.text = "Глава пройдена";

            GameObject endFrame = CreateFrame("StatFrame", root.transform, out endBody, 30);
            RectTransform endFrameRect = endFrame.GetComponent<RectTransform>();
            endFrameRect.anchorMin = new Vector2(0.5f, 0.5f);
            endFrameRect.anchorMax = new Vector2(0.5f, 0.5f);
            endFrameRect.pivot = new Vector2(0.5f, 0.5f);
            endFrameRect.anchoredPosition = new Vector2(0f, 10f);
            endFrameRect.sizeDelta = new Vector2(760f, 380f);
            endBody.alignment = TextAnchor.MiddleCenter;

            Button back = CreateButton("Back", root.transform, "К оглавлению", ButtonFace, ShowTitle);
            RectTransform backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0.5f);
            backRect.anchorMax = new Vector2(0.5f, 0.5f);
            backRect.sizeDelta = new Vector2(420f, 68f);
            backRect.anchoredPosition = new Vector2(0f, -280f);
            root.SetActive(false);
            return root;
        }

        void OnChapterPressed(int index)
        {
            ChapterInfo[] chapters = StoryCatalog.Chapters();
            ChapterInfo info = chapters[index];
            if (!info.Playable || info.Beats == null)
            {
                titleNote.text = "Глава " + Roman(info.Number) + " ещё пишется. Сейчас можно пройти первую.";
                return;
            }

            chapter = info;
            session = new StorySession();
            session.Begin(info.Beats);
            titleRoot.SetActive(false);
            endRoot.SetActive(false);
            dialogueRoot.SetActive(true);
            chapterMark.text = "Глава " + Roman(info.Number) + "  ·  " + info.Title;
            Present();
        }

        void ShowTitle()
        {
            StopTyping();
            ClearChoices();
            titleRoot.SetActive(true);
            endRoot.SetActive(false);
            dialogueRoot.SetActive(false);
            portrait.gameObject.SetActive(false);
            chapterMark.text = "";
            toastGroup.alpha = 0f;
            shownBackground = "";
            shownPortrait = "";
            SetBackground(Bg.Title);
            titleNote.text = "Глава занимает около пятнадцати минут.";
        }

        void Present()
        {
            if (session == null || session.Finished || session.Current == null)
            {
                ShowEnd();
                return;
            }

            Beat beat = session.Current;
            if (!string.IsNullOrEmpty(beat.Background)) SetBackground(beat.Background);
            ApplyPortrait(beat.Portrait);

            bool narration = string.IsNullOrEmpty(beat.Speaker);
            namePlate.SetActive(!narration);
            nameText.text = narration ? "" : beat.Speaker;
            bodyText.fontStyle = narration ? FontStyle.Italic : FontStyle.Normal;
            bodyText.color = narration ? new Color(0.9f, 0.86f, 0.78f, 1f) : Parchment;
            ClearChoices();
            hintText.text = "";
            advanceButton.SetActive(true);
            BeginTyping(beat.Text);
        }

        void BeginTyping(string text)
        {
            StopTyping();
            fullLine = text ?? "";
            lineReady = false;
            if (font != null) font.RequestCharactersInTexture(fullLine, bodyText.fontSize, bodyText.fontStyle);
            typing = StartCoroutine(TypeLine());
        }

        IEnumerator TypeLine()
        {
            bodyText.text = "";
            float shown = 0f;
            while (shown < fullLine.Length)
            {
                shown += LettersPerSecond * Time.deltaTime;
                int count = Mathf.Clamp(Mathf.FloorToInt(shown), 0, fullLine.Length);
                bodyText.text = fullLine.Substring(0, count);
                yield return null;
            }

            FinishLine();
        }

        void FinishLine()
        {
            StopTyping();
            bodyText.text = fullLine;
            lineReady = true;
            if (session != null && session.HasChoices)
            {
                advanceButton.SetActive(false);
                hintText.text = "";
                BuildChoices();
            }
            else
            {
                hintText.text = "дальше";
            }
        }

        void StopTyping()
        {
            if (typing == null) return;
            StopCoroutine(typing);
            typing = null;
        }

        void OnAdvance()
        {
            if (session == null) return;
            if (!lineReady)
            {
                FinishLine();
                return;
            }

            if (session.HasChoices) return;
            session.Advance();
            if (session.Finished) ShowEnd();
            else Present();
        }

        void BuildChoices()
        {
            ClearChoices();
            Choice[] options = session.Current.Choices;
            for (int i = 0; i < options.Length; i++)
            {
                int captured = i;
                Button button = CreateButton(
                    "Choice" + i,
                    choiceColumn,
                    options[i].Text,
                    ButtonFace,
                    delegate { OnChoice(captured); });
                LayoutElement element = button.gameObject.AddComponent<LayoutElement>();
                element.minHeight = 72f;
                element.preferredHeight = 84f;
                choiceButtons.Add(button.gameObject);
            }
        }

        void OnChoice(int index)
        {
            if (session == null || !session.HasChoices) return;
            session.Choose(index);
            ShowToast(session.LastEffects);
            ClearChoices();
            if (session.Finished) ShowEnd();
            else Present();
        }

        void ShowToast(IList<StatDelta> effects)
        {
            if (effects == null || effects.Count == 0)
            {
                toastGroup.alpha = 0f;
                return;
            }

            string line = "";
            for (int i = 0; i < effects.Count; i++)
            {
                if (i > 0) line += "\n";
                int delta = effects[i].Delta;
                string sign = delta > 0 ? "+" : "";
                line += StatNames.Of(effects[i].Stat) + "    " + sign + delta;
            }

            toastText.text = line;
            RectTransform toastRect = toastRoot.GetComponent<RectTransform>();
            toastRect.sizeDelta = new Vector2(680f, 48f + effects.Count * 48f);
            if (font != null) font.RequestCharactersInTexture(line, toastText.fontSize, toastText.fontStyle);
            if (toastFade != null) StopCoroutine(toastFade);
            toastFade = StartCoroutine(FadeToast());
        }

        IEnumerator FadeToast()
        {
            toastGroup.alpha = 1f;
            yield return new WaitForSeconds(2.8f);
            float left = 0.45f;
            while (left > 0f)
            {
                left -= Time.deltaTime;
                toastGroup.alpha = Mathf.Clamp01(left / 0.45f);
                yield return null;
            }

            toastGroup.alpha = 0f;
        }

        void ShowEnd()
        {
            StopTyping();
            ClearChoices();
            dialogueRoot.SetActive(false);
            portrait.gameObject.SetActive(false);
            advanceButton.SetActive(false);
            endRoot.SetActive(true);
            chapterMark.text = "";
            if (chapter != null)
                endHeading.text = "Глава " + Roman(chapter.Number) + " пройдена";

            string body = "";
            foreach (Stat stat in System.Enum.GetValues(typeof(Stat)))
            {
                body += StatNames.Of(stat) + "  —  " + session.Stats[stat] + "\n";
            }

            body += "\n";
            if (session.Flags.Contains(StoryFlags.AlienTail))
                body += "Лоскут с двумя подписями спрятан в поясе.";
            else
                body += "Лоскут сожжён. Разница подписей осталась только в памяти.";

            endBody.text = body;
            if (font != null) font.RequestCharactersInTexture(body, endBody.fontSize, endBody.fontStyle);
        }

        void ClearChoices()
        {
            for (int i = 0; i < choiceButtons.Count; i++)
            {
                if (choiceButtons[i] != null) Destroy(choiceButtons[i]);
            }

            choiceButtons.Clear();
        }

        void SetBackground(string key)
        {
            if (key == shownBackground) return;
            shownBackground = key;
            Sprite sprite = LoadSprite("Backgrounds/" + key);
            background.sprite = sprite;
            background.color = sprite == null ? new Color(0.08f, 0.06f, 0.05f, 1f) : Color.white;
        }

        void ApplyPortrait(string key)
        {
            if (key == null) return;
            if (key.Length == 0)
            {
                portrait.sprite = null;
                portrait.gameObject.SetActive(false);
                shownPortrait = "";
                return;
            }

            if (key == shownPortrait && portrait.gameObject.activeSelf) return;
            shownPortrait = key;
            Sprite sprite = LoadSprite("Characters/" + key);
            portrait.sprite = sprite;
            portrait.gameObject.SetActive(sprite != null);
        }

        Sprite LoadSprite(string resourcePath)
        {
            Sprite cached;
            if (sprites.TryGetValue(resourcePath, out cached)) return cached;
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }
            }

            sprites[resourcePath] = sprite;
            return sprite;
        }

        GameObject CreateFrame(string name, Transform parent, out Text label, int fontSize)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.AddComponent<RectTransform>();
            Image border = root.AddComponent<Image>();
            border.color = Gold;
            border.raycastTarget = false;

            Image fill = CreateImage("Fill", root.transform, new Color(0.07f, 0.045f, 0.03f, 0.96f));
            Stretch(fill.rectTransform, 4f, 4f, 4f, 4f);
            fill.raycastTarget = false;

            label = CreateText("Label", fill.transform, fontSize, FontStyle.Normal, Parchment, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 28f, 16f, 28f, 16f);
            return root;
        }

        Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        Text CreateText(string name, Transform parent, int size, FontStyle style, Color color, TextAnchor anchor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            if (font != null) text.font.RequestCharactersInTexture("Цена герба Агата", size, style);
            return text;
        }

        Button CreateButton(string name, Transform parent, string label, Color face, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = face;
            Button button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = face;
            colors.highlightedColor = new Color(0.28f, 0.16f, 0.12f, 1f);
            colors.pressedColor = new Color(0.1f, 0.06f, 0.05f, 1f);
            colors.selectedColor = face;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.targetGraphic = image;
            if (onClick != null) button.onClick.AddListener(onClick);

            if (!string.IsNullOrEmpty(label))
            {
                Text text = CreateText("Label", go.transform, 26, FontStyle.Normal, Parchment, TextAnchor.MiddleLeft);
                Stretch(text.rectTransform, 22f, 8f, 18f, 8f);
                text.text = label;
                if (font != null) font.RequestCharactersInTexture(label, text.fontSize, text.fontStyle);
            }

            return button;
        }

        GameObject CreateFull(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            Stretch(rect);
            return go;
        }

        RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        static void Stretch(RectTransform rect)
        {
            Stretch(rect, 0f, 0f, 0f, 0f);
        }

        static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        static string Roman(int number)
        {
            switch (number)
            {
                case 1: return "I";
                case 2: return "II";
                case 3: return "III";
                case 4: return "IV";
                case 5: return "V";
                default: return number.ToString();
            }
        }
    }
}
