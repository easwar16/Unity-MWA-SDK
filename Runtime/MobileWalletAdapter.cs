using System;
using System.Collections;
using UnityEngine;

namespace Solana.MWA
{
    /// <summary>
    /// Main Mobile Wallet Adapter class.
    /// Provides full MWA 2.0 API parity with the React Native SDK.
    /// Attach to a GameObject or use MobileWalletAdapter.Instance for singleton access.
    /// </summary>
    public class MobileWalletAdapter : MonoBehaviour
    {
        // --- Singleton ---
        private static MobileWalletAdapter _instance;
        public static MobileWalletAdapter Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("MobileWalletAdapter");
                    _instance = go.AddComponent<MobileWalletAdapter>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // --- Configuration ---

        /// <summary>
        /// Dapp identity presented to wallet during authorization.
        /// </summary>
        public DappIdentity Identity = new DappIdentity();

        /// <summary>
        /// Blockchain cluster to connect to.
        /// </summary>
        public Cluster ActiveCluster = Cluster.Devnet;

        /// <summary>
        /// Authorization cache implementation. Defaults to FileMWACache.
        /// Set to a custom IMWACache implementation for different storage backends.
        /// </summary>
        public IMWACache AuthCache { get; set; }

        // --- Session State ---

        private MWASession _session = new MWASession();

        /// <summary>Current connection state.</summary>
        public ConnectionState State => _session.State;

        /// <summary>Current authorization result.</summary>
        public AuthorizationResult CurrentAuth => _session.CurrentAuth;

        /// <summary>Last queried wallet capabilities.</summary>
        public WalletCapabilities Capabilities
        {
            get => _session.Capabilities;
            private set => _session.Capabilities = value;
        }

        /// <summary>Whether we have a valid authorization.</summary>
        public bool IsAuthorized => _session.IsAuthorized;

        /// <summary>Whether we are connected (authorized with active session).</summary>
        public bool IsConnected => _session.IsConnected;

        // --- Events ---

        public event Action<AuthorizationResult> OnAuthorized;
        public event Action<MWAErrorCode, string> OnAuthorizationFailed;
        public event Action OnDeauthorized;
        public event Action<string> OnDeauthorizationFailed;
        public event Action<WalletCapabilities> OnCapabilitiesReceived;
        public event Action<byte[][]> OnTransactionsSigned;
        public event Action<MWAErrorCode, string> OnTransactionsSignFailed;
        public event Action<string[]> OnTransactionsSent;
        public event Action<MWAErrorCode, string> OnTransactionsSendFailed;
        public event Action<byte[][]> OnMessagesSigned;
        public event Action<MWAErrorCode, string> OnMessagesSignFailed;
        public event Action<string> OnAuthorizationCloned;
        public event Action<string> OnCloneFailed;
        public event Action<ConnectionState> OnStateChanged;

        // --- Internal ---

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaClass _bridge;
#endif
        private string _pollAction = "";
        private const float PollInterval = 0.05f; // 50ms

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            AuthCache = new FileMWACache();

            _session.OnStateChanged += (state) => OnStateChanged?.Invoke(state);

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                _bridge = new AndroidJavaClass("com.solana.mwa.unity.MWABridge");
                _bridge.CallStatic("initialize");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"MobileWalletAdapter: Failed to load Android bridge: {e.Message}");
            }
#endif

            // Try to restore cached authorization.
            if (AuthCache != null)
            {
                var cached = AuthCache.GetAuthorization();
                if (cached != null && !string.IsNullOrEmpty(cached.AuthToken))
                    _session.SetAuth(cached);
            }
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(_pollAction)) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_bridge == null) return;
            PollAndroidStatus();
