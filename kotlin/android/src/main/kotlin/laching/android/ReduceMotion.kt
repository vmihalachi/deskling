package laching.android

import android.animation.ValueAnimator
import android.content.Context
import android.provider.Settings

/**
 * Whether the user turned animations off (Developer options or the accessibility "Remove animations" switch), which
 * sets `ANIMATOR_DURATION_SCALE` or `TRANSITION_ANIMATION_SCALE` to 0. The Android counterpart of macOS's Reduce
 * Motion: doodles hold still, state changes cut instead of easing.
 */
public object ReduceMotion {
    public fun isEnabled(context: Context): Boolean {
        val resolver = context.contentResolver
        val animators = Settings.Global.getFloat(resolver, Settings.Global.ANIMATOR_DURATION_SCALE, 1f)
        val transitions = Settings.Global.getFloat(resolver, Settings.Global.TRANSITION_ANIMATION_SCALE, 1f)
        return animators == 0f || transitions == 0f || !ValueAnimator.areAnimatorsEnabled()
    }
}
