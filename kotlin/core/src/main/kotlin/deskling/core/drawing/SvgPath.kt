package deskling.core.drawing

import kotlin.math.PI
import kotlin.math.abs
import kotlin.math.acos
import kotlin.math.cos
import kotlin.math.hypot
import kotlin.math.max
import kotlin.math.min
import kotlin.math.sin
import kotlin.math.sqrt

/**
 * Samples SVG path data into dense polylines, one per subpath, for hand-drawn rendering (wobble, dashes, shadows
 * need points, not curves). Reads `M L H V Q T C S A Z` in absolute and relative form; curves and arcs are flattened
 * to segments about `step` units long. Results are cached per path string behind a lock, so any thread may call it.
 * Pure geometry: no views, no drawing. The same sampling as `DesklingShell`'s `SVGPath`.
 */
public object SvgPath {
    private sealed interface Token {
        data class Cmd(
            val char: Char,
        ) : Token

        data class Num(
            val value: Double,
        ) : Token
    }

    private val cache = HashMap<String, List<List<Point>>>()
    private val lock = Any()

    public fun sample(
        d: String,
        step: Double = 2.0,
    ): List<List<Point>> {
        val key = if (step == 2.0) d else "$step|$d"
        synchronized(lock) { cache[key] }?.let { return it }
        val result = Sampler(tokenize(d), step).run()
        synchronized(lock) { cache[key] = result }
        return result
    }

    /** Drops every cached sampling. */
    public fun clearCache() {
        synchronized(lock) { cache.clear() }
    }

    private fun tokenize(d: String): List<Token> {
        val tokens = mutableListOf<Token>()
        val buffer = StringBuilder()
        // 0...6 while inside an arc's parameters, whose 4th and 5th (index 3 and 4) are one-digit flags that may be
        // written without a separator, as in `a1 1 0 00-1 0`; -1 elsewhere.
        var arcParam = -1

        fun flush() {
            buffer.toString().toDoubleOrNull()?.let { v ->
                tokens += Token.Num(v)
                if (arcParam >= 0) arcParam = (arcParam + 1) % 7
            }
            buffer.setLength(0)
        }
        for (ch in d) {
            val last = buffer.lastOrNull()
            if ((ch == 'e' || ch == 'E') && last != null && (last.isDigit() || last == '.')) {
                buffer.append(ch) // exponent
            } else if (ch.isLetter()) {
                flush()
                tokens += Token.Cmd(ch)
                arcParam = if (ch == 'A' || ch == 'a') 0 else -1
            } else if (ch == '-' || ch == '+') {
                if (last == 'e' || last == 'E') {
                    buffer.append(ch)
                } else {
                    flush()
                    if (ch == '-') buffer.append('-')
                }
            } else if (ch.isDigit() || ch == '.') {
                if ((arcParam == 3 || arcParam == 4) && ch.isDigit()) {
                    flush()
                    buffer.append(ch)
                    flush()
                } else {
                    if (ch == '.' && buffer.contains('.') && !buffer.contains('e') && !buffer.contains('E')) flush()
                    buffer.append(ch)
                }
            } else {
                flush()
            }
        }
        flush()
        return tokens
    }

