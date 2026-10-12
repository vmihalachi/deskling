using Deskling.Windows.Shell;

namespace Deskling.Windows.Tests.Shell;

public sealed class LayoutCheckTests
{
    [Fact]
    public void UnbreakablePiecesAreWordsSplitAfterHyphensDashesAndSlashes()
    {
        Assert.Equal(["Remind", "me", "every"], LayoutCheck.Unbreakable("Remind me  every\n"));
        Assert.Equal(["Upper-", "back", "and/", "or", "neck—", "stretch"], LayoutCheck.Unbreakable("Upper-back and/or neck—stretch"));
        Assert.Equal(["Erinnerungseinstellungen"], LayoutCheck.Unbreakable("Erinnerungseinstellungen"));
        Assert.Empty(LayoutCheck.Unbreakable("   "));
    }

    [Fact]
    public void ChineseAndJapaneseHaveNothingUnbreakable()
    {
        Assert.Empty(LayoutCheck.Unbreakable("每隔一段时间提醒我"));
        Assert.Empty(LayoutCheck.Unbreakable("リマインダーを一時停止"));
        Assert.Equal(["Pause"], LayoutCheck.Unbreakable("Pause 暂停"));
        Assert.Equal(["알림을", "일시", "중지"], LayoutCheck.Unbreakable("알림을 일시 중지"));
    }
}
