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

#region docs:events:basic-interface
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

#region docs:events:testing-viewmodel
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

#region docs:events:progress-reporting
[KnockOff]
public partial class GuideDownloaderKnockOff : IGuideDownloader { }
#endregion
