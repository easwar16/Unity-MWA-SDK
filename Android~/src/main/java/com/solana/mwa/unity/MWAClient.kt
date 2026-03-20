package com.solana.mwa.unity

import android.app.Activity
import android.net.Uri
import android.util.Base64
import android.util.Log
import com.solana.mobilewalletadapter.clientlib.ConnectionIdentity
import com.solana.mobilewalletadapter.clientlib.MobileWalletAdapter
import com.solana.mobilewalletadapter.clientlib.Blockchain
import com.solana.mobilewalletadapter.clientlib.TransactionResult
import kotlinx.coroutines.*
import org.json.JSONArray
import org.json.JSONObject

/**
 * Core MWA client that performs wallet adapter operations.
 * All methods are non-blocking — they launch coroutines and update status/result fields
 * that are polled from Unity C#.
 */
class MWAClient {

    companion object {
        private const val TAG = "MWAClient"
    }

    // --- Polling state (read from Unity C#) ---
    @Volatile var status: Int = 0          // 0=pending, 1=success, 2=error
    @Volatile var resultJson: String = ""
    @Volatile var errorMessage: String = ""
    @Volatile var errorCode: Int = 0

    private val scope = CoroutineScope(Dispatchers.IO + SupervisorJob())

    // Persistent auth token for session reuse
    private var cachedAuthToken: String? = null
    private var cachedPublicKey: ByteArray? = null

    fun clearState() {
        status = 0
        resultJson = ""
        errorMessage = ""
        errorCode = 0
    }

    fun setError(code: Int, message: String) {
        errorCode = code
        errorMessage = message
        status = 2
    }

    private fun setSuccess(json: String) {
        resultJson = json
        status = 1
    }

    private fun buildIdentity(uri: String, icon: String, name: String): ConnectionIdentity {
        return ConnectionIdentity(
            identityUri = Uri.parse(uri),
            iconUri = Uri.parse(icon),
            identityName = name
        )
    }

    private fun chainToBlockchain(chain: String): Blockchain {
        return when (chain) {
            "solana:devnet" -> Blockchain.SOLANA_DEVNET
            "solana:testnet" -> Blockchain.SOLANA_TESTNET
            else -> Blockchain.SOLANA_MAINNET
        }
    }

    // ===================================================================
    // AUTHORIZE
    // ===================================================================

    fun authorize(
        activity: Activity,
        identityUri: String,
        iconPath: String,
        identityName: String,
        chain: String,
        existingAuthToken: String,
        signInPayloadJson: String
    ) {
        clearState()
        scope.launch {
            try {
                val identity = buildIdentity(identityUri, iconPath, identityName)
                val blockchain = chainToBlockchain(chain)
                val walletAdapter = MobileWalletAdapter(connectionIdentity = identity)
                walletAdapter.blockchain = blockchain

                // Reuse cached auth token if available.
                if (existingAuthToken.isNotEmpty()) {
                    walletAdapter.authToken = existingAuthToken
                }

                val result = walletAdapter.connect(activity)

                when (result) {
                    is TransactionResult.Success -> {
                        val authResult = result.authResult
                        cachedAuthToken = authResult.authToken
                        cachedPublicKey = authResult.publicKey

                        val json = JSONObject().apply {
                            put("auth_token", authResult.authToken)
                            put("wallet_uri_base", authResult.walletUriBase ?: "")

                            val accounts = JSONArray()
                            val account = JSONObject().apply {
                                put("address", Base64.encodeToString(authResult.publicKey, Base64.NO_WRAP))
                                put("public_key", Base64.encodeToString(authResult.publicKey, Base64.NO_WRAP))
                                put("label", authResult.accountLabel ?: "")
                                put("icon", "")
                                put("chains", JSONArray().put(chain))
                                put("features", JSONArray())
                            }
                            accounts.put(account)
                            put("accounts", accounts)
                        }

                        setSuccess(json.toString())
                        Log.d(TAG, "authorize: success")
                    }
                    is TransactionResult.NoWalletFound -> {
                        setError(-1, "No MWA-compatible wallet found on device")
                    }
                    is TransactionResult.Failure -> {
                        setError(-1, "Authorization failed: ${result.e.message}")
                    }
                }
            } catch (e: Exception) {
                Log.e(TAG, "authorize error", e)
                setError(-1, "Authorization error: ${e.message}")
            }
        }
    }

    // ===================================================================
    // DEAUTHORIZE
    // ===================================================================

