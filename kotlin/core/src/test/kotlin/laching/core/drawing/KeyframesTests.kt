package laching.core.drawing

import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertTrue

class KeyframesTests {
    @Test
    fun `holds the end values outside the stops`() {
        val stops = listOf(0.2 to 1.0, 0.8 to 3.0)
        assertEquals(1.0, Keyframes.value(0.0, stops))
        assertEquals(1.0, Keyframes.value(0.2, stops))
        assertEquals(3.0, Keyframes.value(0.8, stops))
        assertEquals(3.0, Keyframes.value(1.0, stops))
    }

    @Test
    fun `eases between two stops`() {
        val stops = listOf(0.0 to 0.0, 1.0 to 10.0)
        assertEquals(5.0, Keyframes.value(0.5, stops), 1e-9)
        assertTrue(Keyframes.value(0.25, stops) < 2.5)
        assertEquals(1.5625, Keyframes.value(0.25, stops), 1e-9)
        assertTrue(Keyframes.value(0.75, stops) > 7.5)
    }

    @Test
    fun `follows several stops`() {
        val stops = listOf(0.0 to 0.0, 0.5 to 10.0, 1.0 to 0.0)
        assertEquals(5.0, Keyframes.value(0.25, stops), 1e-9)
        assertEquals(10.0, Keyframes.value(0.5, stops), 1e-9)
        assertEquals(5.0, Keyframes.value(0.75, stops), 1e-9)
    }

    @Test
    fun `no stops is zero`() {
        assertEquals(0.0, Keyframes.value(0.5, emptyList()))
    }
}
