// -----------------------------------------------------------------------------
// Design.Stubs - All Nine Stub Patterns Side-by-Side
// -----------------------------------------------------------------------------
// This file is part of the Design Source of Truth. It demonstrates ALL NINE
// stub patterns that KnockOff supports, with extensive documentation of
// when to use each pattern and what gets generated.
//
// THE NINE PATTERNS:
// 1. Standalone              - [KnockOff] partial class Stub : IService
// 2. Generic Standalone      - [KnockOff] partial class Stub<T> : IService<T>
// 3. Standalone Class        - [KnockOffBase<ConcreteClass>] partial class Stub
// 4. Generic Standalone Class- [KnockOffBase(typeof(ClassBase<>))] partial class Stub<T>
// 5. Inline Interface        - [KnockOff<IService>]
// 6. Inline Class            - [KnockOff<ConcreteClass>]
// 7. Inline Delegate         - [KnockOff<DelegateType>]
// 8. Open Generic Interface  - [KnockOff(typeof(IService<>))]
// 9. Open Generic Class      - [KnockOff(typeof(ServiceBase<>))]
//
// Note: Patterns 1-4 are Standalone (file-based, reusable across tests)
//       Patterns 5-9 are Inline (nested within test class)
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
// PATTERN 2: GENERIC STANDALONE STUB
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
// PATTERN 3: STANDALONE CLASS STUB
// =============================================================================
// Use this pattern when:
// - You need a reusable stub for a class (not interface) across test files
// - You want to add custom methods or state to the stub
// - The class has virtual or abstract members you want to intercept
// - You prefer explicit, discoverable stub classes in IntelliSense
//
// DESIGN DECISION: The [KnockOffBase<T>] attribute on a partial class generates
// a stub for a class (not interface). Like inline class stubs, standalone class
// stubs use the composition pattern with .Object property.
//
// GENERATOR BEHAVIOR: For this class:
//
//   [KnockOffBase<ServiceBase>]
//   public partial class ServiceStub { }
//
// The generator produces (in ServiceStub.g.cs):
//
//   public partial class ServiceStub : IKnockOffStub
//   {
//       private readonly global::ServiceBase _object;
//       public global::ServiceBase Object => _object;
//
//       public bool Strict { get; set; }
//       public PropertyInterceptor<string> Name { get; }
//       public MethodInterceptor<ValueTuple> Initialize { get; }
//       public MethodInterceptor<string, ValueTuple> Execute { get; }
//
//       private class Impl : global::ServiceBase
//       {
//           // Extends base class and delegates to interceptors
//       }
//   }
//
// KEY DIFFERENCE FROM STANDALONE INTERFACE:
// - Standalone Interface: stub IS the implementation (implements interface)
// - Standalone Class: stub.Object IS the implementation (composition pattern)
//
// PATTERN COMPARISON:
// - Standalone Interface: [KnockOff] on class implementing interface, no .Object
// - Standalone Class: [KnockOffBase<T>] on class, requires .Object property
// - Inline Class: [KnockOff<ConcreteClass>], nested in test class, uses .Object
// =============================================================================

[KnockOffBase<ServiceBase>]
public partial class StandaloneServiceStub
{
    // DID NOT DO THIS: Make standalone class stubs inherit from the target class
    //
    // REJECTED PATTERN:
    //   [KnockOffBase<ServiceBase>]
    //   public partial class StandaloneServiceStub : ServiceBase { }
    //
    // WHY NOT: The stub wrapper and the actual instance are different objects.
    // The stub holds interceptors; the instance is the generated nested Impl class
    // that extends ServiceBase. This is the same pattern as inline class stubs.

    // Users can add custom methods or state to standalone class stubs.
    // This is NOT possible with inline class stubs.
    public int CallCount { get; set; }

    public void IncrementCallCount()
    {
        CallCount++;
    }
}

