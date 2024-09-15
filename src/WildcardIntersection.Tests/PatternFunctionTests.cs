using FluentAssertions;
using static WildcardIntersection.PatternFunctions;

namespace WildcardIntersection.Tests;

public class PatternFunctionTests
{
    [Theory]
    [InlineData("", "", "")]
    //
    [InlineData("a", "a", "a")]
    //
    [InlineData("a*", "a", "a")]
    [InlineData("a", "a*", "a")]
    //
    [InlineData("a", "aa", "")]
    [InlineData("aa", "a", "")]
    //
    [InlineData("a*", "a*", "a*")]
    //
    [InlineData("a*", "aa", "aa")]
    [InlineData("aa", "a*", "aa")]
    //
    [InlineData("a*a", "aaa", "aaa")]
    [InlineData("aaa", "a*a", "aaa")]
    //
    [InlineData("a*a", "aa", "aa")]
    [InlineData("aa", "a*a", "aa")]
    //
    [InlineData("a*a", "aaaa", "aaaa")]
    [InlineData("aaaa", "a*a", "aaaa")]
    //
    [InlineData("a*cdea", "abcd*a", "abcd*ea")]
    [InlineData("abcd*a", "a*cdea", "abcd*ea")]
    //
    [InlineData("a*dea", "abc*a", "abc*dea")]
    [InlineData("abc*a", "a*dea", "abc*dea")]
    //
    [InlineData("a*defga", "abc*a", "abc*defga")]
    [InlineData("abc*a", "a*defga", "abc*defga")]
    public void AssertPatternIntersection(string x, string y, string expected)
    {
        IntersectPatterns(x, y).Should().Be(expected);
    }
}
