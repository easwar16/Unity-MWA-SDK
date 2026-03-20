# API Reference

## MobileWalletAdapter (MonoBehaviour)

The main entry point for all MWA operations. Access via singleton or attach to a GameObject.

```csharp
using Solana.MWA;

var mwa = MobileWalletAdapter.Instance;
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Identity` | `DappIdentity` | Dapp identity presented to wallet during authorization |
| `ActiveCluster` | `Cluster` | Blockchain cluster to connect to (Devnet, Mainnet, Testnet) |
| `AuthCache` | `IMWACache` | Authorization cache implementation (default: FileMWACache) |
| `State` | `ConnectionState` | Current connection state (read-only) |
| `CurrentAuth` | `AuthorizationResult` | Current authorization result (read-only) |
| `Capabilities` | `WalletCapabilities` | Last queried wallet capabilities (read-only) |
| `IsAuthorized` | `bool` | Whether we have a valid authorization (read-only) |
| `IsConnected` | `bool` | Whether connected with active session (read-only) |

### Methods

#### Authorize

```csharp
void Authorize(SignInPayload signInPayload = null)
```

Authorize this dapp with a wallet. If a cached auth token exists, attempts reauthorization. Optionally include a Sign In With Solana payload.

```csharp
// Simple authorization
mwa.Authorize();

// With Sign In With Solana
mwa.Authorize(new SignInPayload {
    Domain = "mygame.com",
    Statement = "Sign in to My Game"
});
```

#### Deauthorize

```csharp
void Deauthorize()
```

Revoke the current auth token and disconnect from the wallet. Clears the auth cache.

#### Disconnect

```csharp
void Disconnect()
```

Alias for `Deauthorize()`.

#### Reconnect

```csharp
void Reconnect()
```

Reconnect using a cached authorization. If the cache is empty, performs a full `Authorize()`.

#### GetCapabilities

```csharp
void GetCapabilities()
```

Query the wallet's capabilities (supported features, limits, transaction versions).

#### SignTransactions

```csharp
void SignTransactions(byte[][] payloads)
```

Sign one or more transactions without submitting to the network.

```csharp
byte[] txBytes = BuildTransaction(); // Your transaction building logic
mwa.SignTransactions(new byte[][] { txBytes });
```

#### SignAndSendTransactions

```csharp
void SignAndSendTransactions(byte[][] payloads, SendOptions options = null)
```

Sign and submit one or more transactions to the network.

```csharp
mwa.SignAndSendTransactions(
    new byte[][] { txBytes },
    new SendOptions { Commitment = "confirmed" }
);
```

#### SignMessages

```csharp
void SignMessages(byte[][] messages, byte[][] addresses = null)
```

Sign one or more arbitrary messages. If `addresses` is null, defaults to the first authorized account.

```csharp
byte[] msg = System.Text.Encoding.UTF8.GetBytes("Hello Solana!");
mwa.SignMessages(new byte[][] { msg });
```

#### CloneAuthorization

```csharp
void CloneAuthorization()
```

Clone the current authorization for sharing with another session.

#### Query Methods

```csharp
Account GetAccount()        // Primary authorized account, or null
Account[] GetAccounts()     // All authorized accounts
byte[] GetPublicKey()       // Public key of primary account
void SetCache(IMWACache c)  // Replace cache at runtime
```

### Events

| Event | Signature | Description |
|-------|-----------|-------------|
| `OnAuthorized` | `Action<AuthorizationResult>` | Authorization succeeded |
| `OnAuthorizationFailed` | `Action<MWAErrorCode, string>` | Authorization failed |
| `OnDeauthorized` | `Action` | Deauthorization succeeded |
| `OnDeauthorizationFailed` | `Action<string>` | Deauthorization failed |
| `OnCapabilitiesReceived` | `Action<WalletCapabilities>` | Capabilities query succeeded |
| `OnTransactionsSigned` | `Action<byte[][]>` | Transactions signed successfully |
| `OnTransactionsSignFailed` | `Action<MWAErrorCode, string>` | Transaction signing failed |
| `OnTransactionsSent` | `Action<string[]>` | Transactions sent (signatures returned) |
| `OnTransactionsSendFailed` | `Action<MWAErrorCode, string>` | Transaction send failed |
| `OnMessagesSigned` | `Action<byte[][]>` | Messages signed successfully |
| `OnMessagesSignFailed` | `Action<MWAErrorCode, string>` | Message signing failed |
| `OnAuthorizationCloned` | `Action<string>` | Authorization cloned (new token) |
| `OnCloneFailed` | `Action<string>` | Clone authorization failed |
| `OnStateChanged` | `Action<ConnectionState>` | Connection state changed |

