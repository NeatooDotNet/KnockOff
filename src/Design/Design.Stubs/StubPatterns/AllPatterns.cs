// -----------------------------------------------------------------------------
// Design.Stubs - All Six Stub Patterns Side-by-Side
// -----------------------------------------------------------------------------
// This file is part of the Design Source of Truth. It demonstrates ALL SIX
// stub patterns that KnockOff supports, with extensive documentation of
// when to use each pattern and what gets generated.
//
// THE SIX PATTERNS:
// 1. Standalone          - [KnockOff] partial class Stub : IService
// 1B. Generic Standalone - [KnockOff] partial class Stub<T> : IService<T>
// 2. Inline Interface    - [KnockOff<IService>]
// 3. Inline Class        - [KnockOff<ConcreteClass>]
// 4. Inline Delegate     - [KnockOff<DelegateType>]
// 5. Open Generic        - [KnockOff(typeof(T<>))]
//
// Note: Generic Standalone is numbered 1B because it is a variant of the
// Standalone pattern. User documentation numbers sequentially 1-6.
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using Design.Domain.Delegates;
using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.StubPatterns;

// =============================================================================
// PATTERN 1: STANDALONE STUB
// =============================================================================
// Use this pattern when:
// - You want a reusable stub class in its own file
// - You need to add custom methods or state to the stub
// - Multiple test classes will share the same stub
//
// DESIGN DECISION: The [KnockOff] attribute on a partial class that implements
// an interface triggers source generation. The partial keyword is REQUIRED -
// the generator adds the implementation to the other partial.
//
// GENERATOR BEHAVIOR: For this class:
//
//   [KnockOff]
//   public partial class CalculatorStub : ICalculator { }
//
// The generator produces (in CalculatorStub.g.cs):
//
//   public partial class CalculatorStub : IKnockOffStub
//   {
//       public bool Strict { get; set; }
//       public AddInterceptor Add { get; }
//       public SubtractInterceptor Subtract { get; }
//       public DivideInterceptor Divide { get; }
//       public ResetInterceptor Reset { get; }
//
//       int ICalculator.Add(int a, int b) => Add.Call((a, b));
//       int ICalculator.Subtract(int a, int b) => Subtract.Call((a, b));
//       int ICalculator.Divide(int a, int b) => Divide.Call((a, b));
//       void ICalculator.Reset() => Reset.Call();
//   }
// =============================================================================

[KnockOff]
public partial class CalculatorStub : ICalculator
{
    // DID NOT DO THIS: Require users to implement interface methods manually
    //
    // REJECTED PATTERN:
    //   public partial class CalculatorStub : ICalculator
    //   {
    //       public int Add(int a, int b) => throw new NotImplementedException();
    //   }
    //
    // WHY NOT: The whole point of KnockOff is to generate the implementation.
    // Users configure behavior via interceptors (stub.Add.Returns(42)), not
    // by writing the implementation code themselves.

    // Users can add custom methods or state to standalone stubs.
    // This is NOT possible with inline stubs.
    public bool WasUsed { get; set; }

    public void MarkUsed()
    {
        WasUsed = true;
    }
}