// =============================================================================
// PATTERN 4: GENERIC STANDALONE CLASS STUB
// =============================================================================
//
// A generic standalone class stub is a reusable stub class with type parameters
// for stubbing generic base classes. Like standalone class stubs, it uses the
// composition pattern with .Object property.
//
// WHEN TO USE:
// - You need a reusable stub for a generic class (Repository<T>, ServiceBase<T>)
// - Multiple tests need the same generic class stub with different type arguments
// - You want to share stub setup code across test classes
// - You need to add custom methods or state to a generic class stub
// - The class has virtual or abstract members you want to intercept
//
// SYNTAX:
//
//   [KnockOffBase(typeof(ServiceBase<>))]
//   public partial class ServiceStub<T> where T : class { }
//
// The stub class must:
// - Have the [KnockOffBase(typeof(T<>))] attribute with open generic
// - Be declared as `partial`
// - Declare the same type parameters as the base class
// - Include the same constraints as the base class (where T : class, etc.)
//
// GENERATED CODE:
// For this class:
//
//   [KnockOffBase(typeof(RepositoryBase<>))]
//   public partial class RepositoryStub<T> where T : class { }
//
// The generator produces (in RepositoryStub.g.cs):
//
//   public partial class RepositoryStub<T> : IKnockOffStub where T : class
//   {
//       private readonly global::RepositoryBase<T> _object;
//       public global::RepositoryBase<T> Object => _object;
//
//       public bool Strict { get; set; }
//       public MethodInterceptor<int, T?> GetById { get; }
//       public MethodInterceptor<T, ValueTuple> Save { get; }
//
//       // Nested Impl class extends base and uses T from outer class
//       private class Impl : global::RepositoryBase<T>
//       {
//           // Overrides virtual/abstract members and delegates to interceptors
//       }
//   }
//
// USAGE IN TESTS:
//
//   // Create stubs with different type arguments
//   var userRepo = new RepositoryStub<User>();
//   var productRepo = new RepositoryStub<Product>();
//
//   // Configure each stub independently
//   userRepo.GetById.OnCall((id) => new User { Id = id, Name = "Test" });
//   productRepo.GetById.OnCall((id) => new Product { Id = id, Price = 9.99m });
//
//   // Use .Object to get the actual class instance
//   RepositoryBase<User> userService = userRepo.Object;
//   RepositoryBase<Product> productService = productRepo.Object;
//
//   // Verify calls per-instance
//   userRepo.GetById.Verify(Times.Once);
//
// VS OPEN GENERIC CLASS PATTERN (PATTERN 9):
//
//   | Aspect              | Generic Standalone Class            | Open Generic Class                    |
//   |---------------------|-------------------------------------|---------------------------------------|
//   | Declaration         | [KnockOffBase(typeof(T<>))] class S | [KnockOff(typeof(Foo<>))]             |
//   | Location            | Separate file (reusable)            | Nested in test class                  |
//   | Instantiation       | new RepositoryStub<User>()          | new Stubs.RepositoryBase<User>()      |
//   | Custom methods      | Yes (add to partial class)          | No                                    |
//   | Shared across tests | Yes                                 | No (scoped to containing class)       |
//   | .Object required    | Yes (composition pattern)           | Yes (composition pattern)             |
//   | Best for            | Shared generic class stubs          | One-off generic class tests           |
//
// DESIGN RATIONALE:
// Generic standalone class stubs provide reusability and extensibility for class
// stubs. While open generic class stubs are convenient for one-off usage, they
// cannot be shared across test classes or extended with custom methods. Generic
// standalone class stubs provide the same reusability as generic standalone
// interface stubs, but use the composition pattern required for class stubs.
//
// CONSTRAINT PROPAGATION:
// The generator preserves all type constraints from the base class. If the
// base class has `where T : class, IEntity, new()`, the stub class must also
// declare these constraints, and the generated code will enforce them.
//
// MULTIPLE TYPE PARAMETERS:
// Generic standalone class stubs support any number of type parameters:
//
//   [KnockOffBase(typeof(CacheBase<,>))]
//   public partial class CacheStub<TKey, TValue> where TKey : notnull { }
//
//   var cache = new CacheStub<string, int>();
//   CacheBase<string, int> cacheInstance = cache.Object;
//
// =============================================================================

<!-- snippet: patterns-generic-standalone-class-basic -->
<!-- endSnippet -->

<!-- snippet: patterns-generic-standalone-class-usage -->
<!-- endSnippet -->

// Placeholder for actual implementation - will be added by docs-code-samples agent
// The snippets above should reference examples from PatternsSamples.cs

// =============================================================================
// PATTERN 5: INLINE INTERFACE STUB
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
// PATTERN 6: INLINE CLASS STUB
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
// PATTERN 7: INLINE DELEGATE STUB
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
// PATTERN 8: OPEN GENERIC INTERFACE STUB (via typeof syntax)
// =============================================================================
// Use this pattern when:
// - You need to stub an open generic interface like IRepository<T>
// - You want a single stub definition that works with any type argument
// - The stub IS the implementation (no .Object property needed)
//
// DESIGN DECISION: Open generics require typeof() syntax because C# doesn't
// allow [KnockOff<IRepository<>>] - the type argument must be closed.
//
// GENERATOR BEHAVIOR: For this declaration:
//
//   [KnockOff(typeof(IRepository<>))]
//   public partial class OpenGenericInterfaceExample { }
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
//
// INSTANTIATION: The stub IS the interface implementation:
//   var stub = new Stubs.IRepository<string>();
//   IRepository<string> repo = stub;  // Direct assignment - no .Object needed
// =============================================================================