    private class Sampler(
        private val tokens: List<Token>,
        private val step: Double,
    ) {
        private var i = 0
        private var cmd = 'M'
        private val subpaths = mutableListOf<List<Point>>()
        private var current = mutableListOf<Point>()
        private var pen = Point.ZERO
        private var start = Point.ZERO
        private var lastCubicControl: Point? = null
        private var lastQuadControl: Point? = null

        fun run(): List<List<Point>> {
            while (i < tokens.size) {
                val token = tokens[i]
                if (token is Token.Cmd) {
                    cmd = token.char
                    i++
                    if (cmd == 'Z' || cmd == 'z') {
                        line(start)
                        lastCubicControl = null
                        lastQuadControl = null
                        continue
                    }
                }
                val relative = cmd.isLowerCase()
                val handled =
                    when (cmd.uppercaseChar()) {
                        'M' -> moveTo(relative)
                        'L' -> lineTo(relative)
                        'H' -> horizontal(relative)
                        'V' -> vertical(relative)
                        'Q' -> quadraticTo(relative)
                        'T' -> smoothQuadraticTo(relative)
                        'C' -> cubicTo(relative)
                        'S' -> smoothCubicTo(relative)
                        'A' -> arcTo(relative)
                        else -> false
                    }
                if (!handled) skip()
            }
            if (current.size > 1) subpaths += current
            return subpaths
        }

        private fun moveTo(relative: Boolean): Boolean {
            val p = absolute(point() ?: return false, relative)
            if (current.size > 1) subpaths += current
            current = mutableListOf(p)
            pen = p
            start = p
            // Further pairs after a move are implicit line-tos.
            cmd = if (relative) 'l' else 'L'
            lastCubicControl = null
            lastQuadControl = null
            return true
        }

        private fun lineTo(relative: Boolean): Boolean {
            line(absolute(point() ?: return false, relative))
            lastCubicControl = null
            lastQuadControl = null
            return true
        }

        private fun horizontal(relative: Boolean): Boolean {
            val x = num() ?: return false
            line(Point(if (relative) pen.x + x else x, pen.y))
            lastCubicControl = null
            lastQuadControl = null
            return true
        }

        private fun vertical(relative: Boolean): Boolean {
            val y = num() ?: return false
            line(Point(pen.x, if (relative) pen.y + y else y))
            lastCubicControl = null
            lastQuadControl = null
            return true
        }

        private fun quadraticTo(relative: Boolean): Boolean {
            val c = point() ?: return false
            val p = point() ?: return false
            quadratic(absolute(c, relative), absolute(p, relative))
            return true
        }

        private fun smoothQuadraticTo(relative: Boolean): Boolean {
            val p = point() ?: return false
            val c = lastQuadControl?.let { Point(2 * pen.x - it.x, 2 * pen.y - it.y) } ?: pen
            quadratic(c, absolute(p, relative))
            return true
        }

        private fun cubicTo(relative: Boolean): Boolean {
            val c1 = point() ?: return false
            val c2 = point() ?: return false
            val p = point() ?: return false
            cubic(absolute(c1, relative), absolute(c2, relative), absolute(p, relative))
            lastQuadControl = null
            return true
        }

        private fun smoothCubicTo(relative: Boolean): Boolean {
            val c2 = point() ?: return false
            val p = point() ?: return false
            val c1 = lastCubicControl?.let { Point(2 * pen.x - it.x, 2 * pen.y - it.y) } ?: pen
            cubic(c1, absolute(c2, relative), absolute(p, relative))
            lastQuadControl = null
            return true
        }

        private fun arcTo(relative: Boolean): Boolean {
            val radii = point() ?: return false
            val rotation = num() ?: return false
            val largeArc = num() ?: return false
            val sweep = num() ?: return false
            val p = point() ?: return false
            arc(radii, rotation, largeArc != 0.0, sweep != 0.0, absolute(p, relative))
            lastCubicControl = null
            lastQuadControl = null
            return true
        }

        private fun num(): Double? {
            val token = tokens.getOrNull(i) as? Token.Num ?: return null
            i++
            return token.value
        }

        private fun point(): Point? {
            val x = num() ?: return null
            val y = num() ?: return null
            return Point(x, y)
        }

        /** A coordinate pair as read, made absolute for a lowercase command. */
        private fun absolute(
            p: Point,
            relative: Boolean,
        ): Point = if (relative) Point(pen.x + p.x, pen.y + p.y) else p

        private fun line(to: Point) {
            val n = max(1, (hypot(to.x - pen.x, to.y - pen.y) / step).toInt())
            for (k in 1..n) {
                val t = k.toDouble() / n
                current += Point(pen.x + (to.x - pen.x) * t, pen.y + (to.y - pen.y) * t)
            }
            pen = to
        }

        private fun cubic(
            c1: Point,
            c2: Point,
            p: Point,
        ) {
            val approx =
                hypot(c1.x - pen.x, c1.y - pen.y) + hypot(c2.x - c1.x, c2.y - c1.y) + hypot(p.x - c2.x, p.y - c2.y)
            val n = max(4, (approx / step).toInt())
            val p0 = pen
            for (k in 1..n) {
                val t = k.toDouble() / n
                val u = 1 - t
                val x = u * u * u * p0.x + 3 * u * u * t * c1.x + 3 * u * t * t * c2.x + t * t * t * p.x
                val y = u * u * u * p0.y + 3 * u * u * t * c1.y + 3 * u * t * t * c2.y + t * t * t * p.y
                current += Point(x, y)
            }
            pen = p
            lastCubicControl = c2
        }

        private fun quadratic(
            c: Point,
            p: Point,
        ) {
            // Elevate the quadratic to a cubic.
            val c1 = Point(pen.x + 2.0 / 3 * (c.x - pen.x), pen.y + 2.0 / 3 * (c.y - pen.y))
            val c2 = Point(p.x + 2.0 / 3 * (c.x - p.x), p.y + 2.0 / 3 * (c.y - p.y))
            cubic(c1, c2, p)
            lastCubicControl = null
            lastQuadControl = c
        }

        /** SVG implementation notes F.6.5: endpoint parameters to center parameterization, then sampled by angle. */
        private fun arc(
            radii: Point,
            rotation: Double,
            largeArc: Boolean,
            sweep: Boolean,
            p: Point,
        ) {
            if (p == pen) return
            var rx = abs(radii.x)
            var ry = abs(radii.y)
            if (rx <= 0 || ry <= 0) return line(p)
            val phi = rotation * PI / 180
            val cosPhi = cos(phi)
            val sinPhi = sin(phi)
            val dx2 = (pen.x - p.x) / 2
            val dy2 = (pen.y - p.y) / 2
            val x1p = cosPhi * dx2 + sinPhi * dy2
            val y1p = -sinPhi * dx2 + cosPhi * dy2
            val lambda = (x1p * x1p) / (rx * rx) + (y1p * y1p) / (ry * ry)
            if (lambda > 1) {
                rx *= sqrt(lambda)
                ry *= sqrt(lambda)
            }
            val numerator = rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p
            val denominator = rx * rx * y1p * y1p + ry * ry * x1p * x1p
            val sign = if (largeArc != sweep) 1.0 else -1.0
            val coefficient = sign * sqrt(max(0.0, if (denominator == 0.0) 0.0 else numerator / denominator))
            val cxp = coefficient * (rx * y1p / ry)
            val cyp = coefficient * -(ry * x1p / rx)
            val cx = cosPhi * cxp - sinPhi * cyp + (pen.x + p.x) / 2
            val cy = sinPhi * cxp + cosPhi * cyp + (pen.y + p.y) / 2

            fun angle(
                ux: Double,
                uy: Double,
                vx: Double,
                vy: Double,
            ): Double {
                val dot = ux * vx + uy * vy
                val length = hypot(ux, uy) * hypot(vx, vy)
                if (length <= 0) return 0.0
                val s = if (ux * vy - uy * vx < 0) -1.0 else 1.0
                return s * acos(min(1.0, max(-1.0, dot / length)))
            }
            val theta1 = angle(1.0, 0.0, (x1p - cxp) / rx, (y1p - cyp) / ry)
            var delta = angle((x1p - cxp) / rx, (y1p - cyp) / ry, (-x1p - cxp) / rx, (-y1p - cyp) / ry)
            if (!sweep && delta > 0) {
                delta -= 2 * PI
            } else if (sweep && delta < 0) {
                delta += 2 * PI
            }
            val n = max(4, (abs(delta) * max(rx, ry) / step).toInt())
            for (k in 1..n) {
                val t = theta1 + delta * k / n
                val x = cosPhi * rx * cos(t) - sinPhi * ry * sin(t) + cx
                val y = sinPhi * rx * cos(t) + cosPhi * ry * sin(t) + cy
                current += if (k == n) p else Point(x, y)
            }
            pen = p
        }

        /** Skips a parameter that doesn't parse. */
        private fun skip() {
            i++
        }
    }
}