    fun deauthorize(
        activity: Activity,
        identityUri: String,
        iconPath: String,
        identityName: String,
        authToken: String
    ) {
        clearState()
        scope.launch {
            try {
                val identity = buildIdentity(identityUri, iconPath, identityName)
                val walletAdapter = MobileWalletAdapter(connectionIdentity = identity)
                walletAdapter.authToken = authToken

                val result = walletAdapter.transact(activity) { sender ->
                    deauthorize(authToken)
                }

                cachedAuthToken = null
                cachedPublicKey = null

                when (result) {
                    is TransactionResult.Success -> {
                        setSuccess("{}")
                        Log.d(TAG, "deauthorize: success")
                    }
                    is TransactionResult.Failure -> {
                        setSuccess("{}")
                        Log.w(TAG, "deauthorize: wallet reported failure, cleared locally")
                    }
                    else -> {
                        setSuccess("{}")
                    }
                }
            } catch (e: Exception) {
                Log.e(TAG, "deauthorize error", e)
                cachedAuthToken = null
                cachedPublicKey = null
                setSuccess("{}")
            }
        }
    }

    // ===================================================================
    // GET CAPABILITIES
    // ===================================================================

    fun getCapabilities(
        activity: Activity,
        identityUri: String,
        iconPath: String,
        identityName: String,
        authToken: String
    ) {
        clearState()
        scope.launch {
            try {
                val identity = buildIdentity(identityUri, iconPath, identityName)
                val walletAdapter = MobileWalletAdapter(connectionIdentity = identity)

                if (authToken.isNotEmpty()) {
                    walletAdapter.authToken = authToken
                } else if (cachedAuthToken != null) {
                    walletAdapter.authToken = cachedAuthToken
                }

                val result = walletAdapter.transact(activity) { sender ->
                    getCapabilities()
                }

                when (result) {
                    is TransactionResult.Success -> {
                        val caps = result.successPayload
                        val json = JSONObject().apply {
                            put("supports_clone_authorization", caps?.supportsCloneAuthorization ?: false)
                            put("supports_sign_and_send_transactions", caps?.supportsSignAndSendTransactions ?: false)
                            put("max_transactions_per_request", caps?.maxTransactionsPerSigningRequest ?: 0)
                            put("max_messages_per_request", caps?.maxMessagesPerSigningRequest ?: 0)

                            val versions = JSONArray()
                            caps?.supportedTransactionVersions?.forEach { versions.put(it) }
                            put("supported_transaction_versions", versions)

                            val features = JSONArray()
                            caps?.supportedFeatures?.forEach { features.put(it) }
                            put("features", features)
                        }
                        setSuccess(json.toString())
                        Log.d(TAG, "getCapabilities: success")
                    }
                    is TransactionResult.Failure -> {
                        setError(-1, "getCapabilities failed: ${result.e.message}")
                    }
                    else -> {
                        setError(-1, "getCapabilities: unexpected result")
                    }
                }
            } catch (e: Exception) {
                Log.e(TAG, "getCapabilities error", e)
                setError(-1, "getCapabilities error: ${e.message}")
            }
        }
    }

    // ===================================================================
    // SIGN TRANSACTIONS
    // ===================================================================

    fun signTransactions(
        activity: Activity,
        identityUri: String,
        iconPath: String,
        identityName: String,
        authToken: String,
        payloads: Array<ByteArray>
    ) {
        clearState()
        scope.launch {
            try {
                val identity = buildIdentity(identityUri, iconPath, identityName)
                val walletAdapter = MobileWalletAdapter(connectionIdentity = identity)
                walletAdapter.authToken = authToken

                val result = walletAdapter.transact(activity) { sender ->
                    signTransactions(payloads)
                }

                when (result) {
                    is TransactionResult.Success -> {
                        val signedPayloads = result.successPayload?.signedPayloads
                        val json = JSONObject().apply {
                            val arr = JSONArray()
                            signedPayloads?.forEach { payload ->
                                arr.put(Base64.encodeToString(payload, Base64.NO_WRAP))
                            }
                            put("signed_payloads", arr)
                        }
                        setSuccess(json.toString())
                        Log.d(TAG, "signTransactions: success, count=${signedPayloads?.size}")
                    }
                    is TransactionResult.Failure -> {
                        val msg = result.e.message ?: "Unknown error"
                        setError(-3, "signTransactions failed: $msg")
                    }
                    else -> {
                        setError(-3, "signTransactions: unexpected result")
                    }
                }
            } catch (e: Exception) {
                Log.e(TAG, "signTransactions error", e)
                setError(-3, "signTransactions error: ${e.message}")
            }
        }
    }

    // ===================================================================
    // SIGN AND SEND TRANSACTIONS
    // ===================================================================

