package com.solana.mwa.unity

import android.app.Activity
import android.app.Application
import android.content.ContentProvider
import android.content.ContentValues
import android.database.Cursor
import android.net.Uri
import android.os.Bundle
import android.util.Log
import androidx.activity.ComponentActivity
import com.solana.mobilewalletadapter.clientlib.ActivityResultSender

/**
 * ContentProvider that initializes early during app startup to register
 * ActivityLifecycleCallbacks. This ensures we can create ActivityResultSender
 * during Activity.onCreate, before it reaches STARTED state.
 */
class MWAInitProvider : ContentProvider() {

    companion object {
        private const val TAG = "MWAInitProvider"
    }

    override fun onCreate(): Boolean {
        val app = context?.applicationContext as? Application ?: return true
        Log.d(TAG, "Registering ActivityLifecycleCallbacks")

        app.registerActivityLifecycleCallbacks(object : Application.ActivityLifecycleCallbacks {
            override fun onActivityCreated(activity: Activity, savedInstanceState: Bundle?) {
                if (activity is ComponentActivity) {
                    try {
                        val sender = ActivityResultSender(activity)
                        MWABridge.setSender(sender)
                        Log.d(TAG, "ActivityResultSender created for ${activity.javaClass.name}")
                    } catch (e: Exception) {
                        Log.e(TAG, "Failed to create ActivityResultSender", e)
                    }
                }
            }
            override fun onActivityStarted(activity: Activity) {}
            override fun onActivityResumed(activity: Activity) {}
            override fun onActivityPaused(activity: Activity) {}
            override fun onActivityStopped(activity: Activity) {}
            override fun onActivitySaveInstanceState(activity: Activity, outState: Bundle) {}
            override fun onActivityDestroyed(activity: Activity) {
                MWABridge.clearSender()
            }
        })
        return true
    }

    override fun query(uri: Uri, projection: Array<out String>?, selection: String?, selectionArgs: Array<out String>?, sortOrder: String?): Cursor? = null
    override fun getType(uri: Uri): String? = null
    override fun insert(uri: Uri, values: ContentValues?): Uri? = null
    override fun delete(uri: Uri, selection: String?, selectionArgs: Array<out String>?): Int = 0
    override fun update(uri: Uri, values: ContentValues?, selection: String?, selectionArgs: Array<out String>?): Int = 0
}
