/// <summary>
/// Code samples for docs/guides/events.md
///
/// Snippets in this file:
/// - docs:events:basic-interface
/// - docs:events:subscription-tracking
/// - docs:events:eventhandler-raise
/// - docs:events:eventhandler-noargs
/// - docs:events:action-raise
/// - docs:events:action-multi
/// - docs:events:raise-tracking
/// - docs:events:reset
/// - docs:events:clear
/// - docs:events:no-subscribers
/// - docs:events:multiple-subscribers
///
/// Corresponding tests: EventsSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Guides;

// ============================================================================
// Basic Usage
// ============================================================================

#region events-basic-interface
public interface IGuideEventSource
{
    event EventHandler<string>? MessageReceived;
    event EventHandler? OnCompleted;
    event Action<int>? OnProgress;
    event Action<string, int>? OnData;
}

[KnockOff]
public partial class GuideEventSourceKnockOff : IGuideEventSource { }
#endregion

// ============================================================================
// Data Changed Event for ViewModel Testing
// ============================================================================

public class DataChangedEventArgs : EventArgs
{
    public int NewValue { get; set; }
}

public interface IGuideDataService
{
    event EventHandler<DataChangedEventArgs>? DataChanged;
}

#region events-testing-viewmodel
[KnockOff]
public partial class GuideDataServiceKnockOff : IGuideDataService { }
#endregion

// ============================================================================
// Progress Reporting
// ============================================================================

public interface IGuideDownloader
{
    event Action<int>? ProgressChanged;
}

#region events-progress-reporting
[KnockOff]
public partial class GuideDownloaderKnockOff : IGuideDownloader { }
#endregion

// ============================================================================
// Usage Examples
// ============================================================================

/// <summary>
/// Usage examples demonstrating event patterns.
/// Each method is compilable; snippets extract key portions.
/// </summary>
public static class EventsUsageExamples
{
    public static void SubscriptionTracking()
    {
        var knockOff = new GuideEventSourceKnockOff();
        IGuideEventSource source = knockOff;

        #region events-subscription-tracking
        // Initially no subscribers
        var hasSubscribers = knockOff.MessageReceived.HasSubscribers;  // false
        var addCount = knockOff.MessageReceived.AddCount;              // 0

        // Subscribe
        EventHandler<string> handler = (sender, e) => Console.WriteLine(e);
        source.MessageReceived += handler;

        hasSubscribers = knockOff.MessageReceived.HasSubscribers;  // true
        addCount = knockOff.MessageReceived.AddCount;              // 1

        // Unsubscribe
        source.MessageReceived -= handler;

        var removeCount = knockOff.MessageReceived.RemoveCount;    // 1
        #endregion

        _ = (hasSubscribers, addCount, removeCount);
    }

    public static void EventHandlerRaise()
    {
        var knockOff = new GuideEventSourceKnockOff();
        IGuideEventSource source = knockOff;

        #region events-eventhandler-raise
        string? received = null;
        source.MessageReceived += (sender, e) => received = e;

        // Raise always requires sender parameter
        knockOff.MessageReceived.Raise(null, "Hello");

        // received is now "Hello"
        #endregion

        _ = received;
    }

    public static void EventHandlerNoArgs()
    {
        var knockOff = new GuideEventSourceKnockOff();
        IGuideEventSource source = knockOff;

        #region events-eventhandler-noargs
        var invoked = false;
        source.OnCompleted += (sender, e) => invoked = true;

        // Raise requires sender and EventArgs
        knockOff.OnCompleted.Raise(null, EventArgs.Empty);

        // invoked is now true
        #endregion

        _ = invoked;
    }

    public static void ActionRaise()
    {
        var knockOff = new GuideEventSourceKnockOff();
        IGuideEventSource source = knockOff;

        #region events-action-raise
        int? progress = null;
        source.OnProgress += (value) => progress = value;

        knockOff.OnProgress.Raise(75);

        // progress is now 75
        #endregion

        _ = progress;
    }

