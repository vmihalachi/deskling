// laching.core: the scheduler, the random source, localization keys and plural rules, the settings store, the purchase
// store and the drawing math. Pure JVM, so it tests on any machine and the Android module stays thin. The JVM setup
// (toolchain, explicit API, JUnit) is the root build's.
plugins {
    alias(libs.plugins.kotlin.jvm)
    alias(libs.plugins.kotlin.serialization)
}

dependencies {
    api(libs.kotlinx.coroutines.core)
    implementation(libs.kotlinx.serialization.json)

    testImplementation(libs.kotlinx.coroutines.test)
    testImplementation(project(":testing"))
}
