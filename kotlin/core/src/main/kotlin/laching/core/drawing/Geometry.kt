package laching.core.drawing

import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.max
import kotlin.math.sin

/** A point in view-box coordinates (y down, as on screen). */
public data class Point(
    public val x: Double,
    public val y: Double,
) {
    public companion object {
        public val ZERO: Point = Point(0.0, 0.0)
    }
}

/** [this] turned by [degrees] about [about], for turning a limb or a doodle in view-box coordinates. */
public fun Point.rotated(
    degrees: Double,
    about: Point,
): Point {
    val radians = degrees * PI / 180
    val dx = x - about.x
    val dy = y - about.y
    return Point(about.x + dx * cos(radians) - dy * sin(radians), about.y + dx * sin(radians) + dy * cos(radians))
}

/**
 * Sampled polylines for a circle, matching [SvgPath.sample]'s output shape so wobble and dashes apply the same way.
 * Starts at twelve o'clock and closes on the first point. Pure geometry.
 */
public object Polyline {
    public fun circle(
        center: Point,
        radius: Double,
        minimumPoints: Int = 24,
    ): List<Point> {
        val n = max(minimumPoints, (radius * 2.4).toInt())
        return (0..n).map { i ->
            val a = i.toDouble() / n * 2 * PI - PI / 2
            Point(center.x + radius * cos(a), center.y + radius * sin(a))
        }
    }
}
