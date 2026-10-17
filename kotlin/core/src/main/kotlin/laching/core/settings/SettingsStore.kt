package laching.core.settings

/**
 * Where an app's settings are kept: `UserDefaults` on the Mac, the app's local settings on Windows,
 * `SharedPreferences` on Android (`SharedPreferencesSettingsStore` in `laching.android`), memory in tests
 * ([InMemorySettingsStore]). Values are typed; a getter returns null when the key is missing or holds another type.
 * A store never touches the network.
 */
public interface SettingsStore {
    public fun getBool(key: String): Boolean?

    public fun getInteger(key: String): Int?

    public fun getDouble(key: String): Double?

    public fun getString(key: String): String?

    public fun getStrings(key: String): List<String>?

    public fun getIntegers(key: String): List<Int>?

    public fun setBool(
        key: String,
        value: Boolean,
    )

    public fun setInteger(
        key: String,
        value: Int,
    )

    public fun setDouble(
        key: String,
        value: Double,
    )

    public fun setString(
        key: String,
        value: String,
    )

    public fun setStrings(
        key: String,
        value: List<String>,
    )

    public fun setIntegers(
        key: String,
        value: List<Int>,
    )

    public fun remove(key: String)
}

/** A settings store that forgets everything when it goes away: tests and previews. */
public class InMemorySettingsStore : SettingsStore {
    private class Strings(
        val items: List<String>,
    )

    private class Integers(
        val items: List<Int>,
    )

    private val values = LinkedHashMap<String, Any>()

    /** Every key with a value. */
    public val keys: Set<String>
        get() = values.keys

    override fun getBool(key: String): Boolean? = values[key] as? Boolean

    override fun getInteger(key: String): Int? = values[key] as? Int

    override fun getDouble(key: String): Double? = values[key] as? Double

    override fun getString(key: String): String? = values[key] as? String

    override fun getStrings(key: String): List<String>? = (values[key] as? Strings)?.items

    override fun getIntegers(key: String): List<Int>? = (values[key] as? Integers)?.items

    override fun setBool(
        key: String,
        value: Boolean,
    ) {
        values[key] = value
    }

    override fun setInteger(
        key: String,
        value: Int,
    ) {
        values[key] = value
    }

    override fun setDouble(
        key: String,
        value: Double,
    ) {
        values[key] = value
    }

    override fun setString(
        key: String,
        value: String,
    ) {
        values[key] = value
    }

    override fun setStrings(
        key: String,
        value: List<String>,
    ) {
        values[key] = Strings(value.toList())
    }

    override fun setIntegers(
        key: String,
        value: List<Int>,
    ) {
        values[key] = Integers(value.toList())
    }

    override fun remove(key: String) {
        values.remove(key)
    }
}