[KnockOff(typeof(IRepository<>))]
public partial class OpenGenericInterfaceExample
{
    public void Example_OpenGenericInterfaceUsage()
    {
        // Open generic interface stubs are instantiated with a concrete type argument
        var stringRepo = new Stubs.IRepository<string>();
        var entityRepo = new Stubs.IRepository<DataEventArgs>();

        // The stub IS the interface implementation (no .Object needed)
        IRepository<string> stringRepoInterface = stringRepo;
        IRepository<DataEventArgs> entityRepoInterface = entityRepo;

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
// PATTERN 9: OPEN GENERIC CLASS STUB (via typeof syntax)
// =============================================================================
// Use this pattern when:
// - You need to stub an open generic abstract class like ServiceBase<T>
// - You want a single stub definition that works with any type argument
// - You need access to the actual class instance via .Object property
//
// DESIGN DECISION: Open generic class stubs work like Inline Class stubs:
// - The generated stub is a wrapper class
// - Access the actual class instance via .Object property
// - This is consistent with the Inline Class pattern (pattern 3)
//
// GENERATOR BEHAVIOR: For this declaration:
//
//   [KnockOff(typeof(ServiceBase<>))]
//   public partial class OpenGenericClassExample { }
//
// The generator produces a wrapper class and internal generated class:
//
//   public static partial class Stubs
//   {
//       public partial class ServiceBase<T> : IKnockOffStub
//       {
//           private readonly global::ServiceBase<T> _object;
//           public global::ServiceBase<T> Object => _object;
//           // Interceptors for virtual/abstract members
//       }
//
//       private class ServiceBase_Generated<T> : global::ServiceBase<T>
//       {
//           // Delegates to interceptors
//       }
//   }
//
// INSTANTIATION: Use .Object to get the actual class instance:
//   var stub = new Stubs.ServiceBase<string>();
//   ServiceBase<string> service = stub.Object;  // .Object required for class stubs
//
// KEY DIFFERENCE FROM PATTERN 6:
// - Pattern 6 (Open Generic Interface): stub IS the implementation
// - Pattern 7 (Open Generic Class): stub.Object IS the implementation
// =============================================================================

[KnockOff(typeof(ServiceBase))]
public partial class OpenGenericClassExample
{
    public void Example_OpenGenericClassUsage()
    {
        // Open generic class stubs are instantiated with a concrete type argument
        // Note: Using the existing non-generic ServiceBase for this example
        var stub = new Stubs.ServiceBase();

        // IMPORTANT: .Object gives you the actual class instance
        ServiceBase service = stub.Object;

        // Configure via interceptors
        stub.Name.OnGet("TestService");
        stub.Execute.OnCall((cmd) => { /* handle command */ });

        // Unconfigured virtual methods call base implementation
        stub.Object.Initialize(); // Calls ServiceBase.Initialize()

        // PATTERN COMPARISON:
        // - Pattern 1 (Standalone Interface): IFoo foo = stub;              (no .Object)
        // - Pattern 3 (Standalone Class):     Foo foo = stub.Object;        (requires .Object)
        // - Pattern 5 (Inline Interface):     IFoo foo = stub;              (no .Object)
        // - Pattern 6 (Inline Class):         Foo foo = stub.Object;        (requires .Object)
        // - Pattern 8 (Open Generic Interface): IFoo<T> foo = stub;         (no .Object)
        // - Pattern 9 (Open Generic Class):     Foo<T> foo = stub.Object;   (requires .Object)
    }
}

// =============================================================================
// DESIGN DECISION SUMMARY
// =============================================================================
//
// DESIGN DECISION: Nine distinct patterns serve different use cases:
//
// STANDALONE PATTERNS (file-based, reusable across tests):
// 1. Standalone: Reusable interface stubs, custom methods/state, shared across tests
// 2. Generic Standalone: Same as Standalone but with generic type parameters
// 3. Standalone Class: Reusable class stubs, custom methods/state, uses .Object property
// 4. Generic Standalone Class: Same as Standalone Class but with generic type parameters
//
// INLINE PATTERNS (nested within test class):
// 5. Inline Interface: Scoped to test class, fewer files, no custom methods
// 6. Inline Class: Virtual/abstract members, base class fallback behavior, uses .Object
// 7. Inline Delegate: Named delegates with .Interceptor configuration
// 8. Open Generic Interface: Generic nested stubs from open generic interfaces (typeof(IFoo<>))
// 9. Open Generic Class: Generic nested stubs from open generic classes (typeof(Foo<>)), uses .Object
//
// KEY TRADE-OFF: Standalone vs Open Generic for generic types:
// - Generic Standalone: Reusable, can add custom methods, lives in own file, stub IS implementation
// - Generic Standalone Class: Reusable class stubs, custom methods, uses .Object property
// - Open Generic Interface: Quick one-off usage for interfaces, stub IS implementation
// - Open Generic Class: Quick one-off usage for classes, uses .Object property
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
