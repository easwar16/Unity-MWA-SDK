using System;
using System.IO;
using UnityEngine;

namespace Solana.MWA
{
    /// <summary>
    /// File-based authorization cache using Application.persistentDataPath.
    /// Persists auth tokens across app restarts.
    /// </summary>
    public class FileMWACache : IMWACache
    {
        private const string CacheFileName = "mwa_auth_cache.json";

        private string CachePath => Path.Combine(Application.persistentDataPath, CacheFileName);

        public AuthorizationResult GetAuthorization()
        {
            if (!File.Exists(CachePath))
                return null;

            try
            {
                string json = File.ReadAllText(CachePath);
                var data = JsonUtility.FromJson<CacheData>(json);
                if (data == null || string.IsNullOrEmpty(data.auth_token))
                    return null;

                var result = new AuthorizationResult
                {
                    AuthToken = data.auth_token,
                    WalletUriBase = data.wallet_uri_base,
                    Accounts = ParseAccounts(data.accounts)
                };
                return result;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"FileMWACache: Failed to read cache: {e.Message}");
                return null;
            }
        }

        public void SetAuthorization(AuthorizationResult auth)
        {
            if (auth == null)
            {
                Clear();
                return;
            }

            try
            {
                var data = new CacheData
                {
                    auth_token = auth.AuthToken,
                    wallet_uri_base = auth.WalletUriBase,
                    accounts = SerializeAccounts(auth.Accounts)
                };
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(CachePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"FileMWACache: Failed to write cache: {e.Message}");
            }
        }

        public void Clear()
        {
            if (File.Exists(CachePath))
            {
                try
                {
                    File.Delete(CachePath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"FileMWACache: Failed to clear cache: {e.Message}");
                }
            }
        }

        public bool HasAuthorization()
        {
            var auth = GetAuthorization();
            return auth != null && !string.IsNullOrEmpty(auth.AuthToken);
        }

        private CacheAccount[] SerializeAccounts(Account[] accounts)
        {
            if (accounts == null) return new CacheAccount[0];
            var result = new CacheAccount[accounts.Length];
            for (int i = 0; i < accounts.Length; i++)
            {
                result[i] = new CacheAccount
                {
                    address = accounts[i].Address,
                    public_key = accounts[i].PublicKeyBase64,
                    label = accounts[i].Label,
                    icon = accounts[i].Icon,
                    chains = accounts[i].Chains,
                    features = accounts[i].Features
                };
            }
            return result;
        }

        private Account[] ParseAccounts(CacheAccount[] cacheAccounts)
        {
            if (cacheAccounts == null) return new Account[0];
            var result = new Account[cacheAccounts.Length];
            for (int i = 0; i < cacheAccounts.Length; i++)
            {
                result[i] = new Account
                {
                    Address = cacheAccounts[i].address,
                    PublicKeyBase64 = cacheAccounts[i].public_key,
                    Label = cacheAccounts[i].label,
                    Icon = cacheAccounts[i].icon,
                    Chains = cacheAccounts[i].chains,
                    Features = cacheAccounts[i].features
                };
            }
            return result;
        }

        [Serializable]
        private class CacheData
        {
            public string auth_token;
            public string wallet_uri_base;
            public CacheAccount[] accounts;
        }

        [Serializable]
        private class CacheAccount
        {
            public string address;
            public string public_key;
            public string label;
            public string icon;
            public string[] chains;
            public string[] features;
        }
    }
}