// =============================================================================
// PATTERN 1B: GENERIC STANDALONE STUB
// =============================================================================
//
// A generic standalone stub is a reusable stub class with type parameters that
// implements a generic interface. Like the non-generic standalone pattern, it
// lives in its own file and can be shared across multiple test classes.
//
// WHEN TO USE:
// - You need a reusable stub for a generic interface (IRepository<T>, IService<T>)
// - Multiple tests need the same generic stub with different type arguments
// - You want to share stub setup code across test classes
// - You need to add custom methods or state to a generic stub
// - You're writing a test fixture that parameterizes on a type
//
// SYNTAX:
//
//   [KnockOff]
//   public partial class RepositoryStub<T> : IRepository<T> where T : class { }
//
// The stub class must:
// - Have the [KnockOff] attribute
// - Be declared as `partial`
// - Implement a generic interface
// - Declare the same type parameters as the interface
// - Include the same constraints as the interface (where T : class, etc.)
//
// GENERATED CODE:
// For this class:
//
//   [KnockOff]
//   public partial class RepositoryStub<T> : IRepository<T> where T : class { }
//
// The generator produces (in RepositoryStub.g.cs):
//
//   public partial class RepositoryStub<T> : IKnockOffStub
//   {
//       public bool Strict { get; set; }
//       public GetByIdInterceptor GetById { get; }
//       public SaveInterceptor Save { get; }
//       public GetAllInterceptor GetAll { get; }
//       public CountInterceptor Count { get; }
//
//       // Nested interceptor classes use T from the outer class
//       public class GetByIdInterceptor : Interceptor<int, T?> { ... }
//       public class SaveInterceptor : Interceptor<T, ValueTuple> { ... }
//       public class GetAllInterceptor : Interceptor<ValueTuple, IEnumerable<T>> { ... }
//       public class CountInterceptor : PropertyInterceptor<int> { ... }
//
//       T? IRepository<T>.GetById(int id) => GetById.Call(id);
//       void IRepository<T>.Save(T entity) => Save.Call(entity);
//       IEnumerable<T> IRepository<T>.GetAll() => GetAll.Call();
//       int IRepository<T>.Count { get => Count.Get(); set => Count.Set(value); }
//   }
//
// USAGE IN TESTS:
//
//   // Create stubs with different type arguments
//   var userRepo = new RepositoryStub<User>();
//   var productRepo = new RepositoryStub<Product>();
//   var orderRepo = new RepositoryStub<Order>();
//
//   // Configure each stub independently
//   userRepo.GetById.OnCall((id) => new User { Id = id, Name = "Test" });
//   productRepo.GetById.OnCall((id) => new Product { Id = id, Price = 9.99m });
//
//   // Use as interface implementations
//   IRepository<User> userService = userRepo;
//   IRepository<Product> productService = productRepo;
//
//   // Verify calls per-instance
//   userRepo.GetById.Verify(Times.Once);
//
// VS OPEN GENERIC PATTERN (PATTERN 6):
//
//   | Aspect              | Generic Standalone                  | Open Generic                          |
//   |---------------------|-------------------------------------|---------------------------------------|
//   | Declaration         | [KnockOff] class Stub<T> : IFoo<T>  | [KnockOff(typeof(IFoo<>))]            |
//   | Location            | Separate file (reusable)            | Nested in test class                  |
//   | Instantiation       | new RepositoryStub<User>()          | new Stubs.IRepository<User>()         |
//   | Custom methods      | Yes (add to partial class)          | No                                    |
//   | Shared across tests | Yes                                 | No (scoped to containing class)       |
//   | Best for            | Test fixtures, shared utilities     | One-off generic interface tests       |
//
// DESIGN RATIONALE:
// Generic standalone stubs fill a gap between non-generic standalone stubs and
// open generic inline stubs. While open generic inline stubs are convenient for
// one-off usage, they cannot be shared across test classes or extended with
// custom methods. Generic standalone stubs provide the reusability and
// extensibility of standalone stubs while supporting generic type parameters.
//
// CONSTRAINT PROPAGATION:
// The generator preserves all type constraints from the interface. If the
// interface has `where T : class, IEntity, new()`, the stub class must also
// declare these constraints, and the generated code will enforce them.
//
// MULTIPLE TYPE PARAMETERS:
// Generic standalone stubs support any number of type parameters:
//
//   [KnockOff]
//   public partial class CacheStub<TKey, TValue> : ICache<TKey, TValue>
//       where TKey : notnull { }
//
//   var cache = new CacheStub<string, int>();
//
// =============================================================================

[KnockOff]
public partial class GenericServiceStub<T> : IGenericService<T> where T : class
{
    // Like non-generic standalone stubs, users can add custom methods or state.
    // This is NOT possible with open generic inline stubs.
    private readonly List<T> _savedEntities = [];
    public IReadOnlyList<T> SavedEntities => _savedEntities;

    public void TrackSave(T entity)
    {
        _savedEntities.Add(entity);
    }
}

