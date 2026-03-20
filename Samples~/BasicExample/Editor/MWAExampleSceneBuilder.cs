#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public class MWAExampleSceneBuilder
{
    [MenuItem("Solana/MWA/Create Example Scene")]
    public static void CreateExampleScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

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

        // Background
        var bg = CreatePanel(canvasGO.transform, "Background", new Color(0.12f, 0.12f, 0.15f, 1f));
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Header
        var header = CreatePanel(bg.transform, "Header", new Color(0.16f, 0.16f, 0.2f, 1f));
        var headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 0.86f);
        headerRect.anchorMax = new Vector2(1, 0.97f);
        headerRect.offsetMin = new Vector2(20, 0);
        headerRect.offsetMax = new Vector2(-20, 0);

        var title = CreateText(header.transform, "Title", "Solana MWA SDK Demo", 36, TextAnchor.MiddleCenter, Color.white);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.55f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = new Vector2(10, 0);
        titleRect.offsetMax = new Vector2(-10, -5);

        var statusLabel = CreateText(header.transform, "StatusLabel", "Disconnected", 28, TextAnchor.MiddleCenter, Color.red);
        var statusRect = statusLabel.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.25f);
        statusRect.anchorMax = new Vector2(0.5f, 0.55f);
        statusRect.offsetMin = new Vector2(10, 0);
        statusRect.offsetMax = new Vector2(-5, 0);

        // Cluster Dropdown
        var clusterDropdown = CreateDropdown(header.transform, "ClusterDropdown");
        var clusterRect = clusterDropdown.GetComponent<RectTransform>();
        clusterRect.anchorMin = new Vector2(0.5f, 0.25f);
        clusterRect.anchorMax = new Vector2(1f, 0.55f);
        clusterRect.offsetMin = new Vector2(5, 0);
        clusterRect.offsetMax = new Vector2(-10, 0);

        var pubkeyLabel = CreateText(header.transform, "PubkeyLabel", "Not connected", 20, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
        var pubkeyRect = pubkeyLabel.GetComponent<RectTransform>();
        pubkeyRect.anchorMin = new Vector2(0, 0);
        pubkeyRect.anchorMax = new Vector2(1, 0.25f);
        pubkeyRect.offsetMin = new Vector2(10, 5);
        pubkeyRect.offsetMax = new Vector2(-10, 0);

        // Button Grid — tight below header
        var buttonArea = CreatePanel(bg.transform, "ButtonArea", Color.clear);
        var buttonAreaRect = buttonArea.GetComponent<RectTransform>();
        buttonAreaRect.anchorMin = new Vector2(0, 0.62f);
        buttonAreaRect.anchorMax = new Vector2(1, 0.855f);
        buttonAreaRect.offsetMin = new Vector2(24, 4);
        buttonAreaRect.offsetMax = new Vector2(-24, -4);

        var gridLayout = buttonArea.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(322, 78);
        gridLayout.spacing = new Vector2(10, 8);
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;
        gridLayout.padding = new RectOffset(0, 0, 0, 0);

        var connectBtn = CreateButton(buttonArea.transform, "ConnectBtn", "Connect", new Color(0.2f, 0.7f, 0.3f));
        var disconnectBtn = CreateButton(buttonArea.transform, "DisconnectBtn", "Disconnect", new Color(0.5f, 0.2f, 0.2f));
        var reconnectBtn = CreateButton(buttonArea.transform, "ReconnectBtn", "Reconnect", new Color(0.2f, 0.4f, 0.8f));
        var capabilitiesBtn = CreateButton(buttonArea.transform, "CapabilitiesBtn", "Capabilities", new Color(0.4f, 0.3f, 0.6f));
        var signTxBtn = CreateButton(buttonArea.transform, "SignTxBtn", "Sign Tx", new Color(0.6f, 0.5f, 0.2f));
        var signSendBtn = CreateButton(buttonArea.transform, "SignSendBtn", "Sign & Send", new Color(0.7f, 0.4f, 0.2f));
        var signMsgBtn = CreateButton(buttonArea.transform, "SignMsgBtn", "Sign Message", new Color(0.2f, 0.6f, 0.6f));
        var cloneAuthBtn = CreateButton(buttonArea.transform, "CloneAuthBtn", "Clone Auth", new Color(0.5f, 0.5f, 0.5f));
        var clearCacheBtn = CreateButton(buttonArea.transform, "ClearCacheBtn", "Clear Cache", new Color(0.6f, 0.2f, 0.2f));

        // Log Area — fill remaining space below buttons
        var logPanel = CreatePanel(bg.transform, "LogPanel", new Color(0.08f, 0.08f, 0.1f, 1f));
        var logPanelRect = logPanel.GetComponent<RectTransform>();
        logPanelRect.anchorMin = new Vector2(0, 0.01f);
        logPanelRect.anchorMax = new Vector2(1, 0.61f);
        logPanelRect.offsetMin = new Vector2(24, 10);
        logPanelRect.offsetMax = new Vector2(-24, -6);

        var logTitle = CreateText(logPanel.transform, "LogTitle", "Output Log", 24, TextAnchor.MiddleLeft, new Color(0.6f, 0.6f, 0.6f));
        var logTitleRect = logTitle.GetComponent<RectTransform>();
        logTitleRect.anchorMin = new Vector2(0, 0.92f);
        logTitleRect.anchorMax = new Vector2(1, 1);
        logTitleRect.offsetMin = new Vector2(15, 0);
        logTitleRect.offsetMax = new Vector2(-15, -5);

        // Simple text log — no scroll view, just text filling the panel
        var outputLog = CreateText(logPanel.transform, "OutputLog", "", 18, TextAnchor.UpperLeft, new Color(0.75f, 0.9f, 0.75f));
        var outputLogRect = outputLog.GetComponent<RectTransform>();
        outputLogRect.anchorMin = Vector2.zero;
        outputLogRect.anchorMax = new Vector2(1, 0.92f);
        outputLogRect.offsetMin = new Vector2(15, 10);
        outputLogRect.offsetMax = new Vector2(-15, -5);
        var outputText = outputLog.GetComponent<Text>();
        outputText.supportRichText = true;
        outputText.horizontalOverflow = HorizontalWrapMode.Wrap;
        outputText.verticalOverflow = VerticalWrapMode.Truncate;

        // Wire controller
        var controller = canvasGO.AddComponent<MWAExampleController>();
        controller.StatusLabel = statusLabel.GetComponent<Text>();
        controller.PubkeyLabel = pubkeyLabel.GetComponent<Text>();
        controller.OutputLog = outputText;
        controller.OutputScrollRect = null;
        controller.ClusterDropdown = clusterDropdown.GetComponent<Dropdown>();
        controller.ConnectBtn = connectBtn.GetComponent<Button>();
        controller.DisconnectBtn = disconnectBtn.GetComponent<Button>();
        controller.ReconnectBtn = reconnectBtn.GetComponent<Button>();
        controller.CapabilitiesBtn = capabilitiesBtn.GetComponent<Button>();
        controller.SignTxBtn = signTxBtn.GetComponent<Button>();
        controller.SignSendBtn = signSendBtn.GetComponent<Button>();
        controller.SignMsgBtn = signMsgBtn.GetComponent<Button>();
        controller.CloneAuthBtn = cloneAuthBtn.GetComponent<Button>();
        controller.ClearCacheBtn = clearCacheBtn.GetComponent<Button>();

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MWAExample.unity");
        EditorSceneManager.OpenScene("Assets/Scenes/MWAExample.unity");
        Debug.Log("MWA Example Scene created successfully.");
    }

    static Font GetFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null) return f;
        f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }

    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    static GameObject CreateText(Transform parent, string name, string text, int fontSize, TextAnchor alignment, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var txt = go.AddComponent<Text>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.alignment = alignment;
        txt.color = color;
        txt.font = GetFont();
        txt.raycastTarget = false;
        return go;
    }

    static GameObject CreateButton(Transform parent, string name, string label, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        go.AddComponent<Button>();

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textGO.AddComponent<Text>();
        text.text = label;
        text.fontSize = 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = GetFont();
        text.fontStyle = FontStyle.Bold;
        text.raycastTarget = false;

        return go;
    }

    static GameObject CreateDropdown(Transform parent, string name)
    {
        var ddGO = DefaultControls.CreateDropdown(new DefaultControls.Resources());
        ddGO.name = name;
        ddGO.transform.SetParent(parent, false);
        ddGO.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f);

        var dd = ddGO.GetComponent<Dropdown>();
        if (dd.captionText != null)
        {
            dd.captionText.font = GetFont();
            dd.captionText.fontSize = 24;
            dd.captionText.color = Color.white;
        }

        // Style template — bigger items
        var tmpl = ddGO.transform.Find("Template");
        if (tmpl != null)
        {
            var tmplRT = tmpl.GetComponent<RectTransform>();
            tmplRT.sizeDelta = new Vector2(tmplRT.sizeDelta.x, 250);

            var tmplImg = tmpl.GetComponent<Image>();
            if (tmplImg) tmplImg.color = new Color(0.2f, 0.2f, 0.25f);

            var item = tmpl.Find("Viewport/Content/Item");
            if (item != null)
            {
                var itemRT = item.GetComponent<RectTransform>();
                itemRT.sizeDelta = new Vector2(itemRT.sizeDelta.x, 70);
            }

            // Dark item backgrounds
            var itemBg = tmpl.Find("Viewport/Content/Item/Item Background");
            if (itemBg != null)
            {
                var ibImg = itemBg.GetComponent<Image>();
                if (ibImg) ibImg.color = new Color(0.18f, 0.18f, 0.24f);
            }

            var itemCheck = tmpl.Find("Viewport/Content/Item/Item Checkmark");
            if (itemCheck != null)
            {
                var icImg = itemCheck.GetComponent<Image>();
                if (icImg) icImg.color = Color.white;
            }

            var il = tmpl.Find("Viewport/Content/Item/Item Label");
            if (il != null)
            {
                var t = il.GetComponent<Text>();
                if (t) { t.font = GetFont(); t.fontSize = 28; t.color = Color.white; }
            }

            // Also darken the scrollbar
            var scrollbar = tmpl.Find("Scrollbar");
            if (scrollbar != null)
            {
                var sbImg = scrollbar.GetComponent<Image>();
                if (sbImg) sbImg.color = new Color(0.15f, 0.15f, 0.2f);
            }
        }

        return ddGO;
    }
}
#endif
