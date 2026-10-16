package deskling.core.drawing

import kotlin.math.hypot
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNotEquals
import kotlin.test.assertSame
import kotlin.test.assertTrue

class SvgPathTests {
    private val accuracy = 0.01

    private fun assertPoint(
        x: Double,
        y: Double,
        actual: Point,
    ) {
        assertEquals(x, actual.x, accuracy, "x of $actual")
        assertEquals(y, actual.y, accuracy, "y of $actual")
    }

    private fun assertSamePoints(
        expected: List<List<Point>>,
        actual: List<List<Point>>,
    ) {
        assertEquals(expected.size, actual.size, "subpaths")
        for ((e, a) in expected.zip(actual)) {
            assertEquals(e.size, a.size, "points")
            for ((ep, ap) in e.zip(a)) assertPoint(ep.x, ep.y, ap)
        }
    }

    @Test
    fun `lines are sampled by step`() {
        val paths = SvgPath.sample("M 0 0 L 10 0 L 10 10", 2.0)
        assertEquals(1, paths.size)
        val points = paths[0]
        assertEquals(11, points.size)
        assertPoint(0.0, 0.0, points[0])
        assertPoint(10.0, 0.0, points[5])
        assertPoint(10.0, 10.0, points[10])
    }

    @Test
    fun `each move starts a subpath`() {
        val paths = SvgPath.sample("M0,0 L10,0 M0,10 L10,10", 5.0)
        assertEquals(2, paths.size)
        assertPoint(0.0, 10.0, paths[1].first())
        assertPoint(10.0, 10.0, paths[1].last())
    }

    @Test
    fun `relative commands match absolute ones`() {
        val absolute = SvgPath.sample("M 10 10 L 20 10 L 20 20 C 20 30 10 30 10 20 Q 5 15 10 10 Z", 1.0)
        val relative = SvgPath.sample("m 10 10 l 10 0 l 0 10 c 0 10 -10 10 -10 0 q -5 -5 0 -10 z", 1.0)
        assertSamePoints(absolute, relative)
        assertPoint(10.0, 10.0, absolute[0].last())
    }

    @Test
    fun `pairs after a move are lines`() {
        val absolute = SvgPath.sample("M 0 0 10 0 10 10", 5.0)
        assertSamePoints(absolute, SvgPath.sample("m 0 0 10 0 0 10", 5.0))
        assertPoint(10.0, 10.0, absolute[0].last())
    }

    @Test
    fun `horizontal and vertical lines`() {
        val points = SvgPath.sample("M 0 0 H 10 V 10 h -10 v -10", 5.0)[0]
        assertEquals(9, points.size)
        assertPoint(10.0, 0.0, points[2])
        assertPoint(10.0, 10.0, points[4])
        assertPoint(0.0, 10.0, points[6])
        assertPoint(0.0, 0.0, points[8])
    }

    @Test
    fun `smooth cubics reflect the last control`() {
        assertSamePoints(
            SvgPath.sample("M 0 0 C 0 10 10 10 10 0 C 10 -10 20 -10 20 0", 1.0),
            SvgPath.sample("M 0 0 C 0 10 10 10 10 0 S 20 -10 20 0", 1.0),
        )
    }

    @Test
    fun `smooth quadratics reflect the last control`() {
        assertSamePoints(
            SvgPath.sample("M 0 0 Q 5 10 10 0 Q 15 -10 20 0", 1.0),
            SvgPath.sample("M 0 0 Q 5 10 10 0 T 20 0", 1.0),
        )
    }

    @Test
    fun `a quadratic reaches half its control height`() {
        val points = SvgPath.sample("M 0 0 Q 5 10 10 0", 0.5)[0]
        assertPoint(10.0, 0.0, points.last())
        assertEquals(5.0, points.maxOf { it.y }, 0.05)
    }

    @Test
    fun `arcs follow their circle in either direction`() {
        val sweep = SvgPath.sample("M 0 0 A 10 10 0 0 1 20 0", 1.0)[0]
        val counter = SvgPath.sample("M 0 0 A 10 10 0 0 0 20 0", 1.0)[0]
        for (points in listOf(sweep, counter)) {
            assertPoint(20.0, 0.0, points.last())
            assertTrue(points.size > 20)
            for (p in points) assertEquals(10.0, hypot(p.x - 10, p.y), accuracy, "$p")
        }
        assertTrue(sweep.minOf { it.y } < -9.9)
        assertTrue(counter.maxOf { it.y } > 9.9)
    }

    @Test
    fun `the large arc flag takes the long way`() {
        val short = SvgPath.sample("M 0 0 A 10 10 0 0 1 10 10", 1.0)[0]
        val long = SvgPath.sample("M 0 0 A 10 10 0 1 1 10 10", 1.0)[0]
        assertTrue(long.size > short.size * 2)
        assertPoint(10.0, 10.0, short.last())
        assertPoint(10.0, 10.0, long.last())
    }

    @Test
    fun `arc flags need no separators`() {
        val spaced = SvgPath.sample("M 1 0 a 1 1 0 0 0 -1 0", 0.1)
        assertSamePoints(spaced, SvgPath.sample("M1 0a1 1 0 00-1 0", 0.1))
        assertPoint(0.0, 0.0, spaced[0].last())
    }

    @Test
    fun `a zero radius arc is a line`() {
        val points = SvgPath.sample("M 0 0 A 0 0 0 0 1 10 0", 5.0)[0]
        assertEquals(3, points.size)
        assertPoint(10.0, 0.0, points.last())
    }

    @Test
    fun `exponents and bare decimals parse`() {
        val points = SvgPath.sample("M 0 0 L 1e1 0 L 10 .5.5", 100.0)[0]
        assertEquals(3, points.size)
        assertPoint(10.0, 0.0, points[1])
        assertPoint(10.0, 0.5, points[2])
    }

    @Test
    fun `samplings are cached per path and step`() {
        val d = "M 0 0 C 10 20 30 -20 40 0 A 5 5 0 1 0 50 0 Z"
        val first = SvgPath.sample(d)
        assertSame(first, SvgPath.sample(d))
        SvgPath.clearCache()
        assertSamePoints(first, SvgPath.sample(d))
        assertNotEquals(first[0].size, SvgPath.sample(d, 0.5)[0].size)
    }
}