// =============================================================================
// PATTERN 2: INLINE INTERFACE STUB
// =============================================================================
// Use this pattern when:
// - You want stubs scoped to a single test class
// - You don't need custom stub methods
// - You prefer fewer files in your test project
//
// DESIGN DECISION: The [KnockOff<T>] attribute on a partial class generates
// a nested `Stubs` class containing a stub for the specified interface.
//
// GENERATOR BEHAVIOR: For this declaration:
//
//   [KnockOff<ICalculator>]
//   public partial class InlineInterfaceExample { }
//
// The generator produces:
//
//   public partial class InlineInterfaceExample
//   {
//       public static partial class Stubs
//       {
//           public partial class ICalculator : global::Design.Domain.Services.ICalculator, IKnockOffStub
//           {
//               public bool Strict { get; set; }
//               public AddInterceptor Add { get; }
//               // ... interceptors for all interface members
//           }
//       }
//   }
//
// COMMON MISTAKE: Forgetting the `partial` keyword on the containing class.
// The attribute will be recognized but generation will fail.
// =============================================================================

[KnockOff<ICalculator>]
public partial class InlineInterfaceExample
{
    // DID NOT DO THIS: Generate stubs directly in the containing class
    //
    // REJECTED PATTERN:
    //   [KnockOff<ICalculator>]
    //   public partial class InlineInterfaceExample
    //   {
    //       // Stub properties added directly here
    //       public ICalculatorStub Calculator { get; }
    //   }
    //
    // WHY NOT: Nested Stubs class provides clear namespace isolation.
    // Multiple [KnockOff<T>] attributes can coexist without name collisions.
    // Access pattern: new Stubs.ICalculator()

    public void Example_InlineInterfaceUsage()
    {
        // Instantiation pattern for inline interface stubs
        var stub = new Stubs.ICalculator();

        // The stub IS the interface implementation
        ICalculator calculator = stub;

        // Configure via interceptors
        stub.Add.Returns(42);
    }
}

// =============================================================================
// PATTERN 3: INLINE CLASS STUB
// =============================================================================
// Use this pattern when:
// - You need to stub virtual/abstract members of a concrete class
// - The class has a protected constructor or other constraints
// - You want base class behavior for unconfigured virtual methods
//
// DESIGN DECISION: Class stubs differ fundamentally from interface stubs:
// - The generated stub EXTENDS the base class
// - Unconfigured virtual methods CALL BASE, not smart default
// - Access the wrapped instance via .Object property
//
// GENERATOR BEHAVIOR: For this declaration:
//
//   [KnockOff<ServiceBase>]
//   public partial class InlineClassExample { }
//
// The generator produces:
//
//   public partial class InlineClassExample
//   {
//       public static partial class Stubs
//       {
//           public partial class ServiceBase : IKnockOffStub
//           {
//               private readonly global::Design.Domain.Abstractions.ServiceBase _object;
//               public global::Design.Domain.Abstractions.ServiceBase Object => _object;
//
//               public NameInterceptor Name { get; }
//               public IsEnabledInterceptor IsEnabled { get; }
//               public InitializeInterceptor Initialize { get; }
//               public ExecuteInterceptor Execute { get; }
//           }
//
//           private class ServiceBase_Generated : global::Design.Domain.Abstractions.ServiceBase
//           {
//               // Internal class that extends the base and delegates to interceptors
//           }
//       }
//   }
//
// PATTERN COMPARISON with Moq:
// - Moq: mock.Object returns the proxy for both interfaces AND classes
// - KnockOff interface: stub IS the implementation (no .Object needed)
// - KnockOff class: stub.Object returns the wrapped instance
// =============================================================================

[KnockOff<ServiceBase>]
public partial class InlineClassExample
{
    public void Example_InlineClassUsage()
    {
        // Instantiation pattern for inline class stubs
        var stub = new Stubs.ServiceBase();

        // IMPORTANT: .Object gives you the actual class instance
        ServiceBase service = stub.Object;

        // Configure via interceptors
        stub.Name.OnGet("TestService");
        stub.Execute.OnCall((cmd) => { /* handle command */ });

        // Unconfigured virtual methods call base implementation
        stub.Object.Initialize(); // Calls ServiceBase.Initialize()
    }

