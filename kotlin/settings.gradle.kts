// Laching's Kotlin side: `:core` (pure JVM), `:android` (the platform services) and `:testing` (the mocks). Apps
// consume it with `includeBuild("…/laching/kotlin")` and dependency substitution of `laching:core`, `laching:android`
// and `laching:testing`; nothing is published to Maven, the apps pin the submodule's tag.
pluginManagement {
    repositories {
        google()
        mavenCentral()
        gradlePluginPortal()
    }
}

dependencyResolutionManagement {
    repositories {
        google()
        mavenCentral()
    }
}

rootProject.name = "laching"

include(":core", ":android", ":testing")
