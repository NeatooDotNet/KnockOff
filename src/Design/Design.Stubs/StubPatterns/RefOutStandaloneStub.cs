// -----------------------------------------------------------------------------
// Design.Stubs - Standalone Stub with Ref/Out Parameters
// -----------------------------------------------------------------------------
// This file demonstrates the standalone stub pattern (Pattern 1) with
// interfaces that have ref/out parameters.
//
// DESIGN DECISION: Standalone stubs work identically to inline stubs for
// ref/out parameters. The generator creates custom delegates and the
// Return()/Call() APIs accept those delegates.
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.StubPatterns;

// =============================================================================
// PATTERN 1: STANDALONE STUB WITH OUT PARAMETERS
// =============================================================================

[KnockOff]
public partial class OutParameterStub : IOutParameterService { }

// =============================================================================
// PATTERN 1: STANDALONE STUB WITH REF PARAMETERS
// =============================================================================

[KnockOff]
public partial class RefParameterStub : IRefParameterService { }

// =============================================================================
// PATTERN 1: STANDALONE STUB WITH MIXED REF/OUT PARAMETERS
// =============================================================================

[KnockOff]
public partial class MixedRefOutStub : IMixedRefOutService { }
