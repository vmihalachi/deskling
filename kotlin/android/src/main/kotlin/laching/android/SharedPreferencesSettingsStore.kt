package laching.android

import android.content.Context
import android.content.SharedPreferences
import androidx.core.content.edit
import laching.core.settings.SettingsStore

/**
 * [SettingsStore] over [SharedPreferences]. Booleans, integers and strings are stored as themselves; a double as the
 * long of its raw bits (preferences have no double), and a list as a string set whose members carry a type tag and
 * an index (`s0:first`, `i1:42`), so every type stays distinct and a key read with the wrong type answers null, as on
 * the Mac and in [laching.core.settings.InMemorySettingsStore]. Writes apply asynchronously.
 */
public class SharedPreferencesSettingsStore(
    private val preferences: SharedPreferences,
) : SettingsStore {
    public constructor(context: Context, name: String = "laching") : this(
        context.getSharedPreferences(name, Context.MODE_PRIVATE),
    )

    override fun getBool(key: String): Boolean? = preferences.all[key] as? Boolean

    override fun getInteger(key: String): Int? = preferences.all[key] as? Int

    override fun getDouble(key: String): Double? =
        (preferences.all[key] as? Long)?.let(java.lang.Double::longBitsToDouble)

    override fun getString(key: String): String? = preferences.all[key] as? String

    override fun getStrings(key: String): List<String>? = decode(key, 's')

    override fun getIntegers(key: String): List<Int>? = decode(key, 'i')?.map { it.toIntOrNull() ?: return null }

    override fun setBool(
        key: String,
        value: Boolean,
    ): Unit = preferences.edit { putBoolean(key, value) }

    override fun setInteger(
        key: String,
        value: Int,
    ): Unit = preferences.edit { putInt(key, value) }

    override fun setDouble(
        key: String,
        value: Double,
    ): Unit = preferences.edit { putLong(key, java.lang.Double.doubleToRawLongBits(value)) }

    override fun setString(
        key: String,
        value: String,
    ): Unit = preferences.edit { putString(key, value) }

    override fun setStrings(
        key: String,
        value: List<String>,
    ): Unit = preferences.edit { putStringSet(key, encode('s', value)) }

    override fun setIntegers(
        key: String,
        value: List<Int>,
    ): Unit = preferences.edit { putStringSet(key, encode('i', value.map { it.toString() })) }

    override fun remove(key: String): Unit = preferences.edit { remove(key) }

    private fun encode(
        tag: Char,
        items: List<String>,
    ): Set<String> = items.mapIndexed { index, item -> "$tag$index:$item" }.toSet()

    private fun decode(
        key: String,
        tag: Char,
    ): List<String>? {
        val members = preferences.all[key] as? Set<*> ?: return null
        return members
            .map { member ->
                val text = member as? String ?: return null
                if (text.firstOrNull() != tag) return null
                val colon = text.indexOf(':')
                if (colon < 2) return null
                val index = text.substring(1, colon).toIntOrNull() ?: return null
                index to text.substring(colon + 1)
            }.sortedBy { it.first }
            .map { it.second }
    }
}
