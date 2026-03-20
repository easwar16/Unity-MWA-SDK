using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Solana.MWA;

/// <summary>
/// Example app demonstrating all MWA SDK methods and authorization cache.
/// Attach this to a Canvas with the required UI elements.
/// </summary>
public class MWAExampleController : MonoBehaviour
{
    [Header("UI References")]
    public Text StatusLabel;
    public Text PubkeyLabel;
    public Text OutputLog;
    public ScrollRect OutputScrollRect;
    public Dropdown ClusterDropdown;

    [Header("Buttons")]
    public Button ConnectBtn;
    public Button DisconnectBtn;
    public Button ReconnectBtn;
    public Button CapabilitiesBtn;
    public Button SignTxBtn;
    public Button SignSendBtn;
    public Button SignMsgBtn;
    public Button CloneAuthBtn;
    public Button ClearCacheBtn;

    private MobileWalletAdapter _adapter;

    private void Start()
    {
        _adapter = MobileWalletAdapter.Instance;

        // Configure dapp identity.
        _adapter.Identity = new DappIdentity("MWA SDK Demo", "https://solanamobile.com", "favicon.ico");

        // Connect events.
        _adapter.OnAuthorized += OnAuthorized;
        _adapter.OnAuthorizationFailed += OnAuthFailed;
        _adapter.OnDeauthorized += OnDeauthorized;
        _adapter.OnDeauthorizationFailed += OnDeauthFailed;
        _adapter.OnCapabilitiesReceived += OnCapabilities;
        _adapter.OnTransactionsSigned += OnTxSigned;
        _adapter.OnTransactionsSignFailed += OnTxSignFailed;
        _adapter.OnTransactionsSent += OnTxSent;
        _adapter.OnTransactionsSendFailed += OnTxSendFailed;
        _adapter.OnMessagesSigned += OnMsgSigned;
        _adapter.OnMessagesSignFailed += OnMsgSignFailed;
        _adapter.OnAuthorizationCloned += OnAuthCloned;
        _adapter.OnCloneFailed += OnCloneFailed;
        _adapter.OnStateChanged += OnStateChanged;

        // Cluster selector.
        ClusterDropdown.ClearOptions();
        ClusterDropdown.AddOptions(new System.Collections.Generic.List<string> { "Devnet", "Mainnet", "Testnet" });
        ClusterDropdown.value = 0;

        // Button connections.
        ConnectBtn.onClick.AddListener(OnConnect);
        DisconnectBtn.onClick.AddListener(OnDisconnect);
        ReconnectBtn.onClick.AddListener(OnReconnect);
        CapabilitiesBtn.onClick.AddListener(OnGetCapabilities);
        SignTxBtn.onClick.AddListener(OnSignTransaction);
        SignSendBtn.onClick.AddListener(OnSignAndSend);
        SignMsgBtn.onClick.AddListener(OnSignMessage);
        CloneAuthBtn.onClick.AddListener(OnCloneAuth);
        ClearCacheBtn.onClick.AddListener(OnClearCache);

        UpdateUI();
        Log("MWA SDK Demo ready. Select a cluster and connect.");

        // Show cache status.
        if (_adapter.AuthCache != null && _adapter.AuthCache.HasAuthorization())
            Log("Found cached authorization. Use 'Reconnect' to restore session.");
    }

    // --- Button handlers ---

    private void OnConnect()
    {
        _adapter.ActiveCluster = (Cluster)ClusterDropdown.value;
        Log($"Authorizing on {ClusterUtil.ClusterToChain(_adapter.ActiveCluster)}...");
        _adapter.Authorize();
    }

    private void OnDisconnect()
    {
        Log("Deauthorizing...");
        _adapter.Deauthorize();
    }

    private void OnReconnect()
    {
        _adapter.ActiveCluster = (Cluster)ClusterDropdown.value;
        Log("Reconnecting with cached auth...");
        _adapter.Reconnect();
    }

    private void OnGetCapabilities()
    {
        Log("Querying wallet capabilities...");
        _adapter.GetCapabilities();
    }

