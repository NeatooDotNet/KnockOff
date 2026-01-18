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
        Assert.False(times.IsForever);
    }

    [Fact]
    public void Twice_HasCountOfTwo()
    {
        var times = Times.Twice;
        Assert.Equal(2, times.Count);
        Assert.False(times.IsForever);
    }

    [Fact]
    public void Exactly_HasSpecifiedCount()
    {
        var times = Times.Exactly(5);
        Assert.Equal(5, times.Count);
        Assert.False(times.IsForever);
    }

    [Fact]
    public void Forever_IsMarkedAsForever()
    {
        var times = Times.Forever;
        Assert.True(times.IsForever);
    }

    [Fact]
    public void AtLeast_ForVerification()
    {
        var times = Times.AtLeast(3);
        Assert.True(times.Verify(3));
        Assert.True(times.Verify(5));
        Assert.False(times.Verify(2));
    }

    [Fact]
    public void AtMost_ForVerification()
    {
        var times = Times.AtMost(3);
        Assert.True(times.Verify(0));
        Assert.True(times.Verify(3));
        Assert.False(times.Verify(4));
    }

    [Fact]
    public void Never_ForVerification()
    {
        var times = Times.Never;
        Assert.True(times.Verify(0));
        Assert.False(times.Verify(1));
    }

    #region Times.Verify() Gap Coverage

    [Fact]
    public void Exactly_Verify_ReturnsTrue_WhenCountMatches()
    {
        var times = Times.Exactly(3);
        Assert.True(times.Verify(3));
    }

    [Fact]
    public void Exactly_Verify_ReturnsFalse_WhenCountTooLow()
    {
        var times = Times.Exactly(3);
        Assert.False(times.Verify(2));
    }

    [Fact]
    public void Exactly_Verify_ReturnsFalse_WhenCountTooHigh()
    {
        var times = Times.Exactly(3);
        Assert.False(times.Verify(4));
    }

    [Fact]
    public void Once_Verify_ReturnsTrue_WhenCalledOnce()
    {
        var times = Times.Once;
        Assert.True(times.Verify(1));
    }

    [Fact]
    public void Once_Verify_ReturnsFalse_WhenNotCalled()
    {
        var times = Times.Once;
        Assert.False(times.Verify(0));
    }

    [Fact]
    public void Once_Verify_ReturnsFalse_WhenCalledTwice()
    {
        var times = Times.Once;
        Assert.False(times.Verify(2));
    }

    [Fact]
    public void Twice_Verify_ReturnsTrue_WhenCalledTwice()
    {
        var times = Times.Twice;
        Assert.True(times.Verify(2));
    }

    [Fact]
    public void Forever_Verify_AlwaysReturnsTrue()
    {
        var times = Times.Forever;
        Assert.True(times.Verify(0));
        Assert.True(times.Verify(1));
        Assert.True(times.Verify(100));
    }

    #endregion
}
