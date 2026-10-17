package laching.core.drawing

import kotlin.math.hypot
import kotlin.math.sin

/**
 * A smooth, stable hand-drawn wobble for sampled polylines, the way an SVG `feTurbulence` + `feDisplacementMap`
 * filter roughens strokes. Pure arithmetic; the same seed always gives the same line, so animated figures keep their
 * character instead of shimmering.
 */
public object Wobble {
    /** Smooth pseudo-noise in roughly -1...1, stable for a given seed. */
    public fun noise(
        s: Double,
        seed: Int,
    ): Double {
        val k = seed.toDouble()
        return sin(s * 0.19 + k * 1.7) * 0.55 + sin(s * 0.47 + k * 2.9) * 0.3 + sin(s * 1.03 + k * 0.7) * 0.15
    }

    /** Nudges each point by [amount] along the polyline's arc length, so the wobble travels with the line when it moves. */
    public fun apply(
        points: List<Point>,
        seed: Int,
        amount: Double,
    ): List<Point> {
        if (amount <= 0 || points.size < 2) return points
        var s = 0.0
        val out = ArrayList<Point>(points.size)
        for ((i, p) in points.withIndex()) {
            if (i > 0) s += hypot(p.x - points[i - 1].x, p.y - points[i - 1].y)
            out += Point(p.x + noise(s, seed) * amount, p.y + noise(s, seed + 17) * amount)
        }
        return out
    }
}
