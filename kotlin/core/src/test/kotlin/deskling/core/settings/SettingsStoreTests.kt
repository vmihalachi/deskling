package deskling.core.settings

import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNull

class SettingsStoreTests {
    private val store = InMemorySettingsStore()

    @Test
    fun `every type round trips`() {
        store.setBool("b", true)
        store.setInteger("i", 7)
        store.setDouble("d", 2.5)
        store.setString("s", "hello")
        store.setStrings("ss", listOf("a", "b"))
        store.setIntegers("is", listOf(1, 2, 3))
        assertEquals(true, store.getBool("b"))
        assertEquals(7, store.getInteger("i"))
        assertEquals(2.5, store.getDouble("d"))
        assertEquals("hello", store.getString("s"))
        assertEquals(listOf("a", "b"), store.getStrings("ss"))
        assertEquals(listOf(1, 2, 3), store.getIntegers("is"))
        assertEquals(listOf("b", "d", "i", "is", "s", "ss"), store.keys.sorted())
    }

    @Test
    fun `missing keys read as null`() {
        assertNull(store.getBool("x"))
        assertNull(store.getInteger("x"))
        assertNull(store.getDouble("x"))
        assertNull(store.getString("x"))
        assertNull(store.getStrings("x"))
        assertNull(store.getIntegers("x"))
    }

    @Test
    fun `remove forgets a key`() {
        store.setString("s", "hello")
        store.remove("s")
        assertNull(store.getString("s"))
        assertEquals(emptySet(), store.keys)
    }

    @Test
    fun `the wrong type reads as null`() {
        store.setInteger("i", 1)
        store.setBool("b", true)
        store.setDouble("d", 1.0)
        assertNull(store.getBool("i"))
        assertNull(store.getDouble("i"))
        assertNull(store.getString("i"))
        assertNull(store.getInteger("b"))
        assertNull(store.getInteger("d"))
        assertNull(store.getStrings("i"))
        assertNull(store.getIntegers("i"))
    }

    @Test
    fun `lists are copied on the way in`() {
        val items = mutableListOf("a")
        store.setStrings("ss", items)
        items += "b"
        assertEquals(listOf("a"), store.getStrings("ss"))
    }
}