    fun signAndSendTransactions(
        activity: Activity,
        identityUri: String,
        iconPath: String,
        identityName: String,
        authToken: String,
        payloads: Array<ByteArray>,
        optionsJson: String
    ) {
        clearState()
        scope.launch {
            try {
                val identity = buildIdentity(identityUri, iconPath, identityName)
                val walletAdapter = MobileWalletAdapter(connectionIdentity = identity)
                walletAdapter.authToken = authToken

                var minContextSlot: Int? = null
                var commitment: String? = null
                var skipPreflight: Boolean? = null
                var maxRetries: Int? = null

                if (optionsJson.isNotEmpty()) {
                    try {
                        val opts = JSONObject(optionsJson)
                        if (opts.has("min_context_slot")) minContextSlot = opts.getInt("min_context_slot")
                        if (opts.has("commitment")) commitment = opts.getString("commitment")
                        if (opts.has("skip_preflight")) skipPreflight = opts.getBoolean("skip_preflight")
                        if (opts.has("max_retries")) maxRetries = opts.getInt("max_retries")
                    } catch (e: Exception) {
                        Log.w(TAG, "Failed to parse send options: ${e.message}")
                    }
                }

                val result = walletAdapter.transact(activity) { sender ->
                    signAndSendTransactions(
                        transactions = payloads,
                        minContextSlot = minContextSlot,
                        commitment = commitment,
                        skipPreflight = skipPreflight ?: false,
                        maxRetries = maxRetries
                    )
                }

                when (result) {
                    is TransactionResult.Success -> {
                        val signatures = result.successPayload?.signatures
                        val json = JSONObject().apply {
                            val arr = JSONArray()
                            signatures?.forEach { sig ->
                                arr.put(Base64.encodeToString(sig, Base64.NO_WRAP))
                            }
                            put("signatures", arr)
                        }
                        setSuccess(json.toString())
                        Log.d(TAG, "signAndSendTransactions: success, sigs=${signatures?.size}")
                    }
                    is TransactionResult.Failure -> {
                        val msg = result.e.message ?: "Unknown error"
                        setError(-4, "signAndSendTransactions failed: $msg")
                    }
                    else -> {
                        setError(-4, "signAndSendTransactions: unexpected result")
                    }
                }
            } catch (e: Exception) {
                Log.e(TAG, "signAndSendTransactions error", e)
                setError(-4, "signAndSendTransactions error: ${e.message}")
            }
        }
    }

    // ===================================================================
    // SIGN MESSAGES
    // ===================================================================

    fun signMessages(
        activity: Activity,
        identityUri: String,
        iconPath: String,
        identityName: String,
        authToken: String,
        messages: Array<ByteArray>,
        addresses: Array<ByteArray>
    ) {
        clearState()
        scope.launch {
            try {
                val identity = buildIdentity(identityUri, iconPath, identityName)
                val walletAdapter = MobileWalletAdapter(connectionIdentity = identity)
                walletAdapter.authToken = authToken

                val result = walletAdapter.transact(activity) { sender ->
                    signMessagesDetached(messages, addresses)
                }

                when (result) {
                    is TransactionResult.Success -> {
                        val signed = result.successPayload
                        val json = JSONObject().apply {
                            val arr = JSONArray()
                            signed?.messages?.forEach { msg ->
                                msg.signatures.forEach { sig ->
                                    arr.put(Base64.encodeToString(sig, Base64.NO_WRAP))
                                }
                            }
                            put("signatures", arr)
                        }
                        setSuccess(json.toString())
                        Log.d(TAG, "signMessages: success")
                    }
                    is TransactionResult.Failure -> {
                        val msg = result.e.message ?: "Unknown error"
                        setError(-3, "signMessages failed: $msg")
                    }
                    else -> {
                        setError(-3, "signMessages: unexpected result")
                    }
                }
            } catch (e: Exception) {
                Log.e(TAG, "signMessages error", e)
                setError(-3, "signMessages error: ${e.message}")
            }
        }
    }

    // ===================================================================
    // CLONE AUTHORIZATION
    // ===================================================================

    fun cloneAuthorization(
        activity: Activity,
        identityUri: String,
        iconPath: String,
        identityName: String,
        authToken: String
    ) {
        clearState()
        scope.launch {
            try {
                val identity = buildIdentity(identityUri, iconPath, identityName)
                val walletAdapter = MobileWalletAdapter(connectionIdentity = identity)
                walletAdapter.authToken = authToken

                val result = walletAdapter.transact(activity) { sender ->
                    cloneAuthorization()
                }

                when (result) {
                    is TransactionResult.Success -> {
                        val clonedToken = result.successPayload
                        val json = JSONObject().apply {
                            put("auth_token", clonedToken?.authToken ?: "")
                        }
                        setSuccess(json.toString())
                        Log.d(TAG, "cloneAuthorization: success")
                    }
                    is TransactionResult.Failure -> {
                        setError(-5, "cloneAuthorization failed: ${result.e.message}")
                    }
                    else -> {
                        setError(-5, "cloneAuthorization: unexpected result")
                    }
                }
            } catch (e: Exception) {
                Log.e(TAG, "cloneAuthorization error", e)
                setError(-5, "cloneAuthorization error: ${e.message}")
            }
        }
    }
}
