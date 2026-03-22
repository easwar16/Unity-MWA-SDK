using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Solana.MWA;

public class MWAExampleController : MonoBehaviour
{
    // --- Session Card ---
    [Header("Session Card")]
    public Text ConnectionValue;
    public Text AuthValue;
    public Text WalletValue;
    public Text TokenValue;
    public Dropdown ClusterDropdown;
    public Button ConnectBtn;
    public Button AuthorizeBtn;
    public Button DeauthorizeBtn;
    public Button ReconnectBtn;

    // --- Wallet Methods Card ---
    [Header("Wallet Methods")]
    public Button SignTxBtn;
    public Button SignMsgBtn;
    public Button SignSendBtn;
    public Button CapabilitiesBtn;
    public Button CloneAuthBtn;

    // --- Auth Cache Card ---
    [Header("Authorization Cache")]
    public Text CacheStatusValue;
    public Text LastSessionValue;
    public Button ClearCacheBtn;
    public Button ReuseSessionBtn;

    // --- Protocol Log ---
    [Header("Protocol Activity Log")]
    public Text OutputLog;
    public ScrollRect LogScrollRect;

    // =====================================================================
    // CENTRAL STATE
    // =====================================================================

    class MwaState
    {
        public bool IsConnected;
        public bool IsAuthorized;
        public string PublicKey;
        public string AuthToken;
        public string WalletLabel;
        public ConnectionState ConnectionState;
        public bool HasCachedToken;
        public string LastSessionTime;
    }

    MwaState _state = new MwaState();
    MobileWalletAdapter _adapter;

    // =====================================================================
    // LIFECYCLE
    // =====================================================================

    void Start()
    {
        _adapter = MobileWalletAdapter.Instance;
        _adapter.Identity = new DappIdentity("MWA SDK Demo", "https://solanamobile.com", "favicon.ico");

        BindEvents();
        SetupClusterDropdown();
        BindButtons();

        // Initialize state from adapter
        SyncState();
        RefreshCacheState();
        UpdateAllUI();

        LogProtocol("SESSION", "MWA SDK Demo initialized");
        LogProtocol("SESSION", $"Cluster: {ClusterUtil.ClusterToChain(_adapter.ActiveCluster)}");

        if (_state.HasCachedToken)
            LogProtocol("CACHE", "Cached authorization found — use Reconnect to restore session");
    }

    void OnDestroy()
    {
        if (_adapter == null) return;
        _adapter.OnAuthorized -= HandleAuthorized;
        _adapter.OnAuthorizationFailed -= HandleAuthFailed;
        _adapter.OnDeauthorized -= HandleDeauthorized;
        _adapter.OnDeauthorizationFailed -= HandleDeauthFailed;
        _adapter.OnCapabilitiesReceived -= HandleCapabilities;
        _adapter.OnTransactionsSigned -= HandleTxSigned;
        _adapter.OnTransactionsSignFailed -= HandleTxSignFailed;
        _adapter.OnTransactionsSent -= HandleTxSent;
        _adapter.OnTransactionsSendFailed -= HandleTxSendFailed;
        _adapter.OnMessagesSigned -= HandleMsgSigned;
        _adapter.OnMessagesSignFailed -= HandleMsgSignFailed;
        _adapter.OnAuthorizationCloned -= HandleAuthCloned;
        _adapter.OnCloneFailed -= HandleCloneFailed;
        _adapter.OnStateChanged -= HandleStateChanged;
    }

    // =====================================================================
    // SETUP
    // =====================================================================

    void BindEvents()
    {
        _adapter.OnAuthorized += HandleAuthorized;
        _adapter.OnAuthorizationFailed += HandleAuthFailed;
        _adapter.OnDeauthorized += HandleDeauthorized;
        _adapter.OnDeauthorizationFailed += HandleDeauthFailed;
        _adapter.OnCapabilitiesReceived += HandleCapabilities;
        _adapter.OnTransactionsSigned += HandleTxSigned;
        _adapter.OnTransactionsSignFailed += HandleTxSignFailed;
        _adapter.OnTransactionsSent += HandleTxSent;
        _adapter.OnTransactionsSendFailed += HandleTxSendFailed;
        _adapter.OnMessagesSigned += HandleMsgSigned;
        _adapter.OnMessagesSignFailed += HandleMsgSignFailed;
        _adapter.OnAuthorizationCloned += HandleAuthCloned;
        _adapter.OnCloneFailed += HandleCloneFailed;
        _adapter.OnStateChanged += HandleStateChanged;
    }

