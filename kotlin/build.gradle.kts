import org.jetbrains.kotlin.gradle.dsl.KotlinJvmProjectExtension

// Plugins are declared here once (apply false) so every module resolves the same versions from gradle/libs.versions.toml.
plugins {
    alias(libs.plugins.kotlin.jvm) apply false
    alias(libs.plugins.kotlin.serialization) apply false
    alias(libs.plugins.android.library) apply false
}

allprojects {
    group = "laching"
    version = providers.gradleProperty("lachingVersion").get()
}

// The JVM modules (:core, :testing) share one setup: JDK 17, explicit API, JUnit 5 tests run from the module folder
// (the conformance tests walk up from the working directory to `conformance/`). :android keeps its own block: AGP's
// built-in Kotlin takes `-Xexplicit-api=strict` through a different extension.
subprojects {
    plugins.withId("org.jetbrains.kotlin.jvm") {
        configure<KotlinJvmProjectExtension> {
            jvmToolchain(17)
            explicitApi()
        }
        dependencies {
            "testImplementation"(platform(libs.junit.bom))
            "testImplementation"(kotlin("test"))
            "testRuntimeOnly"(libs.junit.platform.launcher)
        }
        tasks.withType<Test>().configureEach {
            useJUnitPlatform()
            workingDir = projectDir
        }
    }
}
