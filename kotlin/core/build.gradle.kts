// laching.core: the scheduler, the random source, localization keys and plural rules, the settings store, the purchase
// store and the drawing math. Pure JVM, so it tests on any machine and the Android module stays thin. The JVM setup
// (toolchain, explicit API, JUnit) is the root build's.
plugins {
    alias(libs.plugins.kotlin.jvm)
    alias(libs.plugins.kotlin.serialization)
}

// Laching.VERSION, written from gradle.properties' lachingVersion before each compile, so a release edits one string on
// the Kotlin side. A generated source dir of :core, no plugin.
val generateVersion by tasks.registering {
    val version = providers.gradleProperty("lachingVersion")
    val output = layout.buildDirectory.dir("generated/laching/kotlin")
    inputs.property("version", version)
    outputs.dir(output)
    doLast {
        val file = output.get().file("laching/core/Laching.kt").asFile
        file.parentFile.mkdirs()
        file.writeText(
            """
            |package laching.core
            |
            |/** The package's own version, matching the git tag and the Swift package. Generated from gradle.properties. */
            |public object Laching {
            |    public const val VERSION: String = "${version.get()}"
            |}
            |
            """.trimMargin(),
        )
    }
}

kotlin {
    sourceSets.named("main") { kotlin.srcDir(generateVersion) }
}

dependencies {
    api(libs.kotlinx.coroutines.core)
    implementation(libs.kotlinx.serialization.json)

    testImplementation(libs.kotlinx.coroutines.test)
    testImplementation(project(":testing"))
}
