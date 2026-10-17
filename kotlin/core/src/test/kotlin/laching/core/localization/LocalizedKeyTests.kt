package laching.core.localization

import kotlinx.serialization.SerializationException
import kotlinx.serialization.json.Json
import laching.core.localization.LocalizedKey.Argument
import laching.core.localization.LocalizedKey.ArgumentKind
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertFailsWith
import kotlin.test.assertNotEquals
import kotlin.test.assertTrue

class LocalizedKeyTests {
    @Test
    fun `a key alone has no arguments`() {
        val key = LocalizedKey("stats.none")
        assertEquals("stats.none", key.key)
        assertTrue(key.args.isEmpty())
    }

    @Test
    fun `argument factories set the kind`() {
        assertEquals(Argument(ArgumentKind.INT, 3), Argument.int(3))
        assertEquals(Argument(ArgumentKind.DURATION_MINUTES, 65), Argument.durationMinutes(65))
    }

    @Test
    fun `keys are equal by value`() {
        val a = LocalizedKey("stats.breaks", listOf(Argument.int(3), Argument.durationMinutes(65)))
        val b = LocalizedKey("stats.breaks", listOf(Argument.int(3), Argument.durationMinutes(65)))
        assertEquals(a, b)
        assertEquals(a.hashCode(), b.hashCode())
        assertNotEquals(a, LocalizedKey("stats.breaks", listOf(Argument.int(4), Argument.durationMinutes(65))))
        assertNotEquals(a, LocalizedKey("stats.other", a.args))
    }

    @Test
    fun `JSON round trips in the shared shape`() {
        val key = LocalizedKey("stats.breaks", listOf(Argument.int(3), Argument.durationMinutes(65)))
        val json = Json.encodeToString(LocalizedKey.serializer(), key)
        assertEquals(
            """{"key":"stats.breaks","args":[{"type":"int","value":3},{"type":"durationMinutes","value":65}]}""",
            json,
        )
        assertEquals(key, Json.decodeFromString(LocalizedKey.serializer(), json))
        assertEquals("""{"key":"a","args":[]}""", Json.encodeToString(LocalizedKey.serializer(), LocalizedKey("a")))
    }

    @Test
    fun `an unknown argument kind fails to decode`() {
        assertFailsWith<SerializationException> {
            Json.decodeFromString(LocalizedKey.serializer(), """{"key":"k","args":[{"type":"float","value":1}]}""")
        }
    }
}