    void SetupClusterDropdown()
    {
        ClusterDropdown.ClearOptions();
        ClusterDropdown.AddOptions(new System.Collections.Generic.List<string>
            { "devnet", "mainnet-beta", "testnet" });
        ClusterDropdown.value = 0;
        ClusterDropdown.onValueChanged.AddListener(idx =>
        {
            _adapter.ActiveCluster = (Cluster)idx;
            LogProtocol("SESSION", $"Cluster changed: {ClusterUtil.ClusterToChain(_adapter.ActiveCluster)}");
        });
    }

    void BindButtons()
    {
        // Session
        ConnectBtn.onClick.AddListener(OnConnect);
        AuthorizeBtn.onClick.AddListener(OnAuthorize);
        DeauthorizeBtn.onClick.AddListener(OnDeauthorize);
        ReconnectBtn.onClick.AddListener(OnReconnect);

        // Wallet methods
        SignTxBtn.onClick.AddListener(OnSignTransactions);
        SignMsgBtn.onClick.AddListener(OnSignMessages);
        SignSendBtn.onClick.AddListener(OnSignAndSendTransactions);
        CapabilitiesBtn.onClick.AddListener(OnGetCapabilities);
        CloneAuthBtn.onClick.AddListener(OnCloneAuthorization);

        // Cache
        ClearCacheBtn.onClick.AddListener(OnClearCache);
        ReuseSessionBtn.onClick.AddListener(OnReuseSession);
    }

    // =====================================================================
    // STATE MANAGEMENT
    // =====================================================================

    void SyncState()
    {
        _state.ConnectionState = _adapter.State;
        _state.IsConnected = _adapter.IsConnected;
        _state.IsAuthorized = _adapter.IsAuthorized;

        var account = _adapter.GetAccount();
        _state.PublicKey = account?.Address;
        _state.WalletLabel = account?.Label;

        var auth = _adapter.CurrentAuth;
        _state.AuthToken = auth?.AuthToken;
    }

    void RefreshCacheState()
    {
        _state.HasCachedToken = _adapter.AuthCache != null && _adapter.AuthCache.HasAuthorization();
    }

    // =====================================================================
    // BUTTON HANDLERS — mapped to MWA SDK methods
    // =====================================================================

    // Connect → transact() — starts a wallet session
    void OnConnect()
    {
        _adapter.ActiveCluster = (Cluster)ClusterDropdown.value;
        LogProtocol("SESSION", $"Starting session on {ClusterUtil.ClusterToChain(_adapter.ActiveCluster)}...");
        LogProtocol("SESSION", "Calling transact() → authorize()");
        _adapter.Authorize();
    }

    // Authorize → authorizeSession() — explicit authorization
    void OnAuthorize()
    {
        _adapter.ActiveCluster = (Cluster)ClusterDropdown.value;
        LogProtocol("AUTH", $"Requesting authorization on {ClusterUtil.ClusterToChain(_adapter.ActiveCluster)}...");
        _adapter.Authorize();
    }

    // Deauthorize → wallet.deauthorize()
    void OnDeauthorize()
    {
        LogProtocol("AUTH", "Deauthorizing session...");
        _adapter.Deauthorize();
    }

    // Reconnect — reauthorize with cached token
    void OnReconnect()
    {
        _adapter.ActiveCluster = (Cluster)ClusterDropdown.value;
        LogProtocol("SESSION", "Reconnecting with cached authorization...");
        _adapter.Reconnect();
    }

    // sign_transactions
    void OnSignTransactions()
    {
        byte[] dummyTx = new byte[64];
        new System.Random().NextBytes(dummyTx);
        LogProtocol("SIGN", "Calling sign_transactions (1 payload, 64 bytes)");
        _adapter.SignTransactions(new byte[][] { dummyTx });
    }

