package laching.testing

import laching.core.scheduling.BusyReason
import java.time.Instant
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter

/** The conformance vectors' spellings: ISO dates in the vector's zone, IANA zones, busy reason names. */
public object ConformanceSupport {
    private val seconds = DateTimeFormatter.ofPattern("yyyy-MM-dd'T'HH:mm:ss")
    private val millis = DateTimeFormatter.ofPattern("yyyy-MM-dd'T'HH:mm:ss.SSS")

    /** `2026-09-28T10:00:00+03:00` or `…Z`, with milliseconds only when the vector has them. */
    public fun date(text: String): Instant = OffsetDateTime.parse(text).toInstant()

    public fun timeZone(id: String): ZoneId = ZoneId.of(id)

    /**
     * [date] written as the vectors write it: seconds, `.fff` only when there is a fraction, the offset in [zone]
     * (`Z` for zero).
     */
    public fun iso(
        date: Instant,
        zone: ZoneId,
    ): String {
        val local = date.atZone(zone)
        val formatter = if (local.nano == 0) seconds else millis
        val offset = local.offset
        val suffix = if (offset == ZoneOffset.UTC) "Z" else offset.id
        return formatter.format(local) + suffix
    }

    public fun busyName(reason: BusyReason): String =
        when (reason) {
            BusyReason.CALL -> "call"
            BusyReason.FULL_SCREEN -> "fullScreen"
        }

    public fun parseBusy(name: String): BusyReason =
        when (name) {
            "call" -> BusyReason.CALL
            "fullScreen" -> BusyReason.FULL_SCREEN
            else -> throw IllegalArgumentException("unknown busy reason $name")
        }
}
