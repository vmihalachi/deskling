package laching.core.scheduling

/** How long since the user last touched the keyboard, mouse or screen, in seconds. */
public fun interface IdleTimeProvider {
    public fun secondsSinceLastInput(): Double
}

/** Why the user is busy right now: a call, or an app in full screen (a talk, a film). */
public enum class BusyReason {
    CALL,
    FULL_SCREEN,
}

/** What keeps the user busy at this moment; empty when nothing does. */
public fun interface BusyStateProvider {
    public fun currentBusyReasons(): Set<BusyReason>
}

/** A platform without call or full-screen detection. */
public object NeverBusy : BusyStateProvider {
    override fun currentBusyReasons(): Set<BusyReason> = emptySet()
}
