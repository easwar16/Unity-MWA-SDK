namespace Solana.MWA
{
    /// <summary>
    /// Interface for MWA authorization cache.
    /// Implement this interface to create custom cache backends (PlayerPrefs, SQLite, cloud, etc).
    /// </summary>
    public interface IMWACache
    {
        /// <summary>
        /// Retrieve the cached authorization. Returns null if no cache exists.
        /// </summary>
        AuthorizationResult GetAuthorization();

        /// <summary>
        /// Store an authorization result.
        /// </summary>
        void SetAuthorization(AuthorizationResult auth);

        /// <summary>
        /// Clear the cached authorization (on deauthorize/disconnect).
        /// </summary>
        void Clear();

        /// <summary>
        /// Check if a cached authorization exists with a valid auth token.
        /// </summary>
        bool HasAuthorization();
    }
}