    // sign_messages
    void OnSignMessages()
    {
        byte[] msg = Encoding.UTF8.GetBytes("Hello from Unity MWA SDK!");
        LogProtocol("SIGN", "Calling sign_messages (\"Hello from Unity MWA SDK!\")");
        _adapter.SignMessages(new byte[][] { msg });
    }

    // sign_and_send_transactions
    void OnSignAndSendTransactions()
    {
        byte[] dummyTx = new byte[64];
        new System.Random().NextBytes(dummyTx);
        var options = new SendOptions { Commitment = "confirmed" };
        LogProtocol("SIGN", "Calling sign_and_send_transactions (commitment=confirmed)");
        _adapter.SignAndSendTransactions(new byte[][] { dummyTx }, options);
    }

    // get_capabilities
    void OnGetCapabilities()
    {
        LogProtocol("METHOD", "Calling get_capabilities...");
        _adapter.GetCapabilities();
    }

    // clone_authorization
    void OnCloneAuthorization()
    {
        LogProtocol("METHOD", "Calling clone_authorization...");
        _adapter.CloneAuthorization();
    }

    // Cache: clear
    void OnClearCache()
    {
        if (_adapter.AuthCache != null)
            _adapter.AuthCache.Clear();
        _state.HasCachedToken = false;
        _state.LastSessionTime = null;
        LogProtocol("CACHE", "Authorization cache cleared");
        UpdateAllUI();
    }

    // Cache: reuse session (same as reconnect)
    void OnReuseSession()
    {
        if (!_state.HasCachedToken)
        {
            LogProtocol("CACHE", "No cached token available");
            return;
        }
        _adapter.ActiveCluster = (Cluster)ClusterDropdown.value;
        LogProtocol("CACHE", "Reusing cached session token...");
        _adapter.Reconnect();
    }

    // =====================================================================
    // EVENT HANDLERS
    // =====================================================================

    void HandleAuthorized(AuthorizationResult result)
    {
        SyncState();
        _state.LastSessionTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        RefreshCacheState();

        string addr = result.Accounts != null && result.Accounts.Length > 0
            ? result.Accounts[0].Address : "unknown";
        string label = result.Accounts != null && result.Accounts.Length > 0
            ? result.Accounts[0].Label : null;

        LogProtocol("AUTH", $"Authorization SUCCESS");
        LogProtocol("AUTH", $"  Account: {Shorten(addr)}");
        if (!string.IsNullOrEmpty(label))
            LogProtocol("AUTH", $"  Label: {label}");
        LogProtocol("AUTH", $"  Token: {Shorten(result.AuthToken, 16)}");
        LogProtocol("AUTH", $"  Accounts: {result.Accounts?.Length ?? 0}");
        LogProtocol("CACHE", "Auth token cached for reconnection");

        UpdateAllUI();
    }

    void HandleAuthFailed(MWAErrorCode code, string message)
    {
        SyncState();
        LogProtocol("ERROR", $"Authorization FAILED ({(int)code}): {message}");
        UpdateAllUI();
    }

    void HandleDeauthorized()
    {
        SyncState();
        RefreshCacheState();
        LogProtocol("AUTH", "Deauthorized — session ended");
        LogProtocol("CACHE", "Auth token invalidated");
        UpdateAllUI();
    }

    void HandleDeauthFailed(string message)
    {
        SyncState();
        LogProtocol("ERROR", $"Deauthorization FAILED: {message}");
        UpdateAllUI();
    }

    void HandleCapabilities(WalletCapabilities caps)
    {
        LogProtocol("METHOD", "get_capabilities → SUCCESS");
        LogProtocol("METHOD", $"  clone_authorization: {caps.SupportsCloneAuthorization}");
        LogProtocol("METHOD", $"  sign_and_send: {caps.SupportsSignAndSend}");
        LogProtocol("METHOD", $"  max_transactions: {caps.MaxTransactions}");
        LogProtocol("METHOD", $"  max_messages: {caps.MaxMessages}");
        if (caps.SupportedVersions != null && caps.SupportedVersions.Length > 0)
            LogProtocol("METHOD", $"  versions: [{string.Join(", ", caps.SupportedVersions)}]");
        if (caps.Features != null && caps.Features.Length > 0)
            LogProtocol("METHOD", $"  features: [{string.Join(", ", caps.Features)}]");
    }