#endif
        }

        // ===================================================================
        // PUBLIC API — Full MWA 2.0 parity with React Native SDK
        // ===================================================================

        /// <summary>
        /// Authorize this dapp with a wallet. If a cached auth_token exists, attempts
        /// reauthorization first. Equivalent to React Native's transact -> authorize().
        /// </summary>
        public void Authorize(SignInPayload signInPayload = null)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_bridge == null)
            {
                OnAuthorizationFailed?.Invoke(MWAErrorCode.AuthorizationFailed, "Android bridge not available");
                return;
            }

            _session.SetState(ConnectionState.Connecting);

            string cachedToken = "";
            if (CurrentAuth != null && !string.IsNullOrEmpty(CurrentAuth.AuthToken))
                cachedToken = CurrentAuth.AuthToken;

            string signInJson = "";
            if (signInPayload != null)
                signInJson = signInPayload.ToJson();

            _bridge.CallStatic("authorize",
                Identity.Uri,
                Identity.Icon,
                Identity.Name,
                ClusterUtil.ClusterToChain(ActiveCluster),
                cachedToken,
                signInJson
            );
            _pollAction = "authorize";
#else
            Debug.LogWarning("MobileWalletAdapter: Only available on Android devices.");
            OnAuthorizationFailed?.Invoke(MWAErrorCode.AuthorizationFailed, "Only available on Android");
#endif
        }

        /// <summary>
        /// Deauthorize and disconnect from the wallet. Invalidates the auth token.
        /// </summary>
        public void Deauthorize()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_bridge == null)
            {
                OnDeauthorizationFailed?.Invoke("Android bridge not available");
                return;
            }

            if (CurrentAuth == null || string.IsNullOrEmpty(CurrentAuth.AuthToken))
            {
                ClearAuth();
                OnDeauthorized?.Invoke();
                return;
            }

            _session.SetState(ConnectionState.Deauthorizing);
            _bridge.CallStatic("deauthorize",
                Identity.Uri,
                Identity.Icon,
                Identity.Name,
                ClusterUtil.ClusterToChain(ActiveCluster),
                CurrentAuth.AuthToken
            );
            _pollAction = "deauthorize";
#else
            ClearAuth();
            OnDeauthorized?.Invoke();
#endif
        }

        /// <summary>
        /// Disconnect from wallet. Alias for Deauthorize that also clears local state.
        /// </summary>
        public void Disconnect()
        {
            Deauthorize();
        }

        /// <summary>
        /// Reconnect to wallet using cached authorization. If cache is empty, performs full authorize.
        /// </summary>
        public void Reconnect()
        {
            if (AuthCache != null)
            {
                var cached = AuthCache.GetAuthorization();
                if (cached != null && !string.IsNullOrEmpty(cached.AuthToken))
                    _session.SetAuth(cached);
            }
            Authorize();
        }

        /// <summary>
        /// Query wallet capabilities. Returns supported features and limits.
        /// </summary>
        public void GetCapabilities()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_bridge == null) return;

            string authToken = CurrentAuth?.AuthToken ?? "";
            _bridge.CallStatic("getCapabilities",
                Identity.Uri,
                Identity.Icon,
                Identity.Name,
                ClusterUtil.ClusterToChain(ActiveCluster),
                authToken
            );
            _pollAction = "get_capabilities";
#else
            Debug.LogWarning("MobileWalletAdapter: Only available on Android devices.");
#endif
        }

        /// <summary>
        /// Sign one or more transactions. Wallet signs but does NOT submit to network.
        /// </summary>
        public void SignTransactions(byte[][] payloads)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_bridge == null)
            {
                OnTransactionsSignFailed?.Invoke(MWAErrorCode.NotSigned, "Android bridge not available");
                return;
            }

            if (!IsAuthorized)
            {
                OnTransactionsSignFailed?.Invoke(MWAErrorCode.AuthorizationFailed, "Not authorized. Call Authorize() first.");
                return;
            }

            _session.SetState(ConnectionState.Signing);

            string[] encoded = new string[payloads.Length];
            for (int i = 0; i < payloads.Length; i++)
                encoded[i] = Convert.ToBase64String(payloads[i]);

            _bridge.CallStatic("signTransactions",
                Identity.Uri,
                Identity.Icon,
                Identity.Name,
                ClusterUtil.ClusterToChain(ActiveCluster),
                CurrentAuth.AuthToken,
                encoded
            );
            _pollAction = "sign_transactions";