    // DID NOT DO THIS: Make class stubs work identically to interface stubs
    //
    // REJECTED PATTERN:
    //   var stub = new Stubs.ServiceBase();
    //   ServiceBase service = stub; // Implicit conversion
    //
    // WHY NOT: The stub wrapper and the actual instance are different objects.
    // The stub holds interceptors; the instance is the generated class that
    // extends ServiceBase. Forcing implicit conversion would be confusing.
}

// =============================================================================
// PATTERN 4: INLINE DELEGATE STUB
// =============================================================================
// Use this pattern when:
// - You need to stub a named delegate type
// - You want to verify the delegate was invoked with specific arguments
// - You need sequence behavior for delegate calls
//
// DESIGN DECISION: KnockOff supports NAMED delegate types, not generic Func<>
// or Action<> types. The stub class name matches the delegate type name.
//
// DID NOT DO THIS: Support generic Func<>/Action<> types directly
//
// REJECTED PATTERN:
//   [KnockOff<Func<int, int, int>>]  // Does NOT work
//   [KnockOff<Action<string>>]       // Does NOT work
//
// WHY NOT: Generic delegate types like Func<> and Action<> don't have
// simple names - their CLR names include generic arity markers. Using
// named delegate types is clearer and more explicit.
//
// GENERATOR BEHAVIOR: For this declaration:
//
//   public delegate int ArithmeticOperation(int a, int b);
//
//   [KnockOff<ArithmeticOperation>]
//   public partial class InlineDelegateExample { }
//
// The generator produces:
//
//   public partial class InlineDelegateExample
//   {
//       public static partial class Stubs
//       {
//           public partial class ArithmeticOperation : IKnockOffStub
//           {
//               public Interceptor Interceptor { get; }
//
//               public static implicit operator ArithmeticOperation(ArithmeticOperation stub)
//                   => (a, b) => stub.Interceptor.Call((a, b));
//           }
//       }
//   }
// =============================================================================

[KnockOff<ArithmeticOperation>]
[KnockOff<LogAction>]
public partial class InlineDelegateExample
{
    public void Example_InlineDelegateUsage()
    {
        // Instantiation pattern for inline delegate stubs
        var addStub = new Stubs.ArithmeticOperation();
        var logStub = new Stubs.LogAction();

        // Implicit conversion to delegate type
        ArithmeticOperation addFunc = addStub;
        LogAction logAction = logStub;

        // Configure via .Interceptor property
        addStub.Interceptor.OnCall((a, b) => a + b);
        logStub.Interceptor.OnCall((msg) => Console.WriteLine(msg));

        // DID NOT DO THIS: Allow direct configuration on the stub
        //
        // REJECTED PATTERN:
        //   addStub.OnCall((a, b) => a + b);
        //
        // WHY NOT: Delegates have only one "member" (the invocation).
        // Using .Interceptor makes it explicit and consistent with the
        // interceptor pattern used elsewhere.
    }
}

// =============================================================================
// PATTERN 6: OPEN GENERIC STUB (via typeof syntax)
// =============================================================================
// Use this pattern when:
// - You need to stub an open generic interface like IRepository<T>
// - You want a single stub definition that works with any type argument
//
// DESIGN DECISION: Open generics require typeof() syntax because C# doesn't
// allow [KnockOff<IRepository<>>] - the type argument must be closed.
//
// GENERATOR BEHAVIOR: For this declaration:
//
//   [KnockOff(typeof(IRepository<>))]
//   public partial class OpenGenericExample { }
//
// The generator produces a stub class with the same generic parameters:
//
//   public static partial class Stubs
//   {
//       public partial class IRepository<T> : global::Design.Domain.Services.IRepository<T>
//       {
//           // Interceptors with generic type T
//       }
//   }
// =============================================================================