    void HandleTxSigned(byte[][] signedPayloads)
    {
        SyncState();
        LogProtocol("SIGN", $"sign_transactions → SUCCESS ({signedPayloads.Length} signed)");
        for (int i = 0; i < signedPayloads.Length; i++)
            LogProtocol("SIGN", $"  [{i}] {signedPayloads[i].Length} bytes");
        UpdateAllUI();
    }

    void HandleTxSignFailed(MWAErrorCode code, string message)
    {
        SyncState();
        LogProtocol("ERROR", $"sign_transactions → FAILED ({(int)code}): {message}");
        UpdateAllUI();
    }

    void HandleTxSent(string[] signatures)
    {
        SyncState();
        LogProtocol("SIGN", $"sign_and_send_transactions → SUCCESS ({signatures.Length} sent)");
        for (int i = 0; i < signatures.Length; i++)
            LogProtocol("SIGN", $"  [{i}] {Shorten(signatures[i], 32)}");
        UpdateAllUI();
    }

    void HandleTxSendFailed(MWAErrorCode code, string message)
    {
        SyncState();
        LogProtocol("ERROR", $"sign_and_send_transactions → FAILED ({(int)code}): {message}");
        UpdateAllUI();
    }

    void HandleMsgSigned(byte[][] signatures)
    {
        SyncState();
        LogProtocol("SIGN", $"sign_messages → SUCCESS ({signatures.Length} signed)");
        for (int i = 0; i < signatures.Length; i++)
        {
            string b64 = Convert.ToBase64String(signatures[i]);
            LogProtocol("SIGN", $"  [{i}] {Shorten(b64, 32)}");
        }
        UpdateAllUI();
    }

    void HandleMsgSignFailed(MWAErrorCode code, string message)
    {
        SyncState();
        LogProtocol("ERROR", $"sign_messages → FAILED ({(int)code}): {message}");
        UpdateAllUI();
    }

    void HandleAuthCloned(string authToken)
    {
        LogProtocol("METHOD", $"clone_authorization → SUCCESS");
        LogProtocol("METHOD", $"  Token: {Shorten(authToken, 16)}");
    }

    void HandleCloneFailed(string message)
    {
        LogProtocol("ERROR", $"clone_authorization → FAILED: {message}");
    }

    void HandleStateChanged(ConnectionState newState)
    {
        var prevState = _state.ConnectionState;
        SyncState();

        if (prevState != newState)
            LogProtocol("SESSION", $"State: {prevState} → {newState}");

        UpdateAllUI();
    }

    // =====================================================================
    // UI UPDATE — all driven from _state
    // =====================================================================

    void UpdateAllUI()
    {
        UpdateSessionCard();
        UpdateWalletMethodsCard();
        UpdateCacheCard();
    }

