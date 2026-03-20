package com.solana.mwa.unity

import android.util.Base64
import android.util.Log
import com.solana.mobilewalletadapter.clientlib.ActivityResultSender
import com.unity3d.player.UnityPlayer

/**
 * Unity Android plugin bridge for Mobile Wallet Adapter 2.0.
 * Exposes static methods callable via JNI from Unity C#.
 *
 * Uses ActivityLifecycleCallbacks to create ActivityResultSender
 * during onCreate (before STARTED state, as required by the MWA library).
 */
object MWABridge {

    private const val TAG = "MWABridge"

    private val mwaClient = MWAClient()
    private var sender: ActivityResultSender? = null

    /** Called by MWAInitProvider during Activity.onCreate */
    fun setSender(s: ActivityResultSender) {
        sender = s
        Log.d(TAG, "ActivityResultSender set")
    }

    fun clearSender() {
        sender = null
        mwaClient.cancelAll()
    }

    @JvmStatic
    fun initialize() {
        // No-op: initialization is handled by MWAInitProvider ContentProvider
        Log.d(TAG, "initialize called, sender=${if (sender != null) "ready" else "null"}")
    }

    private fun getSender(): ActivityResultSender? {
        if (sender != null) return sender
        Log.e(TAG, "getSender: ActivityResultSender is null. MWAInitProvider may not have run.")
        mwaClient.setError(MWAErrorCode.NOT_INITIALIZED, "MWA not initialized. No wallet activity available.")
        return null
    }

    // ===================================================================
    // STATUS POLLING
    // ===================================================================

    @JvmStatic fun getStatus(): Int = mwaClient.status
    @JvmStatic fun getResultJson(): String = mwaClient.resultJson
    @JvmStatic fun getErrorMessage(): String = mwaClient.errorMessage
    @JvmStatic fun getErrorCode(): Int = mwaClient.errorCode
    @JvmStatic fun clearState() { mwaClient.clearState() }

    // ===================================================================
    // MWA 2.0 API METHODS
    // ===================================================================

    @JvmStatic
    fun authorize(
        identityUri: String, iconPath: String, identityName: String,
        chain: String, cachedAuthToken: String, signInPayloadJson: String
    ) {
        Log.d(TAG, "authorize: name=$identityName chain=$chain")
        val s = getSender() ?: return
        mwaClient.authorize(s, identityUri, iconPath, identityName, chain, cachedAuthToken, signInPayloadJson)
    }

    @JvmStatic
    fun deauthorize(identityUri: String, iconPath: String, identityName: String, chain: String, authToken: String) {
        Log.d(TAG, "deauthorize")
        val s = getSender() ?: return
        mwaClient.deauthorize(s, identityUri, iconPath, identityName, chain, authToken)
    }

    @JvmStatic
    fun getCapabilities(identityUri: String, iconPath: String, identityName: String, chain: String, authToken: String) {
        Log.d(TAG, "getCapabilities")
        val s = getSender() ?: return
        mwaClient.getCapabilities(s, identityUri, iconPath, identityName, chain, authToken)
    }

    @JvmStatic
    fun signTransactions(identityUri: String, iconPath: String, identityName: String, chain: String, authToken: String, payloadsB64: Array<String>) {
        Log.d(TAG, "signTransactions: count=${payloadsB64.size}")
        val s = getSender() ?: return
        val payloads = payloadsB64.map { Base64.decode(it, Base64.DEFAULT) }.toTypedArray()
        mwaClient.signTransactions(s, identityUri, iconPath, identityName, chain, authToken, payloads)
    }

    @JvmStatic
    fun signAndSendTransactions(identityUri: String, iconPath: String, identityName: String, chain: String, authToken: String, payloadsB64: Array<String>, optionsJson: String) {
        Log.d(TAG, "signAndSendTransactions: count=${payloadsB64.size}")
        val s = getSender() ?: return
        val payloads = payloadsB64.map { Base64.decode(it, Base64.DEFAULT) }.toTypedArray()
        mwaClient.signAndSendTransactions(s, identityUri, iconPath, identityName, chain, authToken, payloads, optionsJson)
    }

    @JvmStatic
    fun signMessages(identityUri: String, iconPath: String, identityName: String, chain: String, authToken: String, messagesB64: Array<String>, addressesB64: Array<String>) {
        Log.d(TAG, "signMessages: count=${messagesB64.size}")
        val s = getSender() ?: return
        val messages = messagesB64.map { Base64.decode(it, Base64.DEFAULT) }.toTypedArray()
        val addressBytes = addressesB64.map { Base64.decode(it, Base64.DEFAULT) }.toTypedArray()
        mwaClient.signMessages(s, identityUri, iconPath, identityName, chain, authToken, messages, addressBytes)
    }
}