    public static void ActionMultiParam()
    {
        var knockOff = new GuideEventSourceKnockOff();
        IGuideEventSource source = knockOff;

        #region events-action-multi
        string? name = null;
        int? value = null;
        source.OnData += (n, v) => { name = n; value = v; };

        knockOff.OnData.Raise("Temperature", 72);

        // name is "Temperature", value is 72
        #endregion

        _ = (name, value);
    }

    public static void TrackingProperties()
    {
        var knockOff = new GuideEventSourceKnockOff();
        IGuideEventSource source = knockOff;

        source.MessageReceived += (s, e) => { };
        source.MessageReceived += (s, e) => { };
        source.MessageReceived -= (s, e) => { };

        #region events-tracking-properties
        // Subscription tracking
        var addCount = knockOff.MessageReceived.AddCount;        // Times += was called
        var removeCount = knockOff.MessageReceived.RemoveCount;  // Times -= was called
        var hasSubs = knockOff.MessageReceived.HasSubscribers;   // At least one handler attached
        #endregion

        _ = (addCount, removeCount, hasSubs);
    }

    public static void ResetBehavior()
    {
        var knockOff = new GuideEventSourceKnockOff();
        IGuideEventSource source = knockOff;

        #region events-reset
        var invoked = false;
        source.MessageReceived += (s, e) => invoked = true;

        // Before reset
        var addCountBefore = knockOff.MessageReceived.AddCount;        // 1
        var hasSubsBefore = knockOff.MessageReceived.HasSubscribers;   // true

        knockOff.MessageReceived.Reset();

        // After reset - tracking cleared
        var addCountAfter = knockOff.MessageReceived.AddCount;         // 0
        var removeCountAfter = knockOff.MessageReceived.RemoveCount;   // 0
        var hasSubsAfter = knockOff.MessageReceived.HasSubscribers;    // false

        // Handlers also cleared - raise does nothing
        invoked = false;
        knockOff.MessageReceived.Raise(null, "After");
        // invoked is still false - handler was removed by Reset
        #endregion

        _ = (addCountBefore, hasSubsBefore, addCountAfter, removeCountAfter, hasSubsAfter, invoked);
    }

    public static void NoSubscribersSafe()
    {
        #region events-no-subscribers
        var knockOff = new GuideEventSourceKnockOff();

        // No exception - safe to raise with no handlers
        knockOff.MessageReceived.Raise(null, "No one listening");
        #endregion
    }

    public static void MultipleSubscribers()
    {
        var knockOff = new GuideEventSourceKnockOff();
        IGuideEventSource source = knockOff;

        #region events-multiple-subscribers
        var received = new List<string>();
        source.MessageReceived += (s, e) => received.Add($"Handler1: {e}");
        source.MessageReceived += (s, e) => received.Add($"Handler2: {e}");
        source.MessageReceived += (s, e) => received.Add($"Handler3: {e}");

        var addCount = knockOff.MessageReceived.AddCount;  // 3

        knockOff.MessageReceived.Raise(null, "Test");

        // received contains ["Handler1: Test", "Handler2: Test", "Handler3: Test"]
        #endregion

        _ = (addCount, received);
    }

    public static void ProgressReporting()
    {
        var knockOff = new GuideDownloaderKnockOff();

        #region events-progress-example
        var progressValues = new List<int>();
        ((IGuideDownloader)knockOff).ProgressChanged += (p) => progressValues.Add(p);

        // Simulate download progress
        knockOff.ProgressChanged.Raise(0);
        knockOff.ProgressChanged.Raise(25);
        knockOff.ProgressChanged.Raise(50);
        knockOff.ProgressChanged.Raise(75);
        knockOff.ProgressChanged.Raise(100);

        // progressValues is [0, 25, 50, 75, 100]
        #endregion

        _ = progressValues;
    }
}
