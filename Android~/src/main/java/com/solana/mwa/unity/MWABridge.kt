package com.solana.mwa.unity

import android.app.Activity
import android.util.Base64
import android.util.Log
import com.unity3d.player.UnityPlayer

/**
 * Unity Android plugin bridge for Mobile Wallet Adapter 2.0.
 * Exposes static methods callable via JNI from Unity C#.
 *
 * All operations are async — Unity polls getStatus() to check completion.
 * Results are retrieved via getResultJson(), errors via getErrorMessage()/getErrorCode().
 */
object MWABridge {

    private const val TAG = "MWABridge"

    private val mwaClient = MWAClient()

    // ===================================================================
    // STATUS POLLING (called from Unity C# coroutine)
    // ===================================================================

    /** Returns 0=pending, 1=success, 2=error */
    @JvmStatic
    fun getStatus(): Int = mwaClient.status

    /** JSON string of the successful result. */
    @JvmStatic
    fun getResultJson(): String = mwaClient.resultJson

    /** Error message from last failed operation. */
    @JvmStatic
    fun getErrorMessage(): String = mwaClient.errorMessage

    /** MWA error code from last failed operation. */
    @JvmStatic
    fun getErrorCode(): Int = mwaClient.errorCode

    /** Reset state for next operation. */
    @JvmStatic
    fun clearState() {
        mwaClient.clearState()
    }

    private fun getActivity(): Activity? {
        return UnityPlayer.currentActivity
    }

    // ===================================================================
    // MWA 2.0 API METHODS
    // ===================================================================

    /**
     * Authorize dapp with wallet. If cachedAuthToken is non-empty, attempts reauthorization.
     * Supports optional Sign In With Solana payload.
     */
    @JvmStatic
    fun authorize(
        identityUri: String,
        iconPath: String,
        identityName: String,
        chain: String,
        cachedAuthToken: String,
        signInPayloadJson: String
    ) {
        Log.d(TAG, "authorize: name=$identityName chain=$chain cached=${cachedAuthToken.isNotEmpty()}")
        val activity = getActivity() ?: run {
            mwaClient.setError(-1, "No activity available")
            return
        }
        mwaClient.authorize(
            activity, identityUri, iconPath, identityName,
            chain, cachedAuthToken, signInPayloadJson
        )
    }

    /**
     * Deauthorize — revoke the given auth token.
     */
    @JvmStatic
    fun deauthorize(
        identityUri: String,
        iconPath: String,
        identityName: String,
        authToken: String
    ) {
        Log.d(TAG, "deauthorize")
        val activity = getActivity() ?: run {
            mwaClient.setError(-1, "No activity available")
            return
        }
        mwaClient.deauthorize(activity, identityUri, iconPath, identityName, authToken)
    }

    /**
     * Query wallet capabilities (supported methods, limits, features).
     */
    @JvmStatic
    fun getCapabilities(
        identityUri: String,
        iconPath: String,
        identityName: String,
        authToken: String
    ) {
        Log.d(TAG, "getCapabilities")
        val activity = getActivity() ?: run {
            mwaClient.setError(-1, "No activity available")
            return
        }
        mwaClient.getCapabilities(activity, identityUri, iconPath, identityName, authToken)
    }

    /**
     * Sign transactions without sending. Payloads are base64-encoded transaction bytes.
     */
    @JvmStatic
    fun signTransactions(
        identityUri: String,
        iconPath: String,
        identityName: String,
        authToken: String,
        payloadsB64: Array<String>
    ) {
        Log.d(TAG, "signTransactions: count=${payloadsB64.size}")
        val activity = getActivity() ?: run {
            mwaClient.setError(-1, "No activity available")
            return
        }
        val payloads = payloadsB64.map { Base64.decode(it, Base64.DEFAULT) }.toTypedArray()
        mwaClient.signTransactions(activity, identityUri, iconPath, identityName, authToken, payloads)
    }

    /**
     * Sign and send transactions. Wallet submits to the network.
     */
    @JvmStatic
    fun signAndSendTransactions(
        identityUri: String,
        iconPath: String,
        identityName: String,
        authToken: String,
        payloadsB64: Array<String>,
        optionsJson: String
    ) {
        Log.d(TAG, "signAndSendTransactions: count=${payloadsB64.size}")
        val activity = getActivity() ?: run {
            mwaClient.setError(-1, "No activity available")
            return
        }
        val payloads = payloadsB64.map { Base64.decode(it, Base64.DEFAULT) }.toTypedArray()
        mwaClient.signAndSendTransactions(
            activity, identityUri, iconPath, identityName, authToken, payloads, optionsJson
        )
    }

    /**
     * Sign arbitrary messages with the specified account addresses.
     */
    @JvmStatic
    fun signMessages(
        identityUri: String,
        iconPath: String,
        identityName: String,
        authToken: String,
        messagesB64: Array<String>,
        addressesB64: Array<String>
    ) {
        Log.d(TAG, "signMessages: count=${messagesB64.size}")
        val activity = getActivity() ?: run {
            mwaClient.setError(-1, "No activity available")
            return
        }
        val messages = messagesB64.map { Base64.decode(it, Base64.DEFAULT) }.toTypedArray()
        val addressBytes = addressesB64.map { Base64.decode(it, Base64.DEFAULT) }.toTypedArray()
        mwaClient.signMessages(
            activity, identityUri, iconPath, identityName, authToken, messages, addressBytes
        )
    }

    /**
     * Clone the current authorization for sharing with another session.
     */
    @JvmStatic
    fun cloneAuthorization(
        identityUri: String,
        iconPath: String,
        identityName: String,
        authToken: String
    ) {
        Log.d(TAG, "cloneAuthorization")
        val activity = getActivity() ?: run {
            mwaClient.setError(-1, "No activity available")
            return
        }
        mwaClient.cloneAuthorization(activity, identityUri, iconPath, identityName, authToken)
    }
}
