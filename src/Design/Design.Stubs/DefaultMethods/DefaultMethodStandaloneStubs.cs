// -----------------------------------------------------------------------------
// Design.Stubs - Standalone DIM Stubs
// -----------------------------------------------------------------------------
// Standalone stubs for interfaces with Default Interface Methods (DIMs).
// Verifies that the DIM shim pattern works for the standalone (flat) pipeline.
//
// BUG: Same issue as inline stubs - DIM members return default(T) instead of
// executing the DIM. See Gap #12.
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.DefaultMethods;

[KnockOff]
public partial class DefaultMethodPolygonStub : IDefaultMethodPolygon { }

[KnockOff]
public partial class DefaultPropertyPolygonStub : IDefaultPropertyPolygon { }

[KnockOff]
public partial class DefaultIndexerPolygonStub : IDefaultIndexerPolygon { }
