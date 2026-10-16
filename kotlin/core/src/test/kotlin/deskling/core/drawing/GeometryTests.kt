package deskling.core.drawing

import kotlin.math.abs
import kotlin.math.hypot
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNotEquals
import kotlin.test.assertTrue

class GeometryTests {
    private val line = (0..20).map { Point(it.toDouble(), 0.0) }

    @Test
    fun `no wobble is the identity`() {
        assertEquals(line, Wobble.apply(line, 1, 0.0))
    }

    @Test
    fun `the same seed gives the same wobble`() {
        val a = Wobble.apply(line, 3, 1.0)
        assertEquals(a, Wobble.apply(line, 3, 1.0))
        assertNotEquals(line, a)
        assertNotEquals(a, Wobble.apply(line, 4, 1.0))
        assertEquals(line.size, a.size)
    }

    @Test
    fun `the wobble stays within its amount`() {
        for ((i, p) in Wobble.apply(line, 2, 0.5).withIndex()) {
            assertTrue(abs(p.x - line[i].x) <= 0.5 + 1e-9)
            assertTrue(abs(p.y - line[i].y) <= 0.5 + 1e-9)
        }
    }

    @Test
    fun `noise stays within one`() {
        var s = 0.0
        while (s <= 196) {
            assertTrue(abs(Wobble.noise(s, 1)) <= 1.0, "s=$s")
            s += 7
        }
    }

    @Test
    fun `rotation turns about a point`() {
        val p = Point(2.0, 1.0).rotated(90.0, Point(1.0, 1.0))
        assertEquals(1.0, p.x, 1e-9)
        assertEquals(2.0, p.y, 1e-9)
        val same = Point(1.0, 1.0).rotated(45.0, Point(1.0, 1.0))
        assertEquals(1.0, same.x, 1e-9)
        assertEquals(1.0, same.y, 1e-9)
    }

    @Test
    fun `a circle starts at twelve and closes`() {
        val center = Point(5.0, 5.0)
        val circle = Polyline.circle(center, 2.0)
        assertEquals(25, circle.size)
        assertEquals(5.0, circle.first().x, 1e-9)
        assertEquals(3.0, circle.first().y, 1e-9)
        assertEquals(circle.first().x, circle.last().x, 1e-9)
        assertEquals(circle.first().y, circle.last().y, 1e-9)
        for (p in circle) assertEquals(2.0, hypot(p.x - center.x, p.y - center.y), 1e-9)
    }
}
