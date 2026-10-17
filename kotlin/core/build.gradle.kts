// laching.core: the scheduler, the random source, localization keys and plural rules, the settings store, the purchase
// store and the drawing math. Pure JVM, so it tests on any machine and the Android module stays thin.
plugins {
    alias(libs.plugins.kotlin.jvm)
    alias(libs.plugins.kotlin.serialization)
}

kotlin {
    jvmToolchain(17)
    explicitApi()
}

dependencies {
    api(libs.kotlinx.coroutines.core)
    implementation(libs.kotlinx.serialization.json)

    testImplementation(platform(libs.junit.bom))
    testImplementation(kotlin("test"))
    testImplementation(libs.kotlinx.coroutines.test)
    testImplementation(project(":testing"))
    testRuntimeOnly(libs.junit.platform.launcher)
}

tasks.test {
    useJUnitPlatform()
    // The conformance tests walk up from the working directory to `conformance/`; keep the module folder as cwd.
    workingDir = projectDir
}
