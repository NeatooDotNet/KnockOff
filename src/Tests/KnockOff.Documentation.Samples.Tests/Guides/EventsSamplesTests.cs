using KnockOff.Documentation.Samples.Guides;

namespace KnockOff.Documentation.Samples.Tests.Guides;

/// <summary>
/// Tests for docs/guides/events.md samples.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "Guides")]
public class EventsSamplesTests
{
    // ========================================================================
    // ViewModel Event Testing
    // ========================================================================

    [Fact]
    public void ViewModel_SubscribesToEvents()
    {
        var knockOff = new GuideDataServiceKnockOff();
        IGuideDataService service = knockOff;

        var viewModel = new GuideViewModel(service);

        Assert.True(knockOff.DataChanged.HasSubscribers);
        Assert.Equal(1, knockOff.DataChanged.AddCount);
    }

    [Fact]
    public void ViewModel_UpdatesOnDataChanged()
    {
        var knockOff = new GuideDataServiceKnockOff();
        IGuideDataService service = knockOff;
        var viewModel = new GuideViewModel(service);

        knockOff.DataChanged.Raise(null, new DataChangedEventArgs { NewValue = 42 });

        Assert.Equal(42, viewModel.CurrentValue);
    }

    [Fact]
    public void ViewModel_Dispose_UnsubscribesFromEvents()
    {
        var knockOff = new GuideDataServiceKnockOff();
        IGuideDataService service = knockOff;
        var viewModel = new GuideViewModel(service);

        Assert.Equal(1, knockOff.DataChanged.AddCount);
        Assert.True(knockOff.DataChanged.HasSubscribers);

        viewModel.Dispose();

        Assert.Equal(1, knockOff.DataChanged.RemoveCount);
        Assert.False(knockOff.DataChanged.HasSubscribers);
    }

    // ========================================================================
    // Basic Event Tracking
    // ========================================================================

    [Fact]
    public void Event_TracksSubscription()
    {
        var knockOff = new GuideEventSourceKnockOff();
        IGuideEventSource source = knockOff;

        Assert.False(knockOff.MessageReceived.HasSubscribers);

        source.MessageReceived += (s, e) => { };

        Assert.True(knockOff.MessageReceived.HasSubscribers);
        Assert.Equal(1, knockOff.MessageReceived.AddCount);
    }

    [Fact]
    public void Event_RaisesWithData()
    {
        var knockOff = new GuideEventSourceKnockOff();
        IGuideEventSource source = knockOff;
        string? received = null;

        source.MessageReceived += (sender, e) => received = e;
        knockOff.MessageReceived.Raise(null, "Test");

        Assert.Equal("Test", received);
    }

    [Fact]
    public void Event_ActionRaises()
    {
        var knockOff = new GuideEventSourceKnockOff();
        IGuideEventSource source = knockOff;
        int? progress = null;

        source.OnProgress += (value) => progress = value;
        knockOff.OnProgress.Raise(75);

        Assert.Equal(75, progress);
    }

    [Fact]
    public void Event_ResetClearsHandlers()
    {
        var knockOff = new GuideEventSourceKnockOff();
        IGuideEventSource source = knockOff;
        var invoked = false;

        source.MessageReceived += (s, e) => invoked = true;
        Assert.True(knockOff.MessageReceived.HasSubscribers);

        knockOff.MessageReceived.Reset();

        Assert.False(knockOff.MessageReceived.HasSubscribers);
        Assert.Equal(0, knockOff.MessageReceived.AddCount);

        // Handler was removed - raise does nothing
        knockOff.MessageReceived.Raise(null, "After");
        Assert.False(invoked);
    }
}
