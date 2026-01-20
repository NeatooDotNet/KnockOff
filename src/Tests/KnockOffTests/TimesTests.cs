using KnockOff;
using Xunit;

namespace KnockOffTests;

public class TimesTests
{
    [Fact]
    public void Once_HasCountOfOne()
    {
        var times = Times.Once;
        Assert.Equal(1, times.Count);
    }

    [Fact]
    public void Twice_HasCountOfTwo()
    {
        var times = Times.Twice;
        Assert.Equal(2, times.Count);
    }

    [Fact]
    public void Exactly_HasSpecifiedCount()
    {
        var times = Times.Exactly(5);
        Assert.Equal(5, times.Count);
    }

    [Fact]
    public void AtLeast_ForVerification()
    {
        var times = Times.AtLeast(3);
        Assert.True(times.Validate(3));
        Assert.True(times.Validate(5));
        Assert.False(times.Validate(2));
    }

    [Fact]
    public void AtLeastOnce_ForVerification()
    {
        var times = Times.AtLeastOnce;
        Assert.True(times.Validate(1));
        Assert.True(times.Validate(5));
        Assert.False(times.Validate(0));
    }

    [Fact]
    public void AtMost_ForVerification()
    {
        var times = Times.AtMost(3);
        Assert.True(times.Validate(0));
        Assert.True(times.Validate(3));
        Assert.False(times.Validate(4));
    }

    [Fact]
    public void Never_ForVerification()
    {
        var times = Times.Never;
        Assert.True(times.Validate(0));
        Assert.False(times.Validate(1));
    }

    #region Times.Validate() Coverage

    [Fact]
    public void Exactly_Validate_ReturnsTrue_WhenCountMatches()
    {
        var times = Times.Exactly(3);
        Assert.True(times.Validate(3));
    }

    [Fact]
    public void Exactly_Validate_ReturnsFalse_WhenCountTooLow()
    {
        var times = Times.Exactly(3);
        Assert.False(times.Validate(2));
    }

    [Fact]
    public void Exactly_Validate_ReturnsFalse_WhenCountTooHigh()
    {
        var times = Times.Exactly(3);
        Assert.False(times.Validate(4));
    }

    [Fact]
    public void Once_Validate_ReturnsTrue_WhenCalledOnce()
    {
        var times = Times.Once;
        Assert.True(times.Validate(1));
    }

    [Fact]
    public void Once_Validate_ReturnsFalse_WhenNotCalled()
    {
        var times = Times.Once;
        Assert.False(times.Validate(0));
    }

    [Fact]
    public void Once_Validate_ReturnsFalse_WhenCalledTwice()
    {
        var times = Times.Once;
        Assert.False(times.Validate(2));
    }

    [Fact]
    public void Twice_Validate_ReturnsTrue_WhenCalledTwice()
    {
        var times = Times.Twice;
        Assert.True(times.Validate(2));
    }

    #endregion

    #region ToString Coverage

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        Assert.Equal("Once", Times.Once.ToString());
        Assert.Equal("Twice", Times.Twice.ToString());
        Assert.Equal("Exactly(5)", Times.Exactly(5).ToString());
        Assert.Equal("AtLeastOnce", Times.AtLeastOnce.ToString());
        Assert.Equal("AtLeast(3)", Times.AtLeast(3).ToString());
        Assert.Equal("AtMost(5)", Times.AtMost(5).ToString());
        Assert.Equal("Never", Times.Never.ToString());
    }

    #endregion

    #region Equality Coverage

    [Fact]
    public void Equality_Works()
    {
        Assert.Equal(Times.Once, Times.Once);
        Assert.Equal(Times.Exactly(3), Times.Exactly(3));
        Assert.NotEqual(Times.Once, Times.Twice);
        Assert.NotEqual(Times.Exactly(3), Times.AtLeast(3));
    }

    [Fact]
    public void Equals_Object_Works()
    {
        Assert.True(Times.Once.Equals((object)Times.Once));
        Assert.False(Times.Once.Equals((object)Times.Twice));
        Assert.False(Times.Once.Equals(null));
        Assert.False(Times.Once.Equals("not a Times"));
    }

    [Fact]
    public void GetHashCode_SameForEqualValues()
    {
        Assert.Equal(Times.Once.GetHashCode(), Times.Exactly(1).GetHashCode());
        Assert.Equal(Times.Twice.GetHashCode(), Times.Exactly(2).GetHashCode());
    }

    [Fact]
    public void Operators_Work()
    {
        Assert.True(Times.Once == Times.Exactly(1));
        Assert.False(Times.Once != Times.Exactly(1));
        Assert.True(Times.Once != Times.Twice);
        Assert.False(Times.Once == Times.Twice);
    }

    #endregion
}
