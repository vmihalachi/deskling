using Deskling.Companion.Phrases;
using Deskling.Companion.Rules;

namespace Deskling.Companion.Coordination;

/// <summary>One thing for the shell to do: show this line in the bubble, and speak, chime or notify as planned.</summary>
public sealed record Delivery(Phrase Phrase, Trigger Trigger, ChannelPlan Plan);
