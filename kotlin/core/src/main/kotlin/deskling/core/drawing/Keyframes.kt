package deskling.core.drawing

import kotlin.math.max

/** Eased keyframes, matching CSS `ease-in-out` timelines closely enough for doodles. Pure arithmetic. */
public object Keyframes {
    /**
     * [stops] are (progress 0...1, value) pairs in ascending order. Before the first stop the first value holds,
     * after the last the last; in between, smoothstep between the neighbors.
     */
    public fun value(
        t: Double,
        stops: List<Pair<Double, Double>>,
    ): Double {
        val first = stops.firstOrNull() ?: return 0.0
        val last = stops.last()
        if (t <= first.first) return first.second
        if (t >= last.first) return last.second
        for (k in 1 until stops.size) {
            if (t > stops[k].first) continue
            val a = stops[k - 1]
            val b = stops[k]
            val x = (t - a.first) / max(b.first - a.first, 0.0001)
            val eased = x * x * (3 - 2 * x)
            return a.second + (b.second - a.second) * eased
        }
        return last.second
    }
}
