import Foundation

/// One thing for the shell to do: show this line in the bubble, and speak, chime or notify as planned.
public struct Delivery: Equatable, Sendable {
    public let phrase: Phrase
    public let trigger: Trigger
    public let plan: ChannelPlan

    public init(phrase: Phrase, trigger: Trigger, plan: ChannelPlan) {
        self.phrase = phrase
        self.trigger = trigger
        self.plan = plan
    }
}