#else
            OnTransactionsSignFailed?.Invoke(MWAErrorCode.NotSigned, "Only available on Android");
#endif
        }

        /// <summary>
        /// Sign and send one or more transactions. Wallet signs AND submits to network.
        /// </summary>
        public void SignAndSendTransactions(byte[][] payloads, SendOptions options = null)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_bridge == null)
            {
                OnTransactionsSendFailed?.Invoke(MWAErrorCode.NotSubmitted, "Android bridge not available");
                return;
            }

            if (!IsAuthorized)
            {
                OnTransactionsSendFailed?.Invoke(MWAErrorCode.AuthorizationFailed, "Not authorized. Call Authorize() first.");
                return;
            }

            _session.SetState(ConnectionState.Signing);

            string[] encoded = new string[payloads.Length];
            for (int i = 0; i < payloads.Length; i++)
                encoded[i] = Convert.ToBase64String(payloads[i]);

            string optionsJson = options != null ? options.ToJson() : "";

            _bridge.CallStatic("signAndSendTransactions",
                Identity.Uri,
                Identity.Icon,
                Identity.Name,
                ClusterUtil.ClusterToChain(ActiveCluster),
                CurrentAuth.AuthToken,
                encoded,
                optionsJson
            );
            _pollAction = "sign_and_send_transactions";
#else
            OnTransactionsSendFailed?.Invoke(MWAErrorCode.NotSubmitted, "Only available on Android");
#endif
        }

        /// <summary>
        /// Sign one or more arbitrary messages.
        /// </summary>
        public void SignMessages(byte[][] messages, byte[][] addresses = null)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_bridge == null)
            {
                OnMessagesSignFailed?.Invoke(MWAErrorCode.NotSigned, "Android bridge not available");
                return;
            }

            if (!IsAuthorized)
            {
                OnMessagesSignFailed?.Invoke(MWAErrorCode.AuthorizationFailed, "Not authorized. Call Authorize() first.");
                return;
            }

            _session.SetState(ConnectionState.Signing);

            string[] encodedMessages = new string[messages.Length];
            for (int i = 0; i < messages.Length; i++)
                encodedMessages[i] = Convert.ToBase64String(messages[i]);

            // Default to first authorized account address if none specified.
            string[] encodedAddresses;
            if (addresses != null && addresses.Length > 0)
            {
                encodedAddresses = new string[addresses.Length];
                for (int i = 0; i < addresses.Length; i++)
                    encodedAddresses[i] = Convert.ToBase64String(addresses[i]);
            }
            else if (CurrentAuth.Accounts != null && CurrentAuth.Accounts.Length > 0)
            {
                encodedAddresses = new string[] { CurrentAuth.Accounts[0].PublicKeyBase64 };
            }
            else
            {
                encodedAddresses = new string[0];
            }

            _bridge.CallStatic("signMessages",
                Identity.Uri,
                Identity.Icon,
                Identity.Name,
                ClusterUtil.ClusterToChain(ActiveCluster),
                CurrentAuth.AuthToken,
                encodedMessages,
                encodedAddresses
            );
            _pollAction = "sign_messages";
#else
            OnMessagesSignFailed?.Invoke(MWAErrorCode.NotSigned, "Only available on Android");
#endif
        }

        /// <summary>
        /// Clone the current authorization for use in another session.
        /// </summary>
        public void CloneAuthorization()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_bridge == null)
            {
                OnCloneFailed?.Invoke("Android bridge not available");
                return;
            }

            if (!IsAuthorized)
            {
                OnCloneFailed?.Invoke("Not authorized. Call Authorize() first.");
                return;
            }

            _bridge.CallStatic("cloneAuthorization",
                Identity.Uri,
                Identity.Icon,
                Identity.Name,
                CurrentAuth.AuthToken
            );
            _pollAction = "clone_authorization";
