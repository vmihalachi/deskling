// laching.testing: the mocks (clock, idle, busy, random, store backend) and the conformance vector loader, as
// Laching.Testing on .NET. For the apps' test source sets; JVM only. The JVM setup (toolchain, explicit API, JUnit) is
// the root build's.
plugins {
    alias(libs.plugins.kotlin.jvm)
}

dependencies {
    api(project(":core"))
    api(libs.kotlinx.serialization.json)
}
