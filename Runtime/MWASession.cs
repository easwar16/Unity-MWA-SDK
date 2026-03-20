using System;
using UnityEngine;

namespace Solana.MWA
{
    /// <summary>
    /// Manages session state for a wallet adapter connection.
    /// Tracks connection state, current authorization, and capabilities.
    /// </summary>
    public class MWASession
    {
        /// <summary>
        /// Current connection state.
        /// </summary>
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

        /// <summary>
        /// Current authorization result (populated after authorize/reauthorize).
        /// </summary>
        public AuthorizationResult CurrentAuth { get; private set; }

        /// <summary>
        /// Last queried wallet capabilities.
        /// </summary>
        public WalletCapabilities Capabilities { get; set; }

        /// <summary>
        /// Fired when the connection state changes.
        /// </summary>
        public event Action<ConnectionState> OnStateChanged;

        /// <summary>
        /// Whether we have a valid authorization.
        /// </summary>
        public bool IsAuthorized => CurrentAuth != null && !string.IsNullOrEmpty(CurrentAuth.AuthToken);

        /// <summary>
        /// Whether we are connected (authorized with active session).
        /// </summary>
        public bool IsConnected => State == ConnectionState.Connected && IsAuthorized;

        /// <summary>
        /// Set the connection state, firing the event if changed.
        /// </summary>
        public void SetState(ConnectionState newState)
        {
            if (State != newState)
            {
                State = newState;
                OnStateChanged?.Invoke(State);
            }
        }

        /// <summary>
        /// Set the authorization result.
        /// </summary>
        public void SetAuth(AuthorizationResult auth)
        {
            CurrentAuth = auth;
        }

        /// <summary>
        /// Clear authorization and reset to disconnected state.
        /// </summary>
        public void ClearAuth()
        {
            CurrentAuth = null;
            SetState(ConnectionState.Disconnected);
        }

        /// <summary>
        /// Get the primary authorized account, or null.
        /// </summary>
        public Account GetAccount()
        {
            if (CurrentAuth != null && CurrentAuth.Accounts != null && CurrentAuth.Accounts.Length > 0)
                return CurrentAuth.Accounts[0];
            return null;
        }

        /// <summary>
        /// Get all authorized accounts.
        /// </summary>
        public Account[] GetAccounts()
        {
            if (CurrentAuth != null && CurrentAuth.Accounts != null)
                return CurrentAuth.Accounts;
            return new Account[0];
        }

        /// <summary>
        /// Get the public key of the primary authorized account.
        /// </summary>
        public byte[] GetPublicKey()
        {
            var account = GetAccount();
            return account?.GetPublicKey();
        }
    }
}
