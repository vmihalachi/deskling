package laching.android

import android.app.Activity
import android.graphics.Bitmap
import android.os.Handler
import android.os.Looper
import android.view.Choreographer
import android.view.PixelCopy
import android.view.View
import android.view.ViewGroup
import kotlinx.coroutines.delay
import kotlinx.coroutines.suspendCancellableCoroutine
import java.io.File
import kotlin.coroutines.resume

/**
 * The Android `ScreenshotCapture`: when the app was launched with [LaunchContext.screens], [run] shows each screen
 * in light then dark (the app's [Screens] does the showing), copies the window through [PixelCopy] into
 * `filesDir/screens/<screen>-<theme>.png`, lists layout problems in `problems.txt` (content taller than the
 * viewport outside a scroll container, found here in the view tree; text with `hasVisualOverflow`, which the app's
 * Compose text reports through [report]), writes `done` and finishes the activity. Laching's `screens.py` launches
 * the app with the extras and pulls the folder.
 */
public class ScreenshotRun(
    private val activity: Activity,
    public val launch: LaunchContext,
) {
    /** The app's side: show a screen in a theme and return once its state is set; the run waits for the frame. */
    public fun interface Screens {
        public suspend fun show(
            screen: String,
            dark: Boolean,
        )
    }

    public val folder: File = File(activity.filesDir, "screens")
    private val problems = mutableListOf<String>()

    /** The shot being captured, `<screen>-<theme>`, for [report]; null outside a run. */
    public var current: String? = null
        private set

    /** The viewport's height in pixels, for the app's own overflow checks. */
    public val viewportHeight: Int
        get() = activity.window.decorView.height

    /** Notes a problem with the current shot, e.g. a Compose text whose layout `hasVisualOverflow`. */
    public fun report(problem: String) {
        problems += "${current ?: "-"}: $problem"
    }

    /** Captures every screen, writes `problems.txt` and `done`, then finishes [activity]. */
    public suspend fun run(
        screens: Screens,
        settleMillis: Long = 400,
    ) {
        folder.mkdirs()
        folder.listFiles()?.forEach { it.delete() }
        for (screen in launch.screens) {
            for (dark in listOf(false, true)) {
                val shot = "$screen-${if (dark) "dark" else "light"}"
                current = shot
                screens.show(screen, dark)
                awaitFrame()
                delay(settleMillis)
                awaitFrame()
                capture(File(folder, "$shot.png"))
                checkViewTree()
            }
        }
        current = null
        File(folder, "problems.txt").writeText(problems.joinToString("") { "$it\n" })
        File(folder, "done").writeText("")
        activity.finishAndRemoveTask()
    }

    private suspend fun awaitFrame() {
        suspendCancellableCoroutine { continuation ->
            Choreographer.getInstance().postFrameCallback { continuation.resume(Unit) }
        }
    }

    private suspend fun capture(file: File) {
        val view = activity.window.decorView
        if (view.width == 0 || view.height == 0) {
            report("the window has no size")
            return
        }
        val bitmap = Bitmap.createBitmap(view.width, view.height, Bitmap.Config.ARGB_8888)
        val code =
            suspendCancellableCoroutine { continuation ->
                PixelCopy.request(activity.window, bitmap, { continuation.resume(it) }, Handler(Looper.getMainLooper()))
            }
        if (code != PixelCopy.SUCCESS) {
            report("PixelCopy failed with $code")
            return
        }
        file.outputStream().use { bitmap.compress(Bitmap.CompressFormat.PNG, 100, it) }
    }

    /** Classic views whose bottom lies below the viewport with no scrolling ancestor; Compose reports its own. */
    private fun checkViewTree() {
        val root = activity.window.decorView
        val viewport = root.height
        val location = IntArray(2)

        fun walk(
            view: View,
            scrollable: Boolean,
        ) {
            if (view.visibility != View.VISIBLE) return
            val inScroll =
                scrollable || view.isScrollContainer || view.canScrollVertically(1) || view.canScrollVertically(-1)
            if (view is ViewGroup) {
                for (i in 0 until view.childCount) walk(view.getChildAt(i), inScroll)
                return
            }
            view.getLocationInWindow(location)
            val bottom = location[1] + view.height
            if (!inScroll && view.height > 0 && bottom > viewport) {
                report(
                    "${describe(view)} extends ${bottom - viewport} px below the viewport outside a scroll container",
                )
            }
        }
        walk(root, false)
    }

    private fun describe(view: View): String {
        val name = view.javaClass.simpleName
        val id =
            if (view.id !=
                View.NO_ID
            ) {
                runCatching { view.resources.getResourceEntryName(view.id) }.getOrNull()
            } else {
                null
            }
        return if (id != null) "$name #$id" else name
    }
}
