// -----------------------------------------------------------------------------
// Design.Stubs - Generic Method Support for Class Stubs
// -----------------------------------------------------------------------------
// This file tests that generic virtual/abstract methods on class stubs
// generate compilable code with Of<T>() handler pattern.
//
// ACCEPTANCE CRITERIA:
// These stub declarations will only compile after the class stub pipeline
// supports generic method-level type parameters. Currently, generic methods
// are skipped by the `!method.IsGenericMethod` filter in ExtractClassInfo.
//
// AFFECTED PATTERNS:
// - Pattern 6: Inline Class ([KnockOff<GenericMethodBase>])
// - Pattern 3: Standalone Class ([KnockOffBase<GenericMethodBase>])
// - Pattern 4: Generic Standalone Class ([KnockOffBase(typeof(GenericMethodRepositoryBase<>))])
// - Pattern 9: Open Generic Class ([KnockOff(typeof(GenericMethodRepositoryBase<>))])
//
// EDGE CASES TESTED:
// - Mixed overloads: Process(string) + Process<T>(T, string) on GenericMethodBase
// - Class-level + method-level type params: GenericMethodRepositoryBase<TEntity>.ConvertEntity<TResult>()
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using KnockOff;

namespace Design.Stubs.Methods;

// =============================================================================
// PATTERN 6: INLINE CLASS STUB WITH GENERIC METHODS
// =============================================================================
//
// DESIGN DECISION: Generic methods on class stubs use the Of<T>() pattern,
// identical to interface stubs. The Impl class generates:
//
//   public override T Convert<T>(object value)
//   {
//       if (_stub == null) return base.Convert<T>(value);
//       var typedHandler = _stub.Convert.Of<T>();
//       typedHandler.RecordCall(value);
//       if (typedHandler.Callback is { } callback)
//           return callback(value);
//       return base.Convert<T>(value); // virtual fallback
//   }
//
// GENERATOR BEHAVIOR:
// - Generic method handler interceptor class (with Of<T>()) is generated
// - Impl class override includes <T> and constraint clauses
// - Virtual methods fall back to base.Method<T>(args) when unconfigured
// - Abstract methods return default! when unconfigured
//
// MIXED OVERLOAD: GenericMethodBase has both Process(string) and Process<T>(T, string).
// The builder must split these into separate interceptors:
// - Process (non-generic) -> UnifiedMethodInterceptorModel with Invoke
// - ProcessGeneric (generic) -> InlineGenericMethodHandlerModel with Of<T>()
//
// COMMON MISTAKE: Trying to use Return(value) directly on a generic method
// interceptor. Generic methods require Of<T>() first:
//   stub.Convert.Return(42);           // WRONG - Convert is a handler, not interceptor
//   stub.Convert.Of<int>().Return(v => 42);  // CORRECT - access typed handler first
// =============================================================================

[KnockOff<GenericMethodBase>]
#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
public partial class GenericMethodInlineClassTest
#pragma warning restore CA1052
{
}

// =============================================================================
// PATTERN 3: STANDALONE CLASS STUB WITH GENERIC METHODS
// =============================================================================
//
// DESIGN DECISION: Standalone class stubs with generic methods work identically
// to inline class stubs. The user's partial class provides interceptor properties
// via the generated code, and generic methods use Of<T>() handlers.
//
// Note: Stub override methods (MethodName_ pattern) are NOT supported for
// generic methods. This is consistent with the interface pipeline where
// generic methods do not support stub overrides.
// =============================================================================

[KnockOffBase<GenericMethodBase>]
public partial class GenericMethodStandaloneStub
{
}

// =============================================================================
// PATTERN 4: GENERIC STANDALONE CLASS WITH GENERIC METHODS
// =============================================================================
//
// This tests the interaction between class-level type parameters (TEntity)
// and method-level type parameters (TResult). The builder must distinguish:
// - TEntity flows from the class declaration -- NOT a generic method parameter
// - TResult flows from the method declaration -- IS a generic method parameter
//
// ACCEPTANCE CRITERIA: This will only compile after the standalone class pipeline
// handles method-level generic type parameters on a generic base class.
// =============================================================================

[KnockOffBase(typeof(GenericMethodRepositoryBase<>))]
public partial class GenericMethodRepositoryStub<TEntity> where TEntity : class
{
}

// =============================================================================
// PATTERN 9: OPEN GENERIC CLASS WITH GENERIC METHODS
// =============================================================================
//
// Same as Pattern 4 but using inline [KnockOff(typeof(...))] syntax.
// Tests that the inline class pipeline handles class-level + method-level
// type parameter interaction correctly.
//
// NOTE: Pattern 9 stubs are in a separate test class from Pattern 6 because
// the generator has separate pipelines for [KnockOff<T>] (closed generic) and
// [KnockOff(typeof(T<>))] (open generic). Mixing both attribute styles on a
// single class causes a duplicate hintName error in the generator.
// =============================================================================

[KnockOff(typeof(GenericMethodRepositoryBase<>))]
#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
public partial class GenericMethodOpenGenericClassTest
#pragma warning restore CA1052
{
}
