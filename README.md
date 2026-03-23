# Solana Mobile Wallet Adapter — Unity SDK

<p align="center">
  <video src="https://github.com/user-attachments/assets/6a2648cc-1e2c-4675-b144-465a7732b346" width="300" autoplay loop muted playsinline></video>
</p>

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![MWA](https://img.shields.io/badge/MWA-2.0-green.svg)](https://github.com/solana-mobile/mobile-wallet-adapter)
[![Unity](https://img.shields.io/badge/Unity-2021.3+-black.svg)](https://unity.com)

Unity SDK for [Solana Mobile Wallet Adapter (MWA) 2.0](https://github.com/solana-mobile/mobile-wallet-adapter), providing **full API parity with the React Native MWA SDK**. Build Android games and apps that connect to Solana wallets like Phantom and Solflare.

## Overview

Mobile Wallet Adapter (MWA) is a protocol by [Solana Mobile](https://solanamobile.com) that enables Android dApps to communicate with wallet apps installed on the same device. Instead of bundling wallet functionality into your app, MWA leverages the user's existing wallet for transaction signing and authorization.

This SDK wraps the official [`mobile-wallet-adapter-clientlib-ktx`](https://github.com/solana-mobile/mobile-wallet-adapter) Android library and exposes it to Unity C# through a JNI bridge, following the same architecture as the [Godot MWA SDK](https://github.com/nicoeseworthy/Godot-MWA-SDK).

## Features

- **Full MWA 2.0 API parity** with the React Native SDK
- **All wallet methods**: Authorize, Reauthorize, Deauthorize, Sign Transactions, Sign & Send Transactions, Sign Messages, Get Capabilities
- **Sign In With Solana (SIWS)** support via `SignInPayload`
- **Extensible authorization cache** — built-in `FileMWACache` + custom `IMWACache` interface
- **Disconnect & Reconnect** — easily manage wallet sessions with cache persistence
- **Event-driven API** — C# events for all async operations
- **Cluster selection** — Devnet, Mainnet, Testnet
- **Singleton pattern** — `MobileWalletAdapter.Instance` for easy global access
- **Zero external Unity dependencies** — works with any Unity 2021.3+ project

## Quick Start

### 1. Install the SDK

Add via Unity Package Manager using the git URL:

**Window > Package Manager > + > Add package from git URL:**
```
https://github.com/nicoeseworthy/Unity-MWA-SDK.git
```

### 2. Configure Android Build

- **File > Build Profiles** > Select **Android** > **Switch Platform**
- **Player Settings > Other Settings**:
  - Minimum API Level: **Android 7.0 (API 24)**
  - Target API Level: **Automatic (highest installed)**
- **Player Settings > Publishing Settings**:
  - Enable **Custom Main Gradle Template**
  - Enable **Custom Gradle Properties Template**
  - Enable **Custom Gradle Settings Template**
- Add MWA dependencies to `Assets/Plugins/Android/mainTemplate.gradle`:
  ```groovy
  dependencies {
      implementation 'com.solanamobile:mobile-wallet-adapter-clientlib-ktx:2.0.3'
      implementation 'com.solanamobile:rpc-core:0.2.8'
      implementation 'androidx.activity:activity-compose:1.8.2'
      implementation 'androidx.lifecycle:lifecycle-runtime-ktx:2.7.0'
      implementation 'org.jetbrains.kotlinx:kotlinx-coroutines-android:1.7.3'
      implementation 'androidx.compose.ui:ui:1.5.4'
      implementation 'androidx.compose.material:material:1.5.4'
  }
  ```
- Add to `Assets/Plugins/Android/gradleTemplate.properties`:
  ```
  android.useAndroidX=true
  android.enableJetifier=true
  ```

### 3. Connect to a Wallet

```csharp
using Solana.MWA;
using UnityEngine;

public class WalletManager : MonoBehaviour
{
    private MobileWalletAdapter mwa;

    void Start()
    {
        mwa = MobileWalletAdapter.Instance;

        // Configure your dApp identity (shown to user in wallet)
        mwa.Identity = new DappIdentity(
            "My Game",
            "https://mygame.com",
            "favicon.ico"
        );
        mwa.ActiveCluster = Cluster.Devnet;

        // Listen for events
        mwa.OnAuthorized += OnWalletConnected;
        mwa.OnAuthorizationFailed += OnWalletFailed;
        mwa.OnDeauthorized += () => Debug.Log("Disconnected");
    }

    public void ConnectWallet()
    {
        mwa.Authorize();
    }

    void OnWalletConnected(AuthorizationResult result)
    {
        Debug.Log($"Connected! Account: {result.Accounts[0].Address}");
        Debug.Log($"Auth token: {result.AuthToken}");
    }

    void OnWalletFailed(MWAErrorCode code, string message)
    {
        Debug.LogError($"Failed ({code}): {message}");
    }
}
```

### 4. Build & Run

Build to an Android device with a MWA-compatible wallet installed (Phantom, Solflare).

## API Reference

### MobileWalletAdapter

The main entry point. Access via `MobileWalletAdapter.Instance` (singleton) or attach to a GameObject.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Identity` | `DappIdentity` | dApp identity shown to user in wallet prompt |
| `ActiveCluster` | `Cluster` | Blockchain cluster (Devnet, Mainnet, Testnet) |
| `AuthCache` | `IMWACache` | Authorization cache (default: `FileMWACache`) |
| `State` | `ConnectionState` | Current state (Disconnected, Connecting, Connected, Signing, Deauthorizing) |
| `CurrentAuth` | `AuthorizationResult` | Current authorization (accounts, auth token) |
| `Capabilities` | `WalletCapabilities` | Last queried wallet capabilities |
| `IsAuthorized` | `bool` | Has valid authorization |
| `IsConnected` | `bool` | Connected with active session |

#### Authorization Methods

| Method | React Native Equivalent | Description |
|--------|------------------------|-------------|
| `Authorize(signInPayload?)` | `wallet.authorize()` | Connect to wallet, optionally with SIWS |
| `Reconnect()` | `wallet.reauthorize()` | Restore session from cached auth token |
| `Deauthorize()` | `wallet.deauthorize()` | Revoke authorization, clear cache |
| `Disconnect()` | — | Alias for `Deauthorize()` |

#### Signing Methods

| Method | React Native Equivalent | Description |
|--------|------------------------|-------------|
| `SignTransactions(byte[][] payloads)` | `wallet.signTransactions()` | Sign without broadcasting |
| `SignAndSendTransactions(byte[][] payloads, SendOptions options?)` | `wallet.signAndSendTransactions()` | Sign and broadcast to network |
| `SignMessages(byte[][] messages, byte[][] addresses?)` | `wallet.signMessages()` | Sign arbitrary off-chain messages |

#### Query Methods

| Method | React Native Equivalent | Description |
|--------|------------------------|-------------|
| `GetCapabilities()` | `wallet.getCapabilities()` | Query wallet features and limits |
| `GetAccount()` | — | Get primary authorized account |
| `GetAccounts()` | — | Get all authorized accounts |
| `GetPublicKey()` | — | Get primary account public key |
| `SetCache(IMWACache)` | — | Replace cache at runtime |

### Events

| Event | Signature | Description |
|-------|-----------|-------------|
| `OnAuthorized` | `Action<AuthorizationResult>` | Wallet authorized successfully |
| `OnAuthorizationFailed` | `Action<MWAErrorCode, string>` | Authorization rejected or failed |
| `OnDeauthorized` | `Action` | Session ended |
| `OnDeauthorizationFailed` | `Action<string>` | Deauthorization failed |
| `OnCapabilitiesReceived` | `Action<WalletCapabilities>` | Capabilities query completed |
| `OnTransactionsSigned` | `Action<byte[][]>` | Transactions signed |
| `OnTransactionsSignFailed` | `Action<MWAErrorCode, string>` | Signing failed |
| `OnTransactionsSent` | `Action<string[]>` | Transactions sent (signatures) |
| `OnTransactionsSendFailed` | `Action<MWAErrorCode, string>` | Send failed |
| `OnMessagesSigned` | `Action<byte[][]>` | Messages signed |
| `OnMessagesSignFailed` | `Action<MWAErrorCode, string>` | Message signing failed |
| `OnAuthorizationCloned` | `Action<string>` | Auth cloned (new token) |
| `OnCloneFailed` | `Action<string>` | Clone failed |
| `OnStateChanged` | `Action<ConnectionState>` | Connection state changed |

### Data Types

#### DappIdentity
```csharp
new DappIdentity(
    name: "My App",        // Display name in wallet
    uri: "https://app.com", // Your dApp URI
    icon: "favicon.ico"     // Icon URI
)
```

#### SendOptions
```csharp
new SendOptions {
    Commitment = "confirmed",  // "processed", "confirmed", "finalized"
    MinContextSlot = -1,       // -1 = unset
    SkipPreflight = false,
    MaxRetries = -1            // -1 = unset
}
```

#### SignInPayload (Sign In With Solana)
```csharp
new SignInPayload {
    Domain = "mygame.com",
    Statement = "Sign in to My Game",
    // Optional: Uri, Version, ChainId, Nonce, IssuedAt, ExpirationTime, etc.
}
```

#### Error Codes

| Code | Name | Description |
|------|------|-------------|
| -1 | `AuthorizationFailed` | Wallet rejected authorization |
| -2 | `InvalidPayloads` | Invalid transaction payloads |
| -3 | `NotSigned` | Signing declined or failed |
| -4 | `NotSubmitted` | Transaction submission failed |
| -5 | `NotCloned` | Clone authorization failed |
| -6 | `TooManyPayloads` | Exceeds wallet's max per request |
| -7 | `ClusterNotSupported` | Wallet doesn't support requested cluster |
| -100 | `AttestOriginAndroid` | Android origin attestation error |

## Authorization Cache

The SDK includes an extensible cache layer that persists auth tokens across app restarts.

### Default: FileMWACache

Stores authorization as JSON at `Application.persistentDataPath/mwa_auth_cache.json`. Works automatically — no configuration needed.

### Custom Cache

Implement `IMWACache` for custom storage (PlayerPrefs, SQLite, cloud, etc.):

```csharp
public class MyCache : IMWACache
{
    public AuthorizationResult GetAuthorization() { ... }
    public void SetAuthorization(AuthorizationResult auth) { ... }
    public void Clear() { ... }
    public bool HasAuthorization() { ... }
}

// Use it:
MobileWalletAdapter.Instance.SetCache(new MyCache());
```

### Cache Flow

| Event | Action |
|-------|--------|
| `Authorize()` succeeds | `SetAuthorization()` persists the token |
| `Deauthorize()` called | `Clear()` removes cached data |
| App starts | `GetAuthorization()` restores session |
| `Reconnect()` called | Loads cached token, then reauthorizes with wallet |

## Usage Examples

### Sign a Transaction

```csharp
// After authorization...
byte[] transactionBytes = BuildYourSolanaTransaction();
mwa.OnTransactionsSigned += (signedPayloads) => {
    Debug.Log($"Signed {signedPayloads.Length} transactions");
};
mwa.OnTransactionsSignFailed += (code, msg) => {
    Debug.LogError($"Sign failed: {msg}");
};
mwa.SignTransactions(new byte[][] { transactionBytes });
```

### Sign and Send a Transaction

```csharp
mwa.OnTransactionsSent += (signatures) => {
    Debug.Log($"Transaction signature: {signatures[0]}");
};
mwa.SignAndSendTransactions(
    new byte[][] { transactionBytes },
    new SendOptions { Commitment = "confirmed" }
);
```

### Sign a Message

```csharp
byte[] message = System.Text.Encoding.UTF8.GetBytes("Hello Solana!");
mwa.OnMessagesSigned += (signatures) => {
    Debug.Log($"Message signed!");
};
mwa.SignMessages(new byte[][] { message });
```

### Reconnect from Cache

```csharp
// On app start, check for cached session
if (mwa.AuthCache.HasAuthorization())
{
    mwa.Reconnect(); // Reauthorizes with wallet using cached token
}
```

### Sign In With Solana (SIWS)

```csharp
mwa.Authorize(new SignInPayload {
    Domain = "mygame.com",
    Statement = "Sign in to My Game"
});
```

## React Native API Parity

| React Native Method | Unity Method | Status |
|---------------------|-------------|--------|
| `transact(callback)` | N/A (managed internally) | N/A |
| `wallet.authorize()` | `Authorize()` | Done |
| `wallet.reauthorize()` | `Reconnect()` | Done |
| `wallet.deauthorize()` | `Deauthorize()` / `Disconnect()` | Done |
| `wallet.getCapabilities()` | `GetCapabilities()` | Done |
| `wallet.signTransactions()` | `SignTransactions()` | Done |
| `wallet.signAndSendTransactions()` | `SignAndSendTransactions()` | Done |
| `wallet.signMessages()` | `SignMessages()` | Done |
| `wallet.cloneAuthorization()` | `CloneAuthorization()` | Done |
| `AuthorizationResultCache` | `IMWACache` + `FileMWACache` | Done |
| Sign In With Solana (SIWS) | `SignInPayload` on `Authorize()` | Done |

## Architecture

```
Unity C# Layer                    Android Native Layer (Kotlin)
+---------------------+          +---------------------------+
| MobileWalletAdapter | --JNI--> | MWABridge.kt (static)     |
|   (MonoBehaviour)   |          | MWAClient.kt (coroutines) |
+---------------------+          | MWAInitProvider.kt        |
| MWATypes.cs         |          |   Uses:                   |
| IMWACache.cs        |          |   clientlib-ktx:2.0.3     |
| FileMWACache.cs     |          +---------------------------+
| MWASession.cs       |
+---------------------+
```

- **MWAInitProvider** — Android `ContentProvider` that creates `ActivityResultSender` during app startup (before Activity reaches STARTED state, as required by MWA)
- **MWABridge** — Static JNI entry point called from Unity C# via `AndroidJavaClass`
- **MWAClient** — Coroutine-based async operations with polling state (status, resultJson, errorMessage)
- **MobileWalletAdapter** — Unity `MonoBehaviour` that polls the bridge in `Update()` and fires C# events

## Example App

The SDK includes a complete example app demonstrating all API methods:

- Cluster selection (Devnet / Mainnet / Testnet)
- Authorize / Deauthorize / Reconnect
- Get Capabilities
- Sign Transactions
- Sign & Send Transactions
- Sign Messages
- Clone Authorization
- Cache management (load from cache, clear cache)
- Real-time output log

Import via **Package Manager > Solana Mobile Wallet Adapter > Samples > Basic Example**, then run **Solana > MWA > Create Example Scene**.

## Building the Android Plugin

If you need to rebuild the Kotlin plugin from source:

```bash
cd Android~

# Place Unity's classes.jar in libs/
cp <Unity>/PlaybackEngines/AndroidPlayer/Variations/il2cpp/Release/Classes/classes.jar libs/

# Generate Gradle wrapper (if needed)
gradle wrapper --gradle-version 8.2

# Build
./gradlew assembleRelease

# Copy output
cp build/outputs/aar/SolanaMWAUnityPlugin-release.aar ../Runtime/Plugins/Android/solana-mwa-bridge.aar
```

## Documentation

| Document | Description |
|----------|-------------|
| [Getting Started](Documentation~/index.md) | Overview and 5-minute quickstart |
| [Installation](Documentation~/installation.md) | Detailed setup and troubleshooting |
| [API Reference](Documentation~/api-reference.md) | Complete API documentation |
| [Cache Layer](Documentation~/cache-layer.md) | Cache customization guide |
| [Migration from React Native](Documentation~/migration-from-rn.md) | Side-by-side API mapping |

## Requirements

- Unity 2021.3 or later
- Android Build Support module
- Android API 24+ (Android 7.0 Nougat)
- MWA-compatible wallet app on device (Phantom, Solflare)

## License

Apache License 2.0. See [LICENSE](LICENSE).
