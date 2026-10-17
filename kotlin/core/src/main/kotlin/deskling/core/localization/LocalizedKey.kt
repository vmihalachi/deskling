package deskling.core.localization

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

/**
 * A localized string as a catalog key plus its arguments. Pure code returns these and the UI layer localizes
 * them, so conformance vectors never contain display text. Two keys are equal when their keys and arguments are.
 * Serialized with the same shape as the Swift side: `{"key":"stats.breaks","args":[{"type":"int","value":3}]}`.
 */
@Serializable
public data class LocalizedKey(
    public val key: String,
    public val args: List<Argument>,
) {
    public constructor(key: String) : this(key, emptyList())

    /** What an argument is, and so how the UI layer formats it. Serialized by name, as the Swift side writes it. */
    @Serializable
    public enum class ArgumentKind {
        /** A number, also the plural count. */
        @SerialName("int")
        INT,

        /**
         * Minutes, formatted with the platform's abbreviated hours/minutes duration style, e.g. "34 min",
         * "1 hr, 5 min".
         */
        @SerialName("durationMinutes")
        DURATION_MINUTES,
    }

    /** One argument: its [ArgumentKind] and the number it carries. */
    @Serializable
    public data class Argument(
        public val type: ArgumentKind,
        public val value: Int,
    ) {
        public companion object {
            public fun int(value: Int): Argument = Argument(ArgumentKind.INT, value)

            public fun durationMinutes(value: Int): Argument = Argument(ArgumentKind.DURATION_MINUTES, value)
        }
    }
}
