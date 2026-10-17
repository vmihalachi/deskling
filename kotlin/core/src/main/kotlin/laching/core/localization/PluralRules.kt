package laching.core.localization

/**
 * CLDR plural categories for integers in the languages the apps ship (cs, da, de, en, es, fr, it, ja, ko, nb, nl,
 * pl, pt-BR, ro, sv, tr, zh), so a count picks the same variant of a string as Foundation does on the Mac. Unknown
 * languages use the English rule. The same rule as `Laching.Core`'s `PluralRules`.
 */
public object PluralRules {
    /** The category ("one", "few", "many" or "other") of [n] in [language], a BCP 47 tag. */
    public fun category(
        language: String,
        n: Int,
    ): String {
        val lang = language.lowercase()
        if (lang.startsWith("pt-br")) return if (million(n)) "many" else oneOrOther(n, zeroIsOne = true)
        return when (lang.split('-')[0]) {
            "en", "da", "de", "nb", "nl", "no", "sv", "tr" -> {
                oneOrOther(n, zeroIsOne = false)
            }

            "es", "it" -> {
                if (million(n)) "many" else oneOrOther(n, zeroIsOne = false)
            }

            "fr" -> {
                if (million(n)) "many" else oneOrOther(n, zeroIsOne = true)
            }

            "ro" -> {
                romanian(n)
            }

            "pl" -> {
                polish(n)
            }

            "cs" -> {
                if (n == 1) {
                    "one"
                } else if (n in 2..4) {
                    "few"
                } else {
                    "other"
                }
            }

            "ja", "ko", "zh" -> {
                "other"
            }

            else -> {
                oneOrOther(n, zeroIsOne = false)
            }
        }
    }

    /**
     * The resource name for [category] of a plural string: `name__category`, falling back to `name__other` when
     * the string has no variant for that category.
     */
    public fun pluralKey(
        name: String,
        category: String,
        availableCategories: Set<String>,
    ): String {
        val chosen = if (category in availableCategories) category else "other"
        return "${name}__$chosen"
    }

    private fun oneOrOther(
        n: Int,
        zeroIsOne: Boolean,
    ): String = if (n == 1 || (zeroIsOne && n == 0)) "one" else "other"

    private fun million(n: Int): Boolean = n != 0 && n % 1_000_000 == 0

    private fun romanian(n: Int): String {
        if (n == 1) return "one"
        val mod100 = n % 100
        return if (n == 0 || mod100 in 1..19) "few" else "other"
    }

    private fun polish(n: Int): String {
        if (n == 1) return "one"
        val mod10 = n % 10
        val mod100 = n % 100
        return if (mod10 in 2..4 && mod100 !in 12..14) "few" else "many"
    }
}