    void UpdateSessionCard()
    {
        // Connection status
        switch (_state.ConnectionState)
        {
            case ConnectionState.Disconnected:
                ConnectionValue.text = "Disconnected";
                ConnectionValue.color = new Color(0.8f, 0.25f, 0.25f);
                break;
            case ConnectionState.Connecting:
                ConnectionValue.text = "Connecting...";
                ConnectionValue.color = new Color(0.9f, 0.75f, 0.2f);
                break;
            case ConnectionState.Connected:
                ConnectionValue.text = "Connected";
                ConnectionValue.color = new Color(0.2f, 0.8f, 0.4f);
                break;
            case ConnectionState.Signing:
                ConnectionValue.text = "Signing...";
                ConnectionValue.color = new Color(0.3f, 0.75f, 0.85f);
                break;
            case ConnectionState.Deauthorizing:
                ConnectionValue.text = "Deauthorizing...";
                ConnectionValue.color = new Color(0.9f, 0.75f, 0.2f);
                break;
        }

        // Auth status
        if (_state.IsAuthorized)
        {
            AuthValue.text = "Authorized";
            AuthValue.color = new Color(0.2f, 0.8f, 0.4f);
        }
        else
        {
            AuthValue.text = "Not Authorized";
            AuthValue.color = new Color(0.45f, 0.45f, 0.5f);
        }

        // Wallet
        if (!string.IsNullOrEmpty(_state.PublicKey))
        {
            WalletValue.text = Shorten(_state.PublicKey);
            WalletValue.color = Color.white;
        }
        else
        {
            WalletValue.text = "---";
            WalletValue.color = new Color(0.45f, 0.45f, 0.5f);
        }

        // Auth token
        if (!string.IsNullOrEmpty(_state.AuthToken))
        {
            TokenValue.text = Shorten(_state.AuthToken, 12);
            TokenValue.color = new Color(0.2f, 0.8f, 0.4f);
        }
        else
        {
            TokenValue.text = "None";
            TokenValue.color = new Color(0.45f, 0.45f, 0.5f);
        }

        // Button states — follow protocol flow
        bool busy = _state.ConnectionState == ConnectionState.Connecting
                  || _state.ConnectionState == ConnectionState.Deauthorizing;

        ConnectBtn.interactable = !busy && !_state.IsConnected;
        AuthorizeBtn.interactable = !busy;
        DeauthorizeBtn.interactable = _state.IsAuthorized && !busy;
        ReconnectBtn.interactable = !busy;
    }

    void UpdateWalletMethodsCard()
    {
        bool canSign = _state.IsConnected;
        SignTxBtn.interactable = canSign;
        SignMsgBtn.interactable = canSign;
        SignSendBtn.interactable = canSign;
        CapabilitiesBtn.interactable = canSign;
        CloneAuthBtn.interactable = canSign;
    }

    void UpdateCacheCard()
    {
        RefreshCacheState();

        if (_state.HasCachedToken)
        {
            CacheStatusValue.text = "Cached";
            CacheStatusValue.color = new Color(0.2f, 0.8f, 0.4f);
        }
        else
        {
            CacheStatusValue.text = "Empty";
            CacheStatusValue.color = new Color(0.45f, 0.45f, 0.5f);
        }

        if (!string.IsNullOrEmpty(_state.LastSessionTime))
        {
            LastSessionValue.text = _state.LastSessionTime;
            LastSessionValue.color = Color.white;
        }
        else
        {
            LastSessionValue.text = "---";
            LastSessionValue.color = new Color(0.45f, 0.45f, 0.5f);
        }

        ClearCacheBtn.interactable = _state.HasCachedToken;
        ReuseSessionBtn.interactable = _state.HasCachedToken;
    }

    // =====================================================================
    // STRUCTURED PROTOCOL LOG
    // =====================================================================

    void LogProtocol(string category, string message)
    {
        string time = DateTime.Now.ToString("HH:mm:ss");
        string color;
        switch (category)
        {
            case "SESSION": color = "#CCCCCC"; break;
            case "AUTH":    color = "#4CAF50"; break;
            case "SIGN":   color = "#FF9800"; break;
            case "METHOD":  color = "#2196F3"; break;
            case "CACHE":   color = "#9C27B0"; break;
            case "ERROR":   color = "#F44336"; break;
            default:        color = "#888888"; break;
        }

        string line = $"<color=#666>{time}</color> <color={color}>[{category}]</color> {message}\n";
        OutputLog.text += line;

        // Trim to last 50 lines
        string[] lines = OutputLog.text.Split('\n');
        if (lines.Length > 50)
            OutputLog.text = string.Join("\n", lines, lines.Length - 50, 50);

        // Auto-scroll to bottom
        if (LogScrollRect != null)
            Canvas.ForceUpdateCanvases();
    }

    // =====================================================================
    // UTILITIES
    // =====================================================================

    string Shorten(string s, int maxLen = 12)
    {
        if (string.IsNullOrEmpty(s)) return "---";
        if (s.Length <= maxLen) return s;
        int half = (maxLen - 3) / 2;
        return s.Substring(0, half) + "..." + s.Substring(s.Length - half);
    }
}
