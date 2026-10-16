package deskling.android

import android.content.Context
import android.content.Intent
import android.content.res.Configuration
import java.util.Locale

/**
 * What the tools pass when they launch the app: the screenshot mode (`DESKLING_SCREENSHOT_MODE`, the app's own
 * mode names, e.g. "simple" or "webcam"), the language and region to show it in (`DESKLING_LANG`, `DESKLING_REGION`,
 * as Deskling's `screens.py` passes them) and the screens to capture (`DESKLING_SCREENS`, comma-separated). Absent
 * extras mean a normal launch. The Android `LaunchContext`; the Mac reads the same values from environment variables.
 */
public data class LaunchContext(
    public val screenshotMode: String? = null,
    public val language: String? = null,
    public val region: String? = null,
    public val screens: List<String> = emptyList(),
) {
    public val isScreenshotRun: Boolean
        get() = screenshotMode != null

    /** The locale the extras ask for, or null for the device's. */
    public val locale: Locale?
        get() {
            val tag = language ?: return null
            return Locale.forLanguageTag(if (region != null && !tag.contains('-')) "$tag-$region" else tag)
        }

    /** [context] with [locale] applied to its configuration, or [context] itself when no language was asked for. */
    public fun localized(context: Context): Context {
        val locale = locale ?: return context
        Locale.setDefault(locale)
        val configuration = Configuration(context.resources.configuration)
        configuration.setLocale(locale)
        return context.createConfigurationContext(configuration)
    }

    public companion object {
        public const val EXTRA_SCREENSHOT_MODE: String = "DESKLING_SCREENSHOT_MODE"
        public const val EXTRA_LANG: String = "DESKLING_LANG"
        public const val EXTRA_REGION: String = "DESKLING_REGION"
        public const val EXTRA_SCREENS: String = "DESKLING_SCREENS"

        /** The context an [intent] carries; a normal launch when it carries none. */
        public fun from(intent: Intent?): LaunchContext {
            if (intent == null) return LaunchContext()
            val screens =
                intent
                    .getStringExtra(
                        EXTRA_SCREENS,
                    )?.split(',')
                    ?.map { it.trim() }
                    ?.filter { it.isNotEmpty() }
            return LaunchContext(
                screenshotMode = intent.getStringExtra(EXTRA_SCREENSHOT_MODE),
                language = intent.getStringExtra(EXTRA_LANG),
                region = intent.getStringExtra(EXTRA_REGION),
                screens = screens ?: emptyList(),
            )
        }
    }
}
