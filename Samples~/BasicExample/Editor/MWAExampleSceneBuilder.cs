#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public class MWAExampleSceneBuilder
{
    // Color palette
    static readonly Color BgColor = new Color(0.09f, 0.09f, 0.11f, 1f);
    static readonly Color CardColor = new Color(0.14f, 0.14f, 0.18f, 1f);
    static readonly Color CardHeaderColor = new Color(0.18f, 0.18f, 0.23f, 1f);
    static readonly Color TextPrimary = Color.white;
    static readonly Color TextSecondary = new Color(0.65f, 0.65f, 0.7f);
    static readonly Color TextMuted = new Color(0.45f, 0.45f, 0.5f);
    static readonly Color AccentGreen = new Color(0.2f, 0.75f, 0.4f);
    static readonly Color AccentBlue = new Color(0.25f, 0.5f, 0.9f);
    static readonly Color AccentRed = new Color(0.8f, 0.25f, 0.25f);
    static readonly Color AccentOrange = new Color(0.85f, 0.55f, 0.2f);
    static readonly Color AccentCyan = new Color(0.2f, 0.7f, 0.75f);
    static readonly Color AccentPurple = new Color(0.5f, 0.35f, 0.75f);
    static readonly Color BtnDisabled = new Color(0.25f, 0.25f, 0.3f);
    static readonly Color LogBg = new Color(0.06f, 0.06f, 0.08f, 1f);

    const int FontTitle = 48;
    const int FontSectionHeader = 36;
    const int FontLabel = 30;
    const int FontValue = 28;
    const int FontButton = 30;
    const int FontLog = 24;
    const int FontSmall = 22;

    const float CardSpacing = 16f;
    const float CardPadding = 24f;
    const float RowHeight = 56f;
    const float ButtonHeight = 80f;
    const float SectionHeaderHeight = 60f;

    [MenuItem("Solana/MWA/Create Example Scene")]
    public static void CreateExampleScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // --- Canvas ---
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // --- Background ---
        var bg = CreateUIObject(canvasGO.transform, "Background");
        bg.AddComponent<Image>().color = BgColor;
        StretchFill(bg);

        // --- Main ScrollView ---
        var scrollGO = CreateUIObject(bg.transform, "MainScroll");
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 40f;
        var scrollImg = scrollGO.AddComponent<Image>();
        scrollImg.color = Color.clear;
        scrollRect.GetComponent<Image>().raycastTarget = true;
        StretchFill(scrollGO);
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.offsetMin = new Vector2(0, 0);
        scrollRT.offsetMax = new Vector2(0, 0);

        // Viewport
        var viewport = CreateUIObject(scrollGO.transform, "Viewport");
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        StretchFill(viewport);

        // Content container
        var content = CreateUIObject(viewport.transform, "Content");
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.offsetMin = new Vector2(16, 0);
        contentRT.offsetMax = new Vector2(-16, 0);
        var contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = CardSpacing;
        contentLayout.padding = new RectOffset(0, 0, 16, 24);
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        var contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        // ===================================================================
        // CARD 1: SESSION
        // ===================================================================
        var sessionCard = CreateCard(content.transform, "SessionCard", "Session");

        // Connection row
        var connRow = CreateStatusRow(sessionCard.transform, "ConnectionRow", "Connection");
        var connValue = connRow.transform.Find("Value").GetComponent<Text>();

        // Auth row
        var authRow = CreateStatusRow(sessionCard.transform, "AuthRow", "Authorization");
        var authValue = authRow.transform.Find("Value").GetComponent<Text>();

        // Wallet row
        var walletRow = CreateStatusRow(sessionCard.transform, "WalletRow", "Wallet");
        var walletValue = walletRow.transform.Find("Value").GetComponent<Text>();

        // Auth token row
        var tokenRow = CreateStatusRow(sessionCard.transform, "TokenRow", "Auth Token");
        var tokenValue = tokenRow.transform.Find("Value").GetComponent<Text>();

        // Cluster selector row
        var clusterRow = CreateUIObject(sessionCard.transform, "ClusterRow");
        var clusterRowLayout = clusterRow.AddComponent<HorizontalLayoutGroup>();
        clusterRowLayout.spacing = 12;
        clusterRowLayout.padding = new RectOffset((int)CardPadding, (int)CardPadding, 8, 8);
        clusterRowLayout.childForceExpandWidth = false;
        clusterRowLayout.childForceExpandHeight = true;
        clusterRowLayout.childControlWidth = true;
        clusterRowLayout.childControlHeight = true;
        var clusterRowLE = clusterRow.AddComponent<LayoutElement>();
        clusterRowLE.preferredHeight = RowHeight + 8;

        var clusterLabel = CreateUIObject(clusterRow.transform, "ClusterLabel");
        var clusterLabelText = clusterLabel.AddComponent<Text>();
        clusterLabelText.text = "Cluster";
        clusterLabelText.fontSize = FontLabel;
        clusterLabelText.color = TextSecondary;
        clusterLabelText.font = GetFont();
        clusterLabelText.alignment = TextAnchor.MiddleLeft;
        var clusterLabelLE = clusterLabel.AddComponent<LayoutElement>();
        clusterLabelLE.flexibleWidth = 1;

        var clusterDropdown = CreateDropdown(clusterRow.transform, "ClusterDropdown");
        var clusterDDLE = clusterDropdown.AddComponent<LayoutElement>();
        clusterDDLE.preferredWidth = 340;
        clusterDDLE.preferredHeight = RowHeight;

        // Session buttons
        var sessionBtnRow = CreateButtonRow(sessionCard.transform, "SessionButtons");
        var connectBtn = CreateButton(sessionBtnRow.transform, "ConnectBtn", "Connect", AccentGreen);
        var authorizeBtn = CreateButton(sessionBtnRow.transform, "AuthorizeBtn", "Authorize", AccentBlue);
        var sessionBtnRow2 = CreateButtonRow(sessionCard.transform, "SessionButtons2");
        var deauthorizeBtn = CreateButton(sessionBtnRow2.transform, "DeauthorizeBtn", "Deauthorize", AccentRed);
        var reconnectBtn = CreateButton(sessionBtnRow2.transform, "ReconnectBtn", "Reconnect", AccentPurple);

        // ===================================================================
        // CARD 2: WALLET METHODS
        // ===================================================================
        var methodsCard = CreateCard(content.transform, "WalletMethodsCard", "Wallet Methods");

        var methodBtnRow1 = CreateButtonRow(methodsCard.transform, "MethodButtons1");
        var signTxBtn = CreateButton(methodBtnRow1.transform, "SignTxBtn", "sign_transactions", AccentOrange);
        var signMsgBtn = CreateButton(methodBtnRow1.transform, "SignMsgBtn", "sign_messages", AccentCyan);

        var methodBtnRow2 = CreateButtonRow(methodsCard.transform, "MethodButtons2");
        var signSendBtn = CreateButton(methodBtnRow2.transform, "SignSendBtn", "sign_and_send_transactions", AccentOrange);

        var methodBtnRow3 = CreateButtonRow(methodsCard.transform, "MethodButtons3");
        var capabilitiesBtn = CreateButton(methodBtnRow3.transform, "CapabilitiesBtn", "get_capabilities", AccentPurple);
        var cloneAuthBtn = CreateButton(methodBtnRow3.transform, "CloneAuthBtn", "clone_authorization", AccentBlue);

        // ===================================================================
        // CARD 3: AUTHORIZATION CACHE
        // ===================================================================
        var cacheCard = CreateCard(content.transform, "AuthCacheCard", "Authorization Cache");

        var cacheStatusRow = CreateStatusRow(cacheCard.transform, "CacheStatusRow", "Token");
        var cacheStatusValue = cacheStatusRow.transform.Find("Value").GetComponent<Text>();

        var lastSessionRow = CreateStatusRow(cacheCard.transform, "LastSessionRow", "Last Session");
        var lastSessionValue = lastSessionRow.transform.Find("Value").GetComponent<Text>();

        var cacheBtnRow = CreateButtonRow(cacheCard.transform, "CacheButtons");
        var clearCacheBtn = CreateButton(cacheBtnRow.transform, "ClearCacheBtn", "Clear Cache", AccentRed);
        var reuseSessionBtn = CreateButton(cacheBtnRow.transform, "ReuseSessionBtn", "Reuse Session", AccentGreen);

        // ===================================================================
        // CARD 4: PROTOCOL ACTIVITY LOG
        // ===================================================================
        var logCard = CreateCard(content.transform, "ProtocolLogCard", "Protocol Activity");

        // Log scroll area inside card
        var logScrollGO = CreateUIObject(logCard.transform, "LogScroll");
        var logScrollRect = logScrollGO.AddComponent<ScrollRect>();
        logScrollRect.horizontal = false;
        logScrollRect.vertical = true;
        logScrollRect.movementType = ScrollRect.MovementType.Elastic;
        logScrollRect.scrollSensitivity = 30f;
        var logScrollImg = logScrollGO.AddComponent<Image>();
        logScrollImg.color = LogBg;
        logScrollImg.raycastTarget = true;
        var logScrollLE = logScrollGO.AddComponent<LayoutElement>();
        logScrollLE.preferredHeight = 500;
        logScrollLE.flexibleHeight = 1;

        var logViewport = CreateUIObject(logScrollGO.transform, "LogViewport");
        logViewport.AddComponent<Image>().color = Color.clear;
        logViewport.AddComponent<Mask>().showMaskGraphic = false;
        StretchFill(logViewport);
        var logViewportRT = logViewport.GetComponent<RectTransform>();
        logViewportRT.offsetMin = new Vector2(12, 8);
        logViewportRT.offsetMax = new Vector2(-12, -8);

        var logContent = CreateUIObject(logViewport.transform, "LogContent");
        var logContentRT = logContent.GetComponent<RectTransform>();
        logContentRT.anchorMin = new Vector2(0, 1);
        logContentRT.anchorMax = new Vector2(1, 1);
        logContentRT.pivot = new Vector2(0.5f, 1);
        logContentRT.offsetMin = new Vector2(0, 0);
        logContentRT.offsetMax = new Vector2(0, 0);
        var logContentFitter = logContent.AddComponent<ContentSizeFitter>();
        logContentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var outputLog = CreateUIObject(logContent.transform, "OutputLog");
        var outputText = outputLog.AddComponent<Text>();
        outputText.text = "";
        outputText.fontSize = FontLog;
        outputText.color = new Color(0.75f, 0.9f, 0.75f);
        outputText.font = GetFont();
        outputText.alignment = TextAnchor.UpperLeft;
        outputText.horizontalOverflow = HorizontalWrapMode.Wrap;
        outputText.verticalOverflow = VerticalWrapMode.Overflow;
        outputText.supportRichText = true;
        outputText.raycastTarget = false;
        var outputLE = outputLog.AddComponent<LayoutElement>();
        outputLE.flexibleWidth = 1;
        StretchFill(outputLog);

        logScrollRect.content = logContentRT;
        logScrollRect.viewport = logViewportRT;

        // Bottom spacer for scroll padding
        var spacer = CreateUIObject(content.transform, "BottomSpacer");
        var spacerLE = spacer.AddComponent<LayoutElement>();
        spacerLE.preferredHeight = 40;

        // ===================================================================
        // WIRE CONTROLLER
        // ===================================================================
        var controller = canvasGO.AddComponent<MWAExampleController>();

        // Session card refs
        controller.ConnectionValue = connValue;
        controller.AuthValue = authValue;
        controller.WalletValue = walletValue;
        controller.TokenValue = tokenValue;
        controller.ClusterDropdown = clusterDropdown.GetComponent<Dropdown>();
        controller.ConnectBtn = connectBtn.GetComponent<Button>();
        controller.AuthorizeBtn = authorizeBtn.GetComponent<Button>();
        controller.DeauthorizeBtn = deauthorizeBtn.GetComponent<Button>();
        controller.ReconnectBtn = reconnectBtn.GetComponent<Button>();

        // Wallet methods refs
        controller.SignTxBtn = signTxBtn.GetComponent<Button>();
        controller.SignMsgBtn = signMsgBtn.GetComponent<Button>();
        controller.SignSendBtn = signSendBtn.GetComponent<Button>();
        controller.CapabilitiesBtn = capabilitiesBtn.GetComponent<Button>();
        controller.CloneAuthBtn = cloneAuthBtn.GetComponent<Button>();

        // Cache refs
        controller.CacheStatusValue = cacheStatusValue;
        controller.LastSessionValue = lastSessionValue;
        controller.ClearCacheBtn = clearCacheBtn.GetComponent<Button>();
        controller.ReuseSessionBtn = reuseSessionBtn.GetComponent<Button>();

        // Log refs
        controller.OutputLog = outputText;
        controller.LogScrollRect = logScrollRect;

        // Save
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MWAExample.unity");
        EditorSceneManager.OpenScene("Assets/Scenes/MWAExample.unity");
        Debug.Log("MWA Example Scene created at Assets/Scenes/MWAExample.unity");
    }

    // =====================================================================
    // BUILDER HELPERS
    // =====================================================================

    static GameObject CreateUIObject(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void StretchFill(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Font GetFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null) return f;
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    static GameObject CreateCard(Transform parent, string name, string title)
    {
        var card = CreateUIObject(parent, name);
        var cardImg = card.AddComponent<Image>();
        cardImg.color = CardColor;
        cardImg.raycastTarget = false;
        var cardLayout = card.AddComponent<VerticalLayoutGroup>();
        cardLayout.spacing = 0;
        cardLayout.padding = new RectOffset(0, 0, 0, 12);
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;

        // Section header
        var header = CreateUIObject(card.transform, "Header");
        var headerImg = header.AddComponent<Image>();
        headerImg.color = CardHeaderColor;
        headerImg.raycastTarget = false;
        var headerLE = header.AddComponent<LayoutElement>();
        headerLE.preferredHeight = SectionHeaderHeight;

        var headerText = CreateUIObject(header.transform, "HeaderText");
        var txt = headerText.AddComponent<Text>();
        txt.text = title;
        txt.fontSize = FontSectionHeader;
        txt.fontStyle = FontStyle.Bold;
        txt.color = TextPrimary;
        txt.font = GetFont();
        txt.alignment = TextAnchor.MiddleLeft;
        txt.raycastTarget = false;
        StretchFill(headerText);
        var headerTextRT = headerText.GetComponent<RectTransform>();
        headerTextRT.offsetMin = new Vector2(CardPadding, 0);
        headerTextRT.offsetMax = new Vector2(-CardPadding, 0);

        return card;
    }

    static GameObject CreateStatusRow(Transform parent, string name, string label)
    {
        var row = CreateUIObject(parent, name);
        var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12;
        rowLayout.padding = new RectOffset((int)CardPadding, (int)CardPadding, 4, 4);
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = RowHeight;

        var labelGO = CreateUIObject(row.transform, "Label");
        var labelText = labelGO.AddComponent<Text>();
        labelText.text = label;
        labelText.fontSize = FontLabel;
        labelText.color = TextSecondary;
        labelText.font = GetFont();
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.raycastTarget = false;
        var labelLE = labelGO.AddComponent<LayoutElement>();
        labelLE.preferredWidth = 300;

        var valueGO = CreateUIObject(row.transform, "Value");
        var valueText = valueGO.AddComponent<Text>();
        valueText.text = "---";
        valueText.fontSize = FontValue;
        valueText.color = TextMuted;
        valueText.font = GetFont();
        valueText.alignment = TextAnchor.MiddleRight;
        valueText.raycastTarget = false;
        var valueLE = valueGO.AddComponent<LayoutElement>();
        valueLE.flexibleWidth = 1;

        return row;
    }

    static GameObject CreateButtonRow(Transform parent, string name)
    {
        var row = CreateUIObject(parent, name);
        var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12;
        rowLayout.padding = new RectOffset((int)CardPadding, (int)CardPadding, 8, 8);
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = ButtonHeight;

        return row;
    }

    static GameObject CreateButton(Transform parent, string name, string label, Color color)
    {
        var go = CreateUIObject(parent, name);
        var image = go.AddComponent<Image>();
        image.color = color;
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        btn.colors = colors;

        var textGO = CreateUIObject(go.transform, "Text");
        var text = textGO.AddComponent<Text>();
        text.text = label;
        text.fontSize = FontButton;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = GetFont();
        text.fontStyle = FontStyle.Bold;
        text.raycastTarget = false;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 18;
        text.resizeTextMaxSize = FontButton;
        StretchFill(textGO);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.offsetMin = new Vector2(8, 4);
        textRT.offsetMax = new Vector2(-8, -4);

        return go;
    }

    static GameObject CreateDropdown(Transform parent, string name)
    {
        var ddGO = DefaultControls.CreateDropdown(new DefaultControls.Resources());
        ddGO.name = name;
        ddGO.transform.SetParent(parent, false);
        ddGO.GetComponent<Image>().color = new Color(0.22f, 0.22f, 0.28f);

        var dd = ddGO.GetComponent<Dropdown>();
        if (dd.captionText != null)
        {
            dd.captionText.font = GetFont();
            dd.captionText.fontSize = FontValue;
            dd.captionText.color = TextPrimary;
        }

        var tmpl = ddGO.transform.Find("Template");
        if (tmpl != null)
        {
            var tmplRT = tmpl.GetComponent<RectTransform>();
            tmplRT.sizeDelta = new Vector2(tmplRT.sizeDelta.x, 280);
            var tmplImg = tmpl.GetComponent<Image>();
            if (tmplImg) tmplImg.color = new Color(0.18f, 0.18f, 0.24f);

            var item = tmpl.Find("Viewport/Content/Item");
            if (item != null)
                item.GetComponent<RectTransform>().sizeDelta = new Vector2(item.GetComponent<RectTransform>().sizeDelta.x, 72);

            var itemBg = tmpl.Find("Viewport/Content/Item/Item Background");
            if (itemBg != null)
            {
                var ibImg = itemBg.GetComponent<Image>();
                if (ibImg) ibImg.color = new Color(0.16f, 0.16f, 0.22f);
            }

            var il = tmpl.Find("Viewport/Content/Item/Item Label");
            if (il != null)
            {
                var t = il.GetComponent<Text>();
                if (t) { t.font = GetFont(); t.fontSize = FontValue; t.color = TextPrimary; }
            }

            var scrollbar = tmpl.Find("Scrollbar");
            if (scrollbar != null)
            {
                var sbImg = scrollbar.GetComponent<Image>();
                if (sbImg) sbImg.color = new Color(0.12f, 0.12f, 0.18f);
            }
        }

        return ddGO;
    }
}
#endif