    private void OnSignTransaction()
    {
        // Create a dummy transaction (in real usage, build a proper Solana tx).
        byte[] dummyTx = new byte[64];
        new System.Random().NextBytes(dummyTx);
        Log("Signing 1 transaction...");
        _adapter.SignTransactions(new byte[][] { dummyTx });
    }

    private void OnSignAndSend()
    {
        byte[] dummyTx = new byte[64];
        new System.Random().NextBytes(dummyTx);
        var options = new SendOptions { Commitment = "confirmed" };
        Log("Sign & send 1 transaction...");
        _adapter.SignAndSendTransactions(new byte[][] { dummyTx }, options);
    }

    private void OnSignMessage()
    {
        byte[] msg = Encoding.UTF8.GetBytes("Hello from Unity MWA SDK!");
        Log("Signing message...");
        _adapter.SignMessages(new byte[][] { msg });
    }

    private void OnCloneAuth()
    {
        Log("Cloning authorization...");
        _adapter.CloneAuthorization();
    }

    private void OnClearCache()
    {
        if (_adapter.AuthCache != null)
            _adapter.AuthCache.Clear();
        Log("Authorization cache cleared.");
    }

    // --- Event handlers ---

    private void OnAuthorized(AuthorizationResult result)
    {
        string addr = result.Accounts != null && result.Accounts.Length > 0
            ? Shorten(result.Accounts[0].Address) : "unknown";
        Log($"<color=green>Authorized!</color> Account: {addr}");
        Log($"  Auth token: {result.AuthToken.Substring(0, Math.Min(16, result.AuthToken.Length))}...");
        Log($"  Accounts: {result.Accounts?.Length ?? 0}");
        Log("  Token cached for reconnection.");
        UpdateUI();
    }

    private void OnAuthFailed(MWAErrorCode errorCode, string errorMessage)
    {
        Log($"<color=red>Authorization failed</color> ({(int)errorCode}): {errorMessage}");
        UpdateUI();
    }

    private void OnDeauthorized()
    {
        Log("<color=yellow>Deauthorized.</color> Session ended, cache cleared.");
        UpdateUI();
    }

    private void OnDeauthFailed(string errorMessage)
    {
        Log($"<color=red>Deauthorization failed:</color> {errorMessage}");
        UpdateUI();
    }

    private void OnCapabilities(WalletCapabilities caps)
    {
        Log("<color=cyan>Wallet Capabilities:</color>");
        Log($"  Clone auth: {caps.SupportsCloneAuthorization}");
        Log($"  Sign & send: {caps.SupportsSignAndSend}");
        Log($"  Max tx/req: {caps.MaxTransactions}");
        Log($"  Max msg/req: {caps.MaxMessages}");
        Log($"  Tx versions: {string.Join(", ", caps.SupportedVersions ?? new string[0])}");
        Log($"  Features: {string.Join(", ", caps.Features ?? new string[0])}");
    }

    private void OnTxSigned(byte[][] signedPayloads)
    {
        Log($"<color=green>Transactions signed!</color> Count: {signedPayloads.Length}");
        for (int i = 0; i < signedPayloads.Length; i++)
            Log($"  [{i}] {signedPayloads[i].Length} bytes");
    }

    private void OnTxSignFailed(MWAErrorCode errorCode, string errorMessage)
    {
        Log($"<color=red>Sign failed</color> ({(int)errorCode}): {errorMessage}");
    }

    private void OnTxSent(string[] signatures)
    {
        Log($"<color=green>Transactions sent!</color> Signatures: {signatures.Length}");
        for (int i = 0; i < signatures.Length; i++)
        {
            string sig = signatures[i];
            string display = sig.Length > 32 ? sig.Substring(0, 32) + "..." : sig;
            Log($"  [{i}] {display}");
        }
    }

    private void OnTxSendFailed(MWAErrorCode errorCode, string errorMessage)
    {
        Log($"<color=red>Send failed</color> ({(int)errorCode}): {errorMessage}");
    }

    private void OnMsgSigned(byte[][] signatures)
    {
        Log($"<color=green>Messages signed!</color> Signatures: {signatures.Length}");
        for (int i = 0; i < signatures.Length; i++)
        {
            string b64 = Convert.ToBase64String(signatures[i]);
            string display = b64.Length > 32 ? b64.Substring(0, 32) + "..." : b64;
            Log($"  [{i}] {display}");
        }
    }