---

## Data Types

### Cluster (enum)

```csharp
public enum Cluster { Devnet, Mainnet, Testnet }
```

### MWAErrorCode (enum)

| Value | Code | Description |
|-------|------|-------------|
| `AuthorizationFailed` | -1 | Authorization was rejected or failed |
| `InvalidPayloads` | -2 | Transaction payloads were invalid |
| `NotSigned` | -3 | Signing was declined or failed |
| `NotSubmitted` | -4 | Transaction submission failed |
| `NotCloned` | -5 | Clone authorization failed |
| `TooManyPayloads` | -6 | Too many payloads for wallet limit |
| `ClusterNotSupported` | -7 | Wallet doesn't support the requested cluster |
| `AttestOriginAndroid` | -100 | Android origin attestation error |

### ConnectionState (enum)

```csharp
public enum ConnectionState { Disconnected, Connecting, Connected, Signing, Deauthorizing }
```

### Account

| Field | Type | Description |
|-------|------|-------------|
| `Address` | `string` | Base64-encoded account address |
| `PublicKeyBase64` | `string` | Base64-encoded public key |
| `Label` | `string` | Human-readable account label |
| `Icon` | `string` | Account icon URI |
| `Chains` | `string[]` | Supported blockchain chains |
| `Features` | `string[]` | Supported features |

Method: `byte[] GetPublicKey()` — Decode the public key from base64.

### AuthorizationResult

| Field | Type | Description |
|-------|------|-------------|
| `Accounts` | `Account[]` | Authorized accounts |
| `AuthToken` | `string` | Authorization token for session reuse |
| `WalletUriBase` | `string` | Wallet's base URI |
| `SignInResult` | `SignInResult` | SIWS result (if applicable) |

### WalletCapabilities

| Field | Type | Description |
|-------|------|-------------|
| `SupportsCloneAuthorization` | `bool` | Whether clone auth is supported |
| `SupportsSignAndSend` | `bool` | Whether sign-and-send is supported |
| `MaxTransactions` | `int` | Max transactions per signing request |
| `MaxMessages` | `int` | Max messages per signing request |
| `SupportedVersions` | `string[]` | Supported transaction versions |
| `Features` | `string[]` | Additional wallet features |

### DappIdentity

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Name` | `string` | "Unity dApp" | Display name shown in wallet |
| `Uri` | `string` | "https://solana.com" | Dapp URI |
| `Icon` | `string` | "favicon.ico" | Icon path/URI |

### SendOptions

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Commitment` | `string` | "confirmed" | Commitment level |
| `MinContextSlot` | `int` | -1 | Minimum context slot (-1 = unset) |
| `SkipPreflight` | `bool` | false | Skip preflight checks |
| `MaxRetries` | `int` | -1 | Max retries (-1 = unset) |

### SignInPayload

| Field | Type | Description |
|-------|------|-------------|
| `Domain` | `string` | Domain requesting sign-in |
| `Address` | `string` | Requested address |
| `Statement` | `string` | Human-readable statement |
| `Uri` | `string` | URI for the sign-in request |
| `Version` | `string` | SIWS version |
| `ChainId` | `string` | Chain identifier |
| `Nonce` | `string` | Random nonce |
| `IssuedAt` | `string` | ISO 8601 timestamp |
| `ExpirationTime` | `string` | Expiration timestamp |
| `NotBefore` | `string` | Not-before timestamp |
| `RequestId` | `string` | Request identifier |
| `Resources` | `string[]` | Resource URIs |

### SignInResult

| Field | Type | Description |
|-------|------|-------------|
| `Address` | `string` | Signed-in address |
| `SignedMessageBase64` | `string` | Base64-encoded signed message |
| `SignatureBase64` | `string` | Base64-encoded signature |
| `SignatureType` | `string` | Signature algorithm type |

---

## ClusterUtil

Static utility class for cluster/chain conversion.

```csharp
string chain = ClusterUtil.ClusterToChain(Cluster.Devnet);  // "solana:devnet"
Cluster cluster = ClusterUtil.ChainToCluster("solana:devnet"); // Cluster.Devnet
```