[KnockOff(typeof(IRepository<>))]
public partial class OpenGenericExample
{
    public void Example_OpenGenericUsage()
    {
        // Open generic stubs are instantiated with a concrete type argument
        var stringRepo = new Stubs.IRepository<string>();
        var entityRepo = new Stubs.IRepository<DataEventArgs>();

        // Configure like any other stub
        stringRepo.GetById.OnCall((id) => $"Item-{id}");
        entityRepo.GetById.OnCall((id) => new DataEventArgs($"Data-{id}"));

        // DID NOT DO THIS: Require separate [KnockOff<IRepository<string>>] for each type
        //
        // REJECTED PATTERN:
        //   [KnockOff<IRepository<string>>]
        //   [KnockOff<IRepository<DataEventArgs>>]
        //   public partial class MultipleRepoExample { }
        //
        // WHY NOT: Open generic stubs are more flexible - one declaration covers all
        // possible type arguments. The typeof() syntax enables this pattern.
    }
}

// =============================================================================
// DESIGN DECISION SUMMARY
// =============================================================================
//
// DESIGN DECISION: Six distinct patterns serve different use cases:
//
// STANDALONE PATTERNS (file-based, reusable across tests):
// 1. Standalone: Reusable stubs, custom methods/state, shared across tests
// 1B. Generic Standalone: Same as Standalone but with generic type parameters
//
// INLINE PATTERNS (nested within test class):
// 2. Inline Interface: Scoped to test class, fewer files, no custom methods
// 3. Inline Class: Virtual/abstract members, base class fallback behavior
// 4. Inline Delegate: Named delegates with .Interceptor configuration
// 5. Open Generic: Generic nested stubs from open generic types (typeof(T<>))
//
// KEY TRADE-OFF: Standalone vs Open Generic for generic interfaces:
// - Generic Standalone: Reusable, can add custom methods, lives in own file
// - Open Generic: Quick one-off usage, nested in test class, no custom methods
//
// DESIGN DECISION: Inline stubs generate a nested `Stubs` class to isolate
// generated types from user code. Multiple [KnockOff<T>] attributes on the
// same class create multiple stub types in the same Stubs container.
//
// DESIGN DECISION: Explicit interface implementation for generated methods
// (e.g., `int ICalculator.Add(...)`) keeps interceptor properties visible
// while hiding the actual interface method implementations from IntelliSense.
//
// DESIGN DECISION: Class stubs use .Object property because:
// 1. The stub wrapper holds interceptors and configuration
// 2. The actual instance is a generated class extending the base
// 3. These are conceptually different objects with different purposes
// =============================================================================

// =============================================================================
// COMMON MISTAKES
// =============================================================================
//
// COMMON MISTAKE: Forgetting `partial` keyword
// [KnockOff] on non-partial class = no code generated
// Always use: `public partial class MyStub : IMyInterface`
//
// COMMON MISTAKE: Using Func<>/Action<> instead of named delegates
// [KnockOff<Func<int, int>>] doesn't work - define a named delegate type
//
// COMMON MISTAKE: Casting inline class stub directly instead of using .Object
// `ServiceBase svc = stub;` doesn't compile
// Use: `ServiceBase svc = stub.Object;`
//
// COMMON MISTAKE: Expecting inline stubs to support custom methods
// Inline stubs are generated entirely - user cannot add methods.
// Use standalone pattern if you need custom stub methods.
// =============================================================================

// =============================================================================
// PRIORITY ORDER
// =============================================================================
// When a stubbed member is called, KnockOff resolves the return value in this order:
//
// 1. When chains - Parameter-specific matching (highest priority)
//    stub.Add.When(1, 2).Returns(100);
//
// 2. Sequences - If OnCall().ThenCall() was used and not exhausted
//    stub.Add.OnCall((a, b) => 1).ThenCall((a, b) => 2);
//
// 3. Returns - Simple constant return value
//    stub.Add.Returns(42);
//
// 4. OnCall - Callback invocation (mutually exclusive with Returns)
//    stub.Add.OnCall((a, b) => a + b);
//
// 5. Source - Delegation to real implementation
//    stub.Source(realCalculator);
//
// 6. Smart Default - default(T) for value types, null for references
//    (or StubException in strict mode)
// =============================================================================
