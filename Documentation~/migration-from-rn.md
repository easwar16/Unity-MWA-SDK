# Migration from React Native

This guide maps the React Native MWA SDK API to the Unity MWA SDK for developers familiar with the React Native implementation.

## API Mapping

### Core Methods

| React Native | Unity | Notes |
|-------------|-------|-------|
| `transact(callback)` | N/A | Unity handles sessions internally |
| `authorize({...})` | `mwa.Authorize()` | Identity set via `mwa.Identity` property |
| `reauthorize({...})` | `mwa.Reconnect()` | Loads from cache and reauthorizes |
| `deauthorize({auth_token})` | `mwa.Deauthorize()` | Uses current session's token |
| `getCapabilities()` | `mwa.GetCapabilities()` | |
| `signTransactions({payloads})` | `mwa.SignTransactions(payloads)` | `byte[][]` instead of `Uint8Array[]` |
| `signAndSendTransactions({payloads, options})` | `mwa.SignAndSendTransactions(payloads, options)` | |
| `signMessages({payloads, addresses})` | `mwa.SignMessages(messages, addresses)` | |
| `cloneAuthorization()` | `mwa.CloneAuthorization()` | |

### Authorization

**React Native:**
```javascript
const result = await transact(async (wallet) => {
    const auth = await wallet.authorize({
        identity: {
            name: "My App",
            uri: "https://myapp.com",
            icon: "icon.png"
        },
        cluster: "devnet"
    });
    return auth;
});
```

**Unity:**
```csharp
var mwa = MobileWalletAdapter.Instance;
mwa.Identity = new DappIdentity("My App", "https://myapp.com", "icon.png");
mwa.ActiveCluster = Cluster.Devnet;
mwa.OnAuthorized += (result) => {
    Debug.Log($"Auth token: {result.AuthToken}");
};
mwa.Authorize();
```

### Async Patterns

| React Native | Unity |
|-------------|-------|
| `async/await` with Promises | Event-based callbacks |
| `try/catch` for errors | Separate error events |
| Single `transact()` wrapping | Direct method calls |

**React Native error handling:**
```javascript
try {
    const result = await transact(async (wallet) => {
        return await wallet.signTransactions({payloads});
    });
} catch (e) {
    console.error(e);
}
```

**Unity error handling:**
```csharp
mwa.OnTransactionsSigned += (signedPayloads) => {
    // Success
};
mwa.OnTransactionsSignFailed += (errorCode, errorMessage) => {
    Debug.LogError($"Sign failed: {errorMessage}");
};
mwa.SignTransactions(payloads);
```

### Cache / AuthorizationResultCache

**React Native:**
```javascript
import { AuthorizationResultCache } from '@solana-mobile/mobile-wallet-adapter-protocol';

class MyCache implements AuthorizationResultCache {
    async get(): Promise<AuthorizationResult | undefined> { ... }
    async set(result: AuthorizationResult): Promise<void> { ... }
    async clear(): Promise<void> { ... }
}
```

**Unity:**
```csharp
public class MyCache : IMWACache {
    public AuthorizationResult GetAuthorization() { ... }
    public void SetAuthorization(AuthorizationResult auth) { ... }
    public void Clear() { ... }
    public bool HasAuthorization() { ... }
}

mwa.SetCache(new MyCache());
```

### Sign In With Solana (SIWS)

**React Native:**
```javascript
const result = await transact(async (wallet) => {
    return await wallet.authorize({
        identity: {...},
        sign_in_payload: {
            domain: "myapp.com",
            statement: "Sign in to My App"
        }
    });
});
```

**Unity:**
```csharp
mwa.Authorize(new SignInPayload {
    Domain = "myapp.com",
    Statement = "Sign in to My App"
});
```

### Data Type Differences

| React Native | Unity | Notes |
|-------------|-------|-------|
| `Uint8Array` | `byte[]` | Transaction/message payloads |
| `Uint8Array[]` | `byte[][]` | Arrays of payloads |
| `string` (base58) | `string` (base64) | Account addresses from bridge |
| `Promise<T>` | Event callbacks | Async pattern |
| `number` | `int` / `MWAErrorCode` | Error codes |

## Feature Parity Checklist

| Feature | React Native | Unity |
|---------|-------------|-------|
| Authorize | Yes | Yes |
| Reauthorize | Yes | Yes (via Reconnect) |
| Deauthorize | Yes | Yes |
| Get Capabilities | Yes | Yes |
| Sign Transactions | Yes | Yes |
| Sign & Send Transactions | Yes | Yes |
| Sign Messages | Yes | Yes |
| Clone Authorization | Yes | Yes |
| Auth Cache | Yes | Yes (IMWACache) |
| Sign In With Solana | Yes | Yes (SignInPayload) |
| Multiple Accounts | Yes | Yes |
| Send Options | Yes | Yes |
| Cluster Selection | Yes | Yes |
