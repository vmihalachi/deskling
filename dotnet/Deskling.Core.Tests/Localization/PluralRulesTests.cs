using System.Security.Cryptography;
using System.Text;
using Deskling.Core.Localization;

namespace Deskling.Core.Tests;

public sealed class PluralRulesTests
{
    // Expectations from the CLDR integer rules, as mybackhurts' plurals.json vector lists them.
    [Theory]
    [InlineData("en", 1, "one")]
    [InlineData("en", 0, "other")]
    [InlineData("en", 2, "other")]
    [InlineData("en", 21, "other")]
    [InlineData("en", 1_000_000, "other")]
    [InlineData("de", 1, "one")]
    [InlineData("de", 0, "other")]
    [InlineData("de", 1_000_000, "other")]
    [InlineData("es", 1, "one")]
    [InlineData("es", 0, "other")]
    [InlineData("es", 1_000, "other")]
    [InlineData("es", 1_000_000, "many")]
    [InlineData("es", 2_000_000, "many")]
    [InlineData("es", 1_000_001, "other")]
    [InlineData("it", 1, "one")]
    [InlineData("it", 0, "other")]
    [InlineData("it", 10_000_000, "many")]
    [InlineData("fr", 0, "one")]
    [InlineData("fr", 1, "one")]
    [InlineData("fr", 2, "other")]
    [InlineData("fr", 1_000_000, "many")]
    [InlineData("fr", 1_000_001, "other")]
    [InlineData("pt-BR", 0, "one")]
    [InlineData("pt-BR", 1, "one")]
    [InlineData("pt-BR", 2, "other")]
    [InlineData("pt-BR", 1_000_000, "many")]
    [InlineData("pt-BR", 1_000_001, "other")]
    [InlineData("ro", 1, "one")]
    [InlineData("ro", 0, "few")]
    [InlineData("ro", 2, "few")]
    [InlineData("ro", 19, "few")]
    [InlineData("ro", 20, "other")]
    [InlineData("ro", 100, "other")]
    [InlineData("ro", 101, "few")]
    [InlineData("ro", 119, "few")]
    [InlineData("ro", 120, "other")]
    [InlineData("ro", 1_001, "few")]
    [InlineData("ro", 1_020, "other")]
    [InlineData("ro", 1_000_000, "other")]
    [InlineData("ro", 1_000_001, "few")]
    public void TestCategoriesMatchCldr(string language, int n, string expected)
    {
        Assert.Equal(expected, PluralRules.Category(language, n));
    }

    [Theory]
    [InlineData("EN", 1, "one")]
    [InlineData("en-US", 0, "other")]
    [InlineData("pt-br", 0, "one")]
    [InlineData("PT-BR", 1_000_000, "many")]
    [InlineData("pt", 0, "other")]
    [InlineData("pt-PT", 1, "one")]
    [InlineData("ro-RO", 5, "few")]
    public void TestLanguageTagsIgnoreCaseAndRegion(string language, int n, string expected)
    {
        Assert.Equal(expected, PluralRules.Category(language, n));
    }

    [Theory]
    [InlineData("ja", 1, "one")]
    [InlineData("ja", 2, "other")]
    [InlineData("", 0, "other")]
    public void TestUnknownLanguagesUseTheEnglishRule(string language, int n, string expected)
    {
        Assert.Equal(expected, PluralRules.Category(language, n));
    }

    [Fact]
    public void TestPluralKeyFallsBackToOther()
    {
        var available = new HashSet<string> { "one", "other" };
        Assert.Equal("key__one", PluralRules.PluralKey("key", "one", available));
        Assert.Equal("key__other", PluralRules.PluralKey("key", "many", available));
        Assert.Equal("key__few", PluralRules.PluralKey("key", "few", new HashSet<string> { "one", "few", "other" }));
    }

    [Fact]
    public void TestReswNameEscapesLikeL10nPy()
    {
        Assert.Equal("_0043hair_0020_0053quat", ReswName.For("Chair Squat"));
        // MRT names ignore case, so "To" and "to" need different names.
        Assert.NotEqual(ReswName.For("To").ToLowerInvariant(), ReswName.For("to").ToLowerInvariant());
        Assert.Equal(
            "_004Cast_0020week_003A_0020_0025lld_0020breaks_002C_0020_0025_0040_0020moved_002E",
            ReswName.For("Last week: %lld breaks, %@ moved.")
        );
        Assert.Equal("a_D83D_DE00", ReswName.For("a😀"));
        Assert.Equal("_005F", ReswName.For("_"));
    }

    [Fact]
    public void TestOverLongReswNamesAreShortenedWithAHash()
    {
        var longKey = string.Concat(Enumerable.Repeat("sitting all day ", 20));
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(longKey)))[..16];
        var name = ReswName.For(longKey);
        Assert.Equal(string.Concat(Enumerable.Repeat("sitting_0020all_0020day_0020", 7)) + "sitt_h" + hash, name);
        Assert.Equal(ReswName.PrefixLength + 2 + 16, name.Length);
        Assert.InRange(ReswName.For(string.Concat(Enumerable.Repeat("a", ReswName.MaxLength))).Length, 0, ReswName.MaxLength);
    }
}