    private void OnMsgSignFailed(MWAErrorCode errorCode, string errorMessage)
    {
        Log($"<color=red>Message sign failed</color> ({(int)errorCode}): {errorMessage}");
    }

    private void OnAuthCloned(string authToken)
    {
        string display = authToken.Length > 16 ? authToken.Substring(0, 16) + "..." : authToken;
        Log($"<color=green>Authorization cloned!</color> Token: {display}");
    }

    private void OnCloneFailed(string errorMessage)
    {
        Log($"<color=red>Clone failed:</color> {errorMessage}");
    }

    private void OnStateChanged(ConnectionState newState)
    {
        UpdateUI();
    }

    // --- UI helpers ---

    private void UpdateUI()
    {
        bool connected = _adapter.IsConnected;
        bool hasAuth = _adapter.IsAuthorized;

        // Status.
        switch (_adapter.State)
        {
            case ConnectionState.Disconnected:
                StatusLabel.text = "Disconnected";
                StatusLabel.color = Color.red;
                break;
            case ConnectionState.Connecting:
                StatusLabel.text = "Connecting...";
                StatusLabel.color = Color.yellow;
                break;
            case ConnectionState.Connected:
                StatusLabel.text = "Connected";
                StatusLabel.color = Color.green;
                break;
            case ConnectionState.Signing:
                StatusLabel.text = "Signing...";
                StatusLabel.color = Color.cyan;
                break;
            case ConnectionState.Deauthorizing:
                StatusLabel.text = "Deauthorizing...";
                StatusLabel.color = Color.yellow;
                break;
        }

        // Pubkey.
        var acc = _adapter.GetAccount();
        PubkeyLabel.text = acc != null ? acc.Address : "Not connected";

        // Button states.
        ConnectBtn.interactable = _adapter.State != ConnectionState.Connecting;
        DisconnectBtn.interactable = hasAuth;
        ReconnectBtn.interactable = _adapter.State != ConnectionState.Connecting;
        CapabilitiesBtn.interactable = connected;
        SignTxBtn.interactable = connected;
        SignSendBtn.interactable = connected;
        SignMsgBtn.interactable = connected;
        CloneAuthBtn.interactable = connected;
    }

    private void Log(string msg)
    {
        string timeStr = DateTime.Now.ToString("HH:mm:ss");
        string line = $"[{timeStr}] {msg}\n";
        OutputLog.text += line;

        // Keep last ~30 lines to prevent overflow
        string[] lines = OutputLog.text.Split('\n');
        if (lines.Length > 30)
        {
            OutputLog.text = string.Join("\n", lines, lines.Length - 30, 30);
        }
    }

    private string Shorten(string addr)
    {
        if (string.IsNullOrEmpty(addr)) return "unknown";
        if (addr.Length > 12)
            return addr.Substring(0, 6) + "..." + addr.Substring(addr.Length - 4);
        return addr;
    }

    private void OnDestroy()
    {
        if (_adapter != null)
        {
            _adapter.OnAuthorized -= OnAuthorized;
            _adapter.OnAuthorizationFailed -= OnAuthFailed;
            _adapter.OnDeauthorized -= OnDeauthorized;
            _adapter.OnDeauthorizationFailed -= OnDeauthFailed;
            _adapter.OnCapabilitiesReceived -= OnCapabilities;
            _adapter.OnTransactionsSigned -= OnTxSigned;
            _adapter.OnTransactionsSignFailed -= OnTxSignFailed;
            _adapter.OnTransactionsSent -= OnTxSent;
            _adapter.OnTransactionsSendFailed -= OnTxSendFailed;
            _adapter.OnMessagesSigned -= OnMsgSigned;
            _adapter.OnMessagesSignFailed -= OnMsgSignFailed;
            _adapter.OnAuthorizationCloned -= OnAuthCloned;
            _adapter.OnCloneFailed -= OnCloneFailed;
            _adapter.OnStateChanged -= OnStateChanged;
        }
    }
}
