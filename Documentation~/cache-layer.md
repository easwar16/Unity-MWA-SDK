# Cache Layer

The MWA SDK includes an extensible authorization cache that persists auth tokens across app restarts. This enables users to reconnect to their wallet without re-authorizing every time.

## Default: FileMWACache

The built-in `FileMWACache` stores authorization data as a JSON file at:
```
Application.persistentDataPath/mwa_auth_cache.json
```

This is used automatically — no configuration needed.

## How It Works

1. When `Authorize()` succeeds, the SDK calls `AuthCache.SetAuthorization(result)` to persist the token
2. On app start, the SDK calls `AuthCache.GetAuthorization()` to restore any cached session
3. When `Deauthorize()` is called, the SDK calls `AuthCache.Clear()` to remove the cached data
4. `Reconnect()` loads the cached token and attempts reauthorization with the wallet

## IMWACache Interface

```csharp
public interface IMWACache
{
    AuthorizationResult GetAuthorization();
    void SetAuthorization(AuthorizationResult auth);
    void Clear();
    bool HasAuthorization();
}
```

## Custom Cache Implementations

### PlayerPrefs Cache

```csharp
using UnityEngine;
using Solana.MWA;

public class PlayerPrefsMWACache : IMWACache
{
    private const string Key = "mwa_auth_cache";

    public AuthorizationResult GetAuthorization()
    {
        string json = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(json)) return null;

        // Parse JSON back to AuthorizationResult
        var data = JsonUtility.FromJson<CacheData>(json);
        if (data == null || string.IsNullOrEmpty(data.auth_token))
            return null;

        return new AuthorizationResult
        {
            AuthToken = data.auth_token,
            WalletUriBase = data.wallet_uri_base,
            Accounts = new Account[]
            {
                new Account
                {
                    Address = data.address,
                    PublicKeyBase64 = data.public_key
                }
            }
        };
    }

    public void SetAuthorization(AuthorizationResult auth)
    {
        if (auth == null) { Clear(); return; }

        var data = new CacheData
        {
            auth_token = auth.AuthToken,
            wallet_uri_base = auth.WalletUriBase,
            address = auth.Accounts?.Length > 0 ? auth.Accounts[0].Address : "",
            public_key = auth.Accounts?.Length > 0 ? auth.Accounts[0].PublicKeyBase64 : ""
        };
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

    public bool HasAuthorization()
    {
        var auth = GetAuthorization();
        return auth != null && !string.IsNullOrEmpty(auth.AuthToken);
    }

    [System.Serializable]
    private class CacheData
    {
        public string auth_token;
        public string wallet_uri_base;
        public string address;
        public string public_key;
    }
}
```

### Using a Custom Cache

```csharp
var mwa = MobileWalletAdapter.Instance;
mwa.SetCache(new PlayerPrefsMWACache());
```

Or set directly:

```csharp
mwa.AuthCache = new PlayerPrefsMWACache();
```

### Cache Lifecycle

| Event | Cache Action |
|-------|-------------|
| `Authorize()` succeeds | `SetAuthorization()` called |
| `Deauthorize()` called | `Clear()` called |
| App starts | `GetAuthorization()` called to restore session |
| `Reconnect()` called | `GetAuthorization()` loads cached token, then `Authorize()` revalidates |

## Security Considerations

- The default `FileMWACache` stores tokens in the app's private storage directory, which is sandboxed on Android
- Auth tokens are opaque strings issued by the wallet — they don't contain private keys
- Tokens can be revoked by the wallet at any time
- For high-security applications, consider implementing an encrypted cache or clearing the cache on specific app events