#else
            OnCloneFailed?.Invoke("Only available on Android");
#endif
        }

        // --- Query methods ---

        /// <summary>Get the primary authorized account, or null.</summary>
        public Account GetAccount() => _session.GetAccount();

        /// <summary>Get all authorized accounts.</summary>
        public Account[] GetAccounts() => _session.GetAccounts();

        /// <summary>Get the public key of the primary authorized account.</summary>
        public byte[] GetPublicKey() => _session.GetPublicKey();

        /// <summary>Replace the cache implementation at runtime.</summary>
        public void SetCache(IMWACache cache)
        {
            AuthCache = cache;
            if (AuthCache != null)
            {
                var cached = AuthCache.GetAuthorization();
                if (cached != null && !string.IsNullOrEmpty(cached.AuthToken))
                    _session.SetAuth(cached);
            }
        }

        // ===================================================================
        // ANDROID POLLING
        // ===================================================================

#if UNITY_ANDROID && !UNITY_EDITOR
        private void PollAndroidStatus()
        {
            int status = _bridge.CallStatic<int>("getStatus");

            // 0 = pending, skip
            if (status == 0) return;

            string action = _pollAction;
            _pollAction = "";

            switch (action)
            {
                case "authorize":
                    HandleAuthorizeResult(status);
                    break;
                case "deauthorize":
                    HandleDeauthorizeResult(status);
                    break;
                case "get_capabilities":
                    HandleCapabilitiesResult(status);
                    break;
                case "sign_transactions":
                    HandleSignTransactionsResult(status);
                    break;
                case "sign_and_send_transactions":
                    HandleSignAndSendResult(status);
                    break;
                case "sign_messages":
                    HandleSignMessagesResult(status);
                    break;
                case "clone_authorization":
                    HandleCloneResult(status);
                    break;
            }
        }

        private void HandleAuthorizeResult(int status)
        {
            if (status == 1) // Success
            {
                string resultJson = _bridge.CallStatic<string>("getResultJson");
                var auth = ParseAuthResult(resultJson);
                if (auth != null)
                {
                    _session.SetAuth(auth);
                    if (AuthCache != null)
                        AuthCache.SetAuthorization(auth);
                    _session.SetState(ConnectionState.Connected);
                    OnAuthorized?.Invoke(auth);
                }
                else
                {
                    _session.SetState(ConnectionState.Disconnected);
                    OnAuthorizationFailed?.Invoke(MWAErrorCode.AuthorizationFailed, "Failed to parse auth result");
                }
            }
            else
            {
                string errorMsg = _bridge.CallStatic<string>("getErrorMessage");
                int errorCode = _bridge.CallStatic<int>("getErrorCode");
                _session.SetState(ConnectionState.Disconnected);
                OnAuthorizationFailed?.Invoke((MWAErrorCode)errorCode, errorMsg);
            }
            _bridge.CallStatic("clearState");
        }

        private void HandleDeauthorizeResult(int status)
        {
            ClearAuth();
            if (status == 1)
            {
                OnDeauthorized?.Invoke();
            }
            else
            {
                string errorMsg = _bridge.CallStatic<string>("getErrorMessage");
                OnDeauthorizationFailed?.Invoke(errorMsg);
            }
            _bridge.CallStatic("clearState");
        }

        private void HandleCapabilitiesResult(int status)
        {
            if (status == 1)
            {
                string resultJson = _bridge.CallStatic<string>("getResultJson");
                var caps = ParseCapabilities(resultJson);
                if (caps != null)
                {
                    Capabilities = caps;
                    OnCapabilitiesReceived?.Invoke(caps);
                }
            }
            _bridge.CallStatic("clearState");
        }

        private void HandleSignTransactionsResult(int status)
        {
            _session.SetState(ConnectionState.Connected);
            if (status == 1)
            {
                string resultJson = _bridge.CallStatic<string>("getResultJson");
                var signedPayloads = ParseBase64Array(resultJson, "signed_payloads");
                if (signedPayloads != null)
                {
                    OnTransactionsSigned?.Invoke(signedPayloads);
                }
                else
                {
                    OnTransactionsSignFailed?.Invoke(MWAErrorCode.NotSigned, "Failed to parse signed transactions");
                }
            }
            else
            {
                string errorMsg = _bridge.CallStatic<string>("getErrorMessage");
                int errorCode = _bridge.CallStatic<int>("getErrorCode");
                OnTransactionsSignFailed?.Invoke((MWAErrorCode)errorCode, errorMsg);
            }
            _bridge.CallStatic("clearState");
        }

        private void HandleSignAndSendResult(int status)
        {
            _session.SetState(ConnectionState.Connected);
            if (status == 1)
            {
                string resultJson = _bridge.CallStatic<string>("getResultJson");
                var signatures = ParseStringArray(resultJson, "signatures");
                if (signatures != null)
                {
                    OnTransactionsSent?.Invoke(signatures);
                }
                else
                {
                    OnTransactionsSendFailed?.Invoke(MWAErrorCode.NotSubmitted, "Failed to parse send result");
                }
            }
            else
            {
                string errorMsg = _bridge.CallStatic<string>("getErrorMessage");
                int errorCode = _bridge.CallStatic<int>("getErrorCode");
                OnTransactionsSendFailed?.Invoke((MWAErrorCode)errorCode, errorMsg);
            }
            _bridge.CallStatic("clearState");
        }

        private void HandleSignMessagesResult(int status)
        {
            _session.SetState(ConnectionState.Connected);
            if (status == 1)
            {
                string resultJson = _bridge.CallStatic<string>("getResultJson");
                var signatures = ParseBase64Array(resultJson, "signatures");
                if (signatures != null)
                {
                    OnMessagesSigned?.Invoke(signatures);
                }
                else
                {
                    OnMessagesSignFailed?.Invoke(MWAErrorCode.NotSigned, "Failed to parse message signatures");
                }
            }
            else
            {
                string errorMsg = _bridge.CallStatic<string>("getErrorMessage");
                int errorCode = _bridge.CallStatic<int>("getErrorCode");
                OnMessagesSignFailed?.Invoke((MWAErrorCode)errorCode, errorMsg);
            }
            _bridge.CallStatic("clearState");
        }

        private void HandleCloneResult(int status)
        {
            if (status == 1)
            {
                string resultJson = _bridge.CallStatic<string>("getResultJson");
                string authToken = ParseStringField(resultJson, "auth_token");
                OnAuthorizationCloned?.Invoke(authToken ?? "");
            }
            else
            {
                string errorMsg = _bridge.CallStatic<string>("getErrorMessage");
                OnCloneFailed?.Invoke(errorMsg);
            }
            _bridge.CallStatic("clearState");
        }

        // ===================================================================
        // JSON PARSING HELPERS (minimal, no external dependencies)
        // ===================================================================

        private AuthorizationResult ParseAuthResult(string json)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<AuthResultJson>(json);
                if (wrapper == null) return null;

                var result = new AuthorizationResult
                {
                    AuthToken = wrapper.auth_token,
                    WalletUriBase = wrapper.wallet_uri_base
                };

                if (wrapper.accounts != null)
                {
                    result.Accounts = new Account[wrapper.accounts.Length];
                    for (int i = 0; i < wrapper.accounts.Length; i++)
                    {
                        var acc = wrapper.accounts[i];
                        result.Accounts[i] = new Account
                        {
                            Address = acc.address,
                            PublicKeyBase64 = acc.public_key,
                            Label = acc.label,
                            Icon = acc.icon,
                            Chains = acc.chains,
                            Features = acc.features
                        };
                    }
                }
                else
                {
                    result.Accounts = new Account[0];
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"MWA: Failed to parse auth result: {e.Message}");
                return null;
            }
        }

        private WalletCapabilities ParseCapabilities(string json)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<CapabilitiesJson>(json);
                if (wrapper == null) return null;

                return new WalletCapabilities
                {
                    SupportsCloneAuthorization = wrapper.supports_clone_authorization,
                    SupportsSignAndSend = wrapper.supports_sign_and_send_transactions,
                    MaxTransactions = wrapper.max_transactions_per_request,
                    MaxMessages = wrapper.max_messages_per_request,
                    SupportedVersions = wrapper.supported_transaction_versions,
                    Features = wrapper.features
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"MWA: Failed to parse capabilities: {e.Message}");
                return null;
            }
        }

        private byte[][] ParseBase64Array(string json, string fieldName)
        {
            try
            {
                int start = json.IndexOf($"\"{fieldName}\"");
                if (start < 0) return null;

                int arrStart = json.IndexOf('[', start);
                int arrEnd = json.IndexOf(']', arrStart);
                if (arrStart < 0 || arrEnd < 0) return null;

                string arrContent = json.Substring(arrStart + 1, arrEnd - arrStart - 1).Trim();
                if (string.IsNullOrEmpty(arrContent)) return new byte[0][];

                string[] items = arrContent.Split(',');
                byte[][] result = new byte[items.Length][];
                for (int i = 0; i < items.Length; i++)
                {
                    string b64 = items[i].Trim().Trim('"');
                    result[i] = Convert.FromBase64String(b64);
                }
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"MWA: Failed to parse base64 array '{fieldName}': {e.Message}");
                return null;
            }
        }

        private string[] ParseStringArray(string json, string fieldName)
        {
            try
            {
                int start = json.IndexOf($"\"{fieldName}\"");
                if (start < 0) return null;

                int arrStart = json.IndexOf('[', start);
                int arrEnd = json.IndexOf(']', arrStart);
                if (arrStart < 0 || arrEnd < 0) return null;

                string arrContent = json.Substring(arrStart + 1, arrEnd - arrStart - 1).Trim();
                if (string.IsNullOrEmpty(arrContent)) return new string[0];

                string[] items = arrContent.Split(',');
                string[] result = new string[items.Length];
                for (int i = 0; i < items.Length; i++)
                    result[i] = items[i].Trim().Trim('"');
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"MWA: Failed to parse string array '{fieldName}': {e.Message}");
                return null;
            }
        }

        private string ParseStringField(string json, string fieldName)
        {
            try
            {
                int start = json.IndexOf($"\"{fieldName}\"");
                if (start < 0) return null;

                int colonPos = json.IndexOf(':', start);
                int valStart = json.IndexOf('"', colonPos + 1);
                int valEnd = json.IndexOf('"', valStart + 1);
                if (valStart < 0 || valEnd < 0) return null;

                return json.Substring(valStart + 1, valEnd - valStart - 1);
            }
            catch
            {
                return null;
            }
        }

        // JSON wrapper classes for Unity's JsonUtility
        [Serializable] private class AuthResultJson
        {
            public string auth_token;
            public string wallet_uri_base;
            public AccountJson[] accounts;
        }

        [Serializable] private class AccountJson
        {
            public string address;
            public string public_key;
            public string label;
            public string icon;
            public string[] chains;
            public string[] features;
        }

        [Serializable] private class CapabilitiesJson
        {
            public bool supports_clone_authorization;
            public bool supports_sign_and_send_transactions;
            public int max_transactions_per_request;
            public int max_messages_per_request;
            public string[] supported_transaction_versions;
            public string[] features;
        }

#endif

        private void ClearAuth()
        {
            _session.ClearAuth();
            if (AuthCache != null)
                AuthCache.Clear();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
