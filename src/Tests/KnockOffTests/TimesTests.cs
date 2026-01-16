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
}
