using System;
using UnityEngine;

namespace Solana.MWA
{
    /// <summary>
    /// Blockchain cluster identifiers.
    /// </summary>
    public enum Cluster
    {
        Devnet = 0,
        Mainnet = 1,
        Testnet = 2
    }

    /// <summary>
    /// MWA protocol error codes.
    /// </summary>
    public enum MWAErrorCode
    {
        AuthorizationFailed = -1,
        InvalidPayloads = -2,
        NotSigned = -3,
        NotSubmitted = -4,
        NotCloned = -5,
        TooManyPayloads = -6,
        ClusterNotSupported = -7,
        AttestOriginAndroid = -100
    }

    /// <summary>
    /// Connection states for the wallet adapter.
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Signing,
        Deauthorizing
    }

    /// <summary>
    /// Account information returned from authorization.
    /// </summary>
    [Serializable]
    public class Account
    {
        public string Address;
        public string PublicKeyBase64;
        public string Label;
        public string Icon;
        public string[] Chains;
        public string[] Features;

        /// <summary>
        /// Gets the public key as a byte array (decoded from base64).
        /// </summary>
        public byte[] GetPublicKey()
        {
            if (string.IsNullOrEmpty(PublicKeyBase64))
                return null;
            return Convert.FromBase64String(PublicKeyBase64);
        }
    }

    /// <summary>
    /// Authorization result returned from authorize/reauthorize.
    /// </summary>
    [Serializable]
    public class AuthorizationResult
    {
        public Account[] Accounts;
        public string AuthToken;
        public string WalletUriBase;
        public SignInResult SignInResult;
    }

    /// <summary>
    /// Wallet capabilities returned from GetCapabilities.
    /// </summary>
    [Serializable]
    public class WalletCapabilities
    {
        public bool SupportsCloneAuthorization;
        public bool SupportsSignAndSend;
        public int MaxTransactions;
        public int MaxMessages;
        public string[] SupportedVersions;
        public string[] Features;
    }

    /// <summary>
    /// Dapp identity presented to wallet during authorization.
    /// </summary>
    [Serializable]
    public class DappIdentity
    {
        public string Name = "Unity dApp";
        public string Uri = "https://solana.com";
        public string Icon = "favicon.ico";

        public DappIdentity() { }

        public DappIdentity(string name, string uri, string icon)
        {
            Name = name;
            Uri = uri;
            Icon = icon;
        }
    }

    /// <summary>
    /// Options for sign and send transactions.
    /// </summary>
    [Serializable]
    public class SendOptions
    {
        public string Commitment = "confirmed";
        public int MinContextSlot = -1;
        public bool SkipPreflight = false;
        public int MaxRetries = -1;

        public string ToJson()
        {
            var json = "{";
            var parts = new System.Collections.Generic.List<string>();

            parts.Add($"\"commitment\":\"{Commitment}\"");
            parts.Add($"\"skip_preflight\":{(SkipPreflight ? "true" : "false")}");

            if (MinContextSlot >= 0)
                parts.Add($"\"min_context_slot\":{MinContextSlot}");
            if (MaxRetries >= 0)
                parts.Add($"\"max_retries\":{MaxRetries}");

            json += string.Join(",", parts) + "}";
            return json;
        }
    }

    /// <summary>
    /// Sign In With Solana (SIWS) payload.
    /// </summary>
    [Serializable]
    public class SignInPayload
    {
        public string Domain;
        public string Address;
        public string Statement;
        public string Uri;
        public string Version;
        public string ChainId;
        public string Nonce;
        public string IssuedAt;
        public string ExpirationTime;
        public string NotBefore;
        public string RequestId;
        public string[] Resources;

        public string ToJson()
        {
            var parts = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrEmpty(Domain)) parts.Add($"\"domain\":\"{Domain}\"");
            if (!string.IsNullOrEmpty(Address)) parts.Add($"\"address\":\"{Address}\"");
            if (!string.IsNullOrEmpty(Statement)) parts.Add($"\"statement\":\"{Statement}\"");
            if (!string.IsNullOrEmpty(Uri)) parts.Add($"\"uri\":\"{Uri}\"");
            if (!string.IsNullOrEmpty(Version)) parts.Add($"\"version\":\"{Version}\"");
            if (!string.IsNullOrEmpty(ChainId)) parts.Add($"\"chain_id\":\"{ChainId}\"");
            if (!string.IsNullOrEmpty(Nonce)) parts.Add($"\"nonce\":\"{Nonce}\"");
            if (!string.IsNullOrEmpty(IssuedAt)) parts.Add($"\"issued_at\":\"{IssuedAt}\"");
            if (!string.IsNullOrEmpty(ExpirationTime)) parts.Add($"\"expiration_time\":\"{ExpirationTime}\"");
            if (!string.IsNullOrEmpty(NotBefore)) parts.Add($"\"not_before\":\"{NotBefore}\"");
            if (!string.IsNullOrEmpty(RequestId)) parts.Add($"\"request_id\":\"{RequestId}\"");

            if (Resources != null && Resources.Length > 0)
            {
                var res = new string[Resources.Length];
                for (int i = 0; i < Resources.Length; i++)
                    res[i] = $"\"{Resources[i]}\"";
                parts.Add($"\"resources\":[{string.Join(",", res)}]");
            }

            return "{" + string.Join(",", parts) + "}";
        }
    }

    /// <summary>
    /// Result from Sign In With Solana.
    /// </summary>
    [Serializable]
    public class SignInResult
    {
        public string Address;
        public string SignedMessageBase64;
        public string SignatureBase64;
        public string SignatureType;

        public byte[] GetSignedMessage()
        {
            if (string.IsNullOrEmpty(SignedMessageBase64)) return null;
            return Convert.FromBase64String(SignedMessageBase64);
        }

        public byte[] GetSignature()
        {
            if (string.IsNullOrEmpty(SignatureBase64)) return null;
            return Convert.FromBase64String(SignatureBase64);
        }
    }

    /// <summary>
    /// Utility methods for cluster/chain conversion.
    /// </summary>
    public static class ClusterUtil
    {
        public static string ClusterToChain(Cluster cluster)
        {
            switch (cluster)
            {
                case Cluster.Devnet: return "solana:devnet";
                case Cluster.Testnet: return "solana:testnet";
                case Cluster.Mainnet: return "solana:mainnet";
                default: return "solana:mainnet";
            }
        }

        public static Cluster ChainToCluster(string chain)
        {
            switch (chain)
            {
                case "solana:devnet": return Cluster.Devnet;
                case "solana:testnet": return Cluster.Testnet;
                case "solana:mainnet": return Cluster.Mainnet;
                default: return Cluster.Mainnet;
            }
        }
    }
}
