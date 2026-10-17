package laching.android

import android.app.AlarmManager
import android.app.PendingIntent
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.provider.Settings
import java.time.Instant

/**
 * The one alarm that wakes the app for the next reminder. Scheduling a date replaces the previous alarm (the same
 * PendingIntent), so there is never more than one. Exact when the user allowed exact alarms
 * (`setExactAndAllowWhileIdle`), otherwise inexact (`setAndAllowWhileIdle`, which Android may delay by minutes);
 * the app shows [requestExactIntent] in its settings when [exactAllowed] is false. Alarms don't survive a reboot:
 * the app's boot receiver schedules the next one again. Never asks for `USE_EXACT_ALARM`.
 */
public class ReminderAlarms(
    private val context: Context,
    private val receiver: Class<out BroadcastReceiver>,
    private val requestCode: Int = 1,
) {
    private val manager: AlarmManager = context.getSystemService(AlarmManager::class.java)

    /** Whether exact alarms may be scheduled: always below Android 12, else what the user allowed. */
    public val exactAllowed: Boolean
        get() = Build.VERSION.SDK_INT < Build.VERSION_CODES.S || manager.canScheduleExactAlarms()

    /** Wakes [receiver] at [date] with [extras], replacing any earlier alarm. */
    public fun schedule(
        date: Instant,
        extras: Bundle? = null,
    ) {
        val intent = pendingIntent(extras)
        manager.cancel(intent)
        val millis = date.toEpochMilli()
        if (exactAllowed) {
            manager.setExactAndAllowWhileIdle(AlarmManager.RTC_WAKEUP, millis, intent)
        } else {
            manager.setAndAllowWhileIdle(AlarmManager.RTC_WAKEUP, millis, intent)
        }
    }

    public fun cancel() {
        manager.cancel(pendingIntent(null))
    }

    /**
     * The system page where the user allows exact alarms for this app (Android 12+), to open from the app's settings
     * when [exactAllowed] is false; null below Android 12, where nothing needs allowing.
     */
    public fun requestExactIntent(): Intent? {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.S) return null
        return Intent(Settings.ACTION_REQUEST_SCHEDULE_EXACT_ALARM, Uri.parse("package:${context.packageName}"))
    }

    private fun pendingIntent(extras: Bundle?): PendingIntent {
        val intent = Intent(context, receiver).setAction(ACTION_REMIND)
        if (extras != null) intent.putExtras(extras)
        return PendingIntent.getBroadcast(
            context,
            requestCode,
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE,
        )
    }

    public companion object {
        /** The action of the broadcast the alarm sends. */
        public const val ACTION_REMIND: String = "laching.android.REMIND"
    }
}
