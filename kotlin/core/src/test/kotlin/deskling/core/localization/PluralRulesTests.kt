package deskling.core.localization

import kotlin.test.Test
import kotlin.test.assertEquals

class PluralRulesTests {
    private fun check(
        language: String,
        vararg cases: Pair<Int, String>,
    ) {
        for ((n, expected) in cases) {
            assertEquals(expected, PluralRules.category(language, n), "$language $n")
        }
    }

    @Test
    fun `one or other languages`() {
        check("en", 1 to "one", 0 to "other", 2 to "other", 21 to "other", 1_000_000 to "other")
        check("de", 1 to "one", 0 to "other", 2 to "other")
        for (language in listOf("nl", "sv", "da", "nb", "tr")) check(language, 1 to "one", 0 to "other", 2 to "other")
    }

    @Test
    fun `millions in the Romance languages`() {
        check("es", 1 to "one", 0 to "other", 1_000_000 to "many", 2_000_000 to "many", 1_000_001 to "other")
        check("it", 1 to "one", 10_000_000 to "many")
        check("fr", 0 to "one", 1 to "one", 2 to "other", 1_000_000 to "many")
        check("pt-BR", 0 to "one", 1 to "one", 2 to "other", 1_000_000 to "many")
    }

    @Test
    fun `romanian`() {
        check(
            "ro",
            1 to "one",
            0 to "few",
            2 to "few",
            19 to "few",
            20 to "other",
            100 to "other",
            101 to "few",
            119 to "few",
            120 to "other",
            1001 to "few",
            1020 to "other",
            1_000_000 to "other",
            1_000_001 to "few",
        )
    }

    @Test
    fun `polish`() {
        check(
            "pl",
            0 to "many",
            1 to "one",
            2 to "few",
            4 to "few",
            5 to "many",
            11 to "many",
            12 to "many",
            14 to "many",
            21 to "many",
            22 to "few",
            112 to "many",
            1_000_000 to "many",
        )
    }

    @Test
    fun `czech`() {
        check("cs", 0 to "other", 1 to "one", 2 to "few", 4 to "few", 5 to "other", 22 to "other")
    }

    @Test
    fun `languages without plurals`() {
        for (language in listOf("ja", "ko", "zh-Hans")) check(language, 0 to "other", 1 to "other", 2 to "other")
    }

    @Test
    fun `case and region are tolerated`() {
        check("EN", 1 to "one")
        check("en-US", 1 to "one", 2 to "other")
        check("pt-br", 0 to "one")
        check("PT-BR", 1_000_000 to "many")
        check("pt", 0 to "other")
        check("pt-PT", 1 to "one")
        check("ro-RO", 5 to "few")
        check("zh", 1 to "other")
        check("zh-Hant", 1 to "other")
        check("pl-PL", 3 to "few")
    }

    @Test
    fun `unknown languages use the English rule`() {
        check("eo", 1 to "one", 2 to "other")
        check("", 1 to "one", 0 to "other")
    }

    @Test
    fun `plural keys fall back to other`() {
        val available = setOf("one", "other")
        assertEquals("key__one", PluralRules.pluralKey("key", "one", available))
        assertEquals("key__other", PluralRules.pluralKey("key", "many", available))
        assertEquals("key__few", PluralRules.pluralKey("key", "few", setOf("one", "few", "other")))
    }
}
