package laching.core.localization

import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNotEquals
import kotlin.test.assertTrue

class ResourceNamesTests {
    @Test
    fun `fallback text turns Apple specifiers into placeholders`() {
        assertEquals("Shh until {0}", ResourceNames.fallbackText("Shh until %@"))
        assertEquals("{1} of {0}, 100%", ResourceNames.fallbackText("%2\$@ of %1\$lld, 100%%"))
        assertEquals("{{braces}} {0}", ResourceNames.fallbackText("{braces} %lld"))
    }

    @Test
    fun `format fills placeholders and unescapes braces`() {
        assertEquals("x of 3, 100%", ResourceNames.format("{1} of {0}, 100%", 3, "x"))
        assertEquals("{braces} 5", ResourceNames.format("{{braces}} {0}", 5))
        assertEquals("{1} left", ResourceNames.format("{1} left", 0))
    }

    @Test
    fun `resw names escape everything but lower-case letters and digits`() {
        assertEquals("_0043hair_0020_0053quat", ResourceNames.reswName("Chair Squat"))
        assertNotEquals(ResourceNames.reswName("to"), ResourceNames.reswName("To"))
        assertNotEquals(ResourceNames.reswName("to"), ResourceNames.reswName("To").lowercase())
        assertEquals(
            "_004Cast_0020week_003A_0020_0025lld_0020breaks_002C_0020_0025_0040_0020moved_002E",
            ResourceNames.reswName("Last week: %lld breaks, %@ moved."),
        )
        assertEquals("a_D83D_DE00", ResourceNames.reswName("a😀"))
        assertEquals("_005F", ResourceNames.reswName("_"))
    }

    @Test
    fun `over-long names keep a prefix and a hash`() {
        val name = ResourceNames.reswName("sitting all day ".repeat(20))
        val prefix = "sitting_0020all_0020day_0020".repeat(7) + "sitt_h"
        assertTrue(name.startsWith(prefix), name)
        assertEquals(218, name.length)
        assertTrue(name.substring(prefix.length).all { it in '0'..'9' || it in 'a'..'f' })
        assertEquals(240, ResourceNames.reswName("a".repeat(240)).length)
    }

    @Test
    fun `android names are the resw name lower-cased behind s_`() {
        assertEquals("s__0043hair_0020_0053quat", ResourceNames.androidName("Chair Squat"))
        assertEquals("s_to", ResourceNames.androidName("to"))
    }
}
