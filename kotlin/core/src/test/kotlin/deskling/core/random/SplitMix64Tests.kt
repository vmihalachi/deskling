package deskling.core.random

import kotlin.test.Test
import kotlin.test.assertEquals

/** The reference sequences every implementation reproduces (Vigna's test vectors and the C# suite's). */
class SplitMix64Tests {
    @Test
    fun `seed zero gives the reference sequence`() {
        val random = SplitMix64(0uL)
        assertEquals(0xE220A8397B1DCDAFuL, random.next())
        assertEquals(0x6E789E6AA1B965F4uL, random.next())
        assertEquals(0x06C45D188009454FuL, random.next())
    }

    @Test
    fun `seed 42 gives the reference sequence`() {
        val random = SplitMix64(42uL)
        assertEquals(13679457532755275413uL, random.next())
        assertEquals(2949826092126892291uL, random.next())
        assertEquals(5139283748462763858uL, random.next())
    }

    @Test
    fun `units are the top 53 bits`() {
        val random = SplitMix64(42uL)
        assertEquals(0.7415648787718233, random.nextUnit())
        assertEquals(0.1599103928769201, random.nextUnit())
        assertEquals(0.27860113025513866, random.nextUnit())
    }

    @Test
    fun `the state advances by the golden gamma`() {
        val random = SplitMix64(1uL)
        random.next()
        assertEquals(1uL + 0x9E3779B97F4A7C15uL, random.state)
    }
}
