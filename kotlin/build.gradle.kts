// Plugins are declared here once (apply false) so every module resolves the same versions from gradle/libs.versions.toml.
plugins {
    alias(libs.plugins.kotlin.jvm) apply false
    alias(libs.plugins.kotlin.serialization) apply false
    alias(libs.plugins.android.library) apply false
}

allprojects {
    group = "deskling"
    version = providers.gradleProperty("desklingVersion").get()
}
