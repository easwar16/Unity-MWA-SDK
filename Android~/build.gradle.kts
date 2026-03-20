plugins {
    id("com.android.library")
    id("org.jetbrains.kotlin.android")
}

val pluginName = "SolanaMWA"
val pluginPackageName = "com.solana.mwa.unity"

android {
    namespace = pluginPackageName
    compileSdk = 34

    defaultConfig {
        minSdk = 24
        targetSdk = 34
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = "17"
    }

    buildFeatures {
        compose = true
    }

    composeOptions {
        kotlinCompilerExtensionVersion = "1.5.4"
    }
}

dependencies {
    // Unity classes.jar — place Unity's classes.jar in Android~/libs/
    compileOnly(fileTree(mapOf("dir" to "libs", "include" to listOf("*.jar", "*.aar"))))

    // MWA 2.0
    implementation("com.solanamobile:mobile-wallet-adapter-clientlib-ktx:2.0.3")

    // Solana RPC
    implementation("com.solanamobile:rpc-core:0.2.8")
    implementation("com.solanamobile:rpc-solana:0.2.8")
    implementation("com.solanamobile:rpc-ktordriver:0.2.8")

    // Compose (required by MWA clientlib)
    implementation("androidx.compose.ui:ui:1.5.4")
    implementation("androidx.compose.material:material:1.5.4")
    implementation("androidx.activity:activity-compose:1.8.2")
    implementation("androidx.lifecycle:lifecycle-runtime-ktx:2.7.0")

    // Coroutines
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.7.3")

    // JSON
    implementation("org.json:json:20231013")
}

// Task to copy all runtime dependencies to a folder for Unity
tasks.register<Copy>("copyDepsToUnity") {
    val unityPluginsDir = file("../Runtime/Plugins/Android")
    from(configurations.named("releaseRuntimeClasspath").get().resolve())
    into(unityPluginsDir)
    doFirst { unityPluginsDir.mkdirs() }
}
