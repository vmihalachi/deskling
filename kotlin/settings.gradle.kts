// Deskling's Kotlin side: `:core` (pure JVM), `:android` (the platform services) and `:testing` (the mocks). Apps
// consume it with `includeBuild("…/deskling/kotlin")` and dependency substitution of `deskling:core`, `deskling:android`
// and `deskling:testing`; nothing is published to Maven, the apps pin the submodule's tag.
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

rootProject.name = "deskling"

include(":core", ":android", ":testing")
