# Installation

## Prerequisites

- Unity 2021.3 or later
- Android Build Support module installed
- Android device with a MWA-compatible wallet (Phantom, Solflare, etc.)

## Install via Unity Package Manager (Recommended)

1. Open your Unity project
2. Go to **Window > Package Manager**
3. Click the **+** button in the top-left corner
4. Select **Add package from git URL...**
5. Enter: `https://github.com/nicoeseworthy/Unity-MWA-SDK.git`
6. Click **Add**

The SDK will be imported as a UPM package under `Packages/Solana Mobile Wallet Adapter`.

## Manual Installation

1. Download or clone the repository
2. Copy the entire folder into your project's `Packages/` directory
3. Unity will automatically detect and import the package

## Android Build Settings

Configure your project for Android:

1. Go to **File > Build Settings**
2. Select **Android** and click **Switch Platform**
3. Click **Player Settings**
4. Under **Other Settings**:
   - Set **Minimum API Level** to **Android 7.0 (API level 24)**
   - Set **Target API Level** to **API level 34**
   - Set **Scripting Backend** to **IL2CPP** (recommended)
   - Ensure **Internet Access** is set to **Require**

## Building the Android Plugin (for contributors)

The pre-built `solana-mwa-bridge.aar` is included in `Runtime/Plugins/Android/`. If you need to rebuild it from source:

1. Navigate to the `Android~/` directory
2. Place Unity's `classes.jar` in `Android~/libs/` (found at `<Unity>/Editor/Data/PlaybackEngines/AndroidPlayer/Variations/il2cpp/Release/Classes/classes.jar`)
3. Run:
   ```bash
   cd Android~
   ./gradlew assembleRelease
   ```
4. Copy the output AAR from `build/outputs/aar/` to `Runtime/Plugins/Android/solana-mwa-bridge.aar`

## Importing the Example

1. Open **Window > Package Manager**
2. Find **Solana Mobile Wallet Adapter** in the package list
3. Expand the **Samples** section
4. Click **Import** next to **Basic Example**

The example scene will be imported into your `Assets/Samples/` directory.

## Verifying the Installation

1. Create a new scene
2. Add an empty GameObject
3. Add the following script:

```csharp
using UnityEngine;
using Solana.MWA;

public class MWATest : MonoBehaviour
{
    void Start()
    {
        var mwa = MobileWalletAdapter.Instance;
        mwa.Identity = new DappIdentity("Test App", "https://test.com", "icon.png");
        Debug.Log("MWA SDK loaded successfully!");
        Debug.Log($"Cache has auth: {mwa.AuthCache.HasAuthorization()}");
    }
}
```

4. Build and run on an Android device
5. Check logcat for "MWA SDK loaded successfully!"

## Troubleshooting

**"Android bridge not available" error:**
- Ensure you're running on a real Android device (not the Unity Editor or emulator)
- Verify the AAR file is present in `Runtime/Plugins/Android/`

**"No MWA-compatible wallet found" error:**
- Install a wallet that supports MWA 2.0 (Phantom, Solflare) on the device

**Build errors related to AndroidX:**
- Enable **Custom Main Gradle Template** in Player Settings > Publishing Settings
- Add `android.useAndroidX=true` to your `gradleTemplate.properties`
