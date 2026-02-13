// -----------------------------------------------------------------------------
// Design.Stubs - Default Interface Method (DIM) Stubs
// -----------------------------------------------------------------------------
// These stubs demonstrate the expected behavior when an interface member has
// a default implementation (DIM). When the member is NOT configured on the
// stub, calling it should execute the DIM — not return default(T).
//
// BUG: KnockOff currently returns default(T) for unconfigured DIM members
// on interface stubs. Class stubs correctly call base implementations.
// See Gap #12 in the Rocks gap analysis.
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.DefaultMethods;

[KnockOff<IDefaultMethodPolygon>]
[KnockOff<IDefaultPropertyPolygon>]
[KnockOff<IDefaultIndexerPolygon>]
public partial class DefaultMethodDemo
{
}
