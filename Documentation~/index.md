# Solana Mobile Wallet Adapter — Unity SDK

## What is MWA?

Mobile Wallet Adapter (MWA) is a protocol developed by Solana Mobile that enables Android dApps to communicate with wallet apps (like Phantom, Solflare) installed on the same device. Instead of bundling wallet functionality directly into your app, MWA lets you leverage the user's existing wallet for transaction signing and authorization.

## Why use this SDK?

- **Full MWA 2.0 API parity** with the React Native SDK
- **Simple MonoBehaviour API** — attach to a GameObject, call methods, listen to events
- **Extensible auth cache** — persist authorization across app restarts with the built-in file cache, or implement your own
- **Zero external Unity dependencies** — works with any Unity 2021.3+ project

## Quick Start (5 minutes)

### 1. Install the SDK

Add via Unity Package Manager using the git URL:
```
https://github.com/nicoeseworthy/Unity-MWA-SDK.git
```

See [Installation](installation.md) for detailed setup.

### 2. Add MobileWalletAdapter to your scene

```csharp
using Solana.MWA;

public class MyWalletManager : MonoBehaviour
{
    private MobileWalletAdapter mwa;

    void Start()
    {
        mwa = MobileWalletAdapter.Instance;
        mwa.Identity = new DappIdentity("My Game", "https://mygame.com", "icon.png");
        mwa.ActiveCluster = Cluster.Devnet;

        mwa.OnAuthorized += (result) => {
            Debug.Log($"Connected! Account: {result.Accounts[0].Address}");
        };

        mwa.OnAuthorizationFailed += (code, msg) => {
            Debug.LogError($"Auth failed: {msg}");
        };
    }

    public void ConnectWallet()
    {
        mwa.Authorize();
    }
}
```

### 3. Build for Android

- Set minimum API level to **24** (Android 7.0)
- Set target API level to **34**
- Build and run on a device with a MWA-compatible wallet installed

## Architecture

```
Unity C# Layer                    Android Native Layer (Kotlin)
┌─────────────────────┐          ┌─────────────────────────┐
│ MobileWalletAdapter │ ──JNI──> │ MWABridge.kt            │
│   (MonoBehaviour)   │          │   (Static methods)       │
├─────────────────────┤          ├─────────────────────────┤
│ MWATypes.cs         │          │ MWAClient.kt             │
│ IMWACache.cs        │          │   Uses:                  │
│ FileMWACache.cs     │          │   mobile-wallet-adapter  │
│ MWASession.cs       │          │   -clientlib-ktx:2.0.3   │
└─────────────────────┘          └─────────────────────────┘
```

Unity communicates with the Android native layer via `AndroidJavaClass` / JNI calls. The Kotlin bridge uses the official `mobile-wallet-adapter-clientlib-ktx` library and returns results as JSON strings. The C# side polls for completion using a coroutine-like pattern in `Update()`.

## Next Steps

- [Installation](installation.md) — Detailed setup instructions
- [API Reference](api-reference.md) — Complete API documentation
- [Cache Layer](cache-layer.md) — Customize authorization persistence
- [Migration from React Native](migration-from-rn.md) — Side-by-side API mapping
