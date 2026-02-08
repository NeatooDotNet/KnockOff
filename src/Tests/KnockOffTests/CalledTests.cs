using KnockOff;
using Xunit;

namespace KnockOffTests;

public class CalledTests
{
    [Fact]
    public void Once_HasCountOfOne()
    {
        var times = Called.Once;
        Assert.Equal(1, times.Count);
    }

    [Fact]
    public void Twice_HasCountOfTwo()
    {
        var times = Called.Twice;
        Assert.Equal(2, times.Count);
    }

    [Fact]
    public void Exactly_HasSpecifiedCount()
    {
        var times = Called.Exactly(5);
        Assert.Equal(5, times.Count);
    }

    [Fact]
    public void AtLeast_ForVerification()
    {
        var times = Called.AtLeast(3);
        Assert.True(times.Validate(3));
        Assert.True(times.Validate(5));
        Assert.False(times.Validate(2));
    }

    [Fact]
    public void AtLeastOnce_ForVerification()
    {
        var times = Called.AtLeastOnce;
        Assert.True(times.Validate(1));
        Assert.True(times.Validate(5));
        Assert.False(times.Validate(0));
    }

    [Fact]
    public void AtMost_ForVerification()
    {
        var times = Called.AtMost(3);
        Assert.True(times.Validate(0));
        Assert.True(times.Validate(3));
        Assert.False(times.Validate(4));
    }

    [Fact]
    public void Never_ForVerification()
    {
        var times = Called.Never;
        Assert.True(times.Validate(0));
        Assert.False(times.Validate(1));
    }

    #region Called.Validate() Coverage

    [Fact]
    public void Exactly_Validate_ReturnsTrue_WhenCountMatches()
    {
        var times = Called.Exactly(3);
        Assert.True(times.Validate(3));
    }

    [Fact]
    public void Exactly_Validate_ReturnsFalse_WhenCountTooLow()
    {
        var times = Called.Exactly(3);
        Assert.False(times.Validate(2));
    }

    [Fact]
    public void Exactly_Validate_ReturnsFalse_WhenCountTooHigh()
    {
        var times = Called.Exactly(3);
        Assert.False(times.Validate(4));
    }

    [Fact]
    public void Once_Validate_ReturnsTrue_WhenCalledOnce()
    {
        var times = Called.Once;
        Assert.True(times.Validate(1));
    }

    [Fact]
    public void Once_Validate_ReturnsFalse_WhenNotCalled()
    {
        var times = Called.Once;
        Assert.False(times.Validate(0));
    }

    [Fact]
    public void Once_Validate_ReturnsFalse_WhenCalledTwice()
    {
        var times = Called.Once;
        Assert.False(times.Validate(2));
    }

    [Fact]
    public void Twice_Validate_ReturnsTrue_WhenCalledTwice()
    {
        var times = Called.Twice;
        Assert.True(times.Validate(2));
    }

    #endregion

    #region ToString Coverage

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        Assert.Equal("Once", Called.Once.ToString());
        Assert.Equal("Twice", Called.Twice.ToString());
        Assert.Equal("Exactly(5)", Called.Exactly(5).ToString());
        Assert.Equal("AtLeastOnce", Called.AtLeastOnce.ToString());
        Assert.Equal("AtLeast(3)", Called.AtLeast(3).ToString());
        Assert.Equal("AtMost(5)", Called.AtMost(5).ToString());
        Assert.Equal("Never", Called.Never.ToString());
    }

    #endregion

    #region Equality Coverage

    [Fact]
    public void Equality_Works()
    {
        Assert.Equal(Called.Once, Called.Once);
        Assert.Equal(Called.Exactly(3), Called.Exactly(3));
        Assert.NotEqual(Called.Once, Called.Twice);
        Assert.NotEqual(Called.Exactly(3), Called.AtLeast(3));
    }

    [Fact]
    public void Equals_Object_Works()
    {
        Assert.True(Called.Once.Equals((object)Called.Once));
        Assert.False(Called.Once.Equals((object)Called.Twice));
        Assert.False(Called.Once.Equals(null));
        Assert.False(Called.Once.Equals("not a Called"));
    }

    [Fact]
    public void GetHashCode_SameForEqualValues()
    {
        Assert.Equal(Called.Once.GetHashCode(), Called.Exactly(1).GetHashCode());
        Assert.Equal(Called.Twice.GetHashCode(), Called.Exactly(2).GetHashCode());
    }

    [Fact]
    public void Operators_Work()
    {
        Assert.True(Called.Once == Called.Exactly(1));
        Assert.False(Called.Once != Called.Exactly(1));
        Assert.True(Called.Once != Called.Twice);
        Assert.False(Called.Once == Called.Twice);
    }

    #endregion
}
