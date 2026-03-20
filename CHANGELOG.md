# Changelog

## [1.0.0] - 2026-03-20

### Added
- Full MWA 2.0 API parity with React Native SDK
  - `Authorize()` with optional Sign In With Solana payload
  - `Deauthorize()` / `Disconnect()`
  - `Reconnect()` from cached authorization
  - `GetCapabilities()` to query wallet features and limits
  - `SignTransactions()` for offline signing
  - `SignAndSendTransactions()` with configurable send options
  - `SignMessages()` for arbitrary message signing
  - `CloneAuthorization()` for session sharing
- Extensible authorization cache via `IMWACache` interface
- Built-in `FileMWACache` for file-based persistence
- `MWASession` for session state management
- Complete C# type system (`MWATypes.cs`): `Account`, `AuthorizationResult`, `WalletCapabilities`, `DappIdentity`, `SendOptions`, `SignInPayload`, `SignInResult`
- C# events for all async operations
- Android native plugin (Kotlin) using `mobile-wallet-adapter-clientlib-ktx:2.0.3`
- Example app demonstrating all API methods
- Comprehensive documentation with API reference, cache guide, and React Native migration guide
- UPM package support (install via git URL)
