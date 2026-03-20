# Solana Mobile Wallet Adapter — Unity SDK

Unity SDK for [Solana Mobile Wallet Adapter (MWA) 2.0](https://github.com/solana-mobile/mobile-wallet-adapter). Provides full API parity with the React Native MWA SDK.

## Features

- **Full MWA 2.0 API** — Authorize, Deauthorize, Sign Transactions, Sign & Send, Sign Messages, Clone Authorization, Get Capabilities
- **Sign In With Solana (SIWS)** support
- **Extensible auth cache** — built-in file cache + custom `IMWACache` interface
- **Event-driven API** — C# events for all operations
- **Singleton pattern** — `MobileWalletAdapter.Instance` for easy access
- **Zero external Unity dependencies**

## Quick Start

```csharp
using Solana.MWA;

public class WalletManager : MonoBehaviour
{
    void Start()
    {
        var mwa = MobileWalletAdapter.Instance;
        mwa.Identity = new DappIdentity("My Game", "https://mygame.com", "icon.png");
        mwa.ActiveCluster = Cluster.Devnet;

        mwa.OnAuthorized += (result) =>
            Debug.Log($"Connected: {result.Accounts[0].Address}");

        mwa.Authorize();
    }
}
```

## Installation

### Unity Package Manager (git URL)

```
https://github.com/nicoeseworthy/Unity-MWA-SDK.git
```

### Requirements

- Unity 2021.3+
- Android API 24+ (Android 7.0)
- MWA-compatible wallet on device (Phantom, Solflare)

## API Overview

| Method | Description |
|--------|-------------|
| `Authorize()` | Connect to wallet |
| `Deauthorize()` / `Disconnect()` | Disconnect from wallet |
| `Reconnect()` | Restore session from cache |
| `GetCapabilities()` | Query wallet features |
| `SignTransactions(payloads)` | Sign without sending |
| `SignAndSendTransactions(payloads, options)` | Sign and submit |
| `SignMessages(messages)` | Sign arbitrary messages |
| `CloneAuthorization()` | Clone auth for another session |

## Documentation

- [Getting Started](Documentation~/index.md)
- [Installation](Documentation~/installation.md)
- [API Reference](Documentation~/api-reference.md)
- [Cache Layer](Documentation~/cache-layer.md)
- [Migration from React Native](Documentation~/migration-from-rn.md)

## Building the Android Plugin

```bash
cd Android~
# Place Unity's classes.jar in libs/
./gradlew assembleRelease
# Copy output AAR to Runtime/Plugins/Android/
```

## License

Apache License 2.0. See [LICENSE](LICENSE).
# Unity-MWA-SDK
