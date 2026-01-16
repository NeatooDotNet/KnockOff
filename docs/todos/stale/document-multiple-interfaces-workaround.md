# Document Multiple Interfaces Workaround

## Task

- [ ] Add documentation explaining that KnockOff doesn't support multiple unrelated interfaces
- [ ] Document the composite interface workaround
- [ ] Update the knockoff skill with this guidance

## Context

Moq supports adding additional interfaces to a mock:

```csharp
var fooMock = new Mock<IFoo>();
fooMock.As<IBar>();
```

KnockOff intentionally does **not** support this for unrelated interfaces. The complexity isn't worth the edge case.

## What KnockOff Does Support

**Interface inheritance works automatically:**

```csharp
interface IBar { void BarMethod(); }
interface IFoo : IBar { void FooMethod(); }

[KnockOff]
partial class FooStub : IFoo { }

// This works - FooStub is already an IBar
if (stub is IBar bar) { ... }  // true
```

The generator uses `AllInterfaces` to generate implementations for the full inheritance chain.

## The Workaround for Unrelated Interfaces

If code under test checks for multiple unrelated interfaces, create a composite interface:

```csharp
// In your test file - one line
interface IFooBar : IFoo, IBar { }

[KnockOff]
partial class FooBarStub : IFooBar { }
```

## Why This Is Fine

1. The workaround is trivial (one line)
2. It's more explicit and self-documenting
3. Runtime type checks on unrelated interfaces is a code smell anyway
4. Avoids significant generator complexity (merging members, naming conflicts, multiple Interceptor containers)

## Documentation Locations

1. **docs/guides/**: New or existing guide about interface handling
2. **knockoff skill**: Add guidance for when users ask about multiple interfaces
3. **Moq migration docs**: Mention this as a migration consideration

## Sample Documentation Text

> **Multiple Interfaces**
>
> KnockOff stubs automatically implement inherited interfaces. If `IFoo : IBar`, a stub of `IFoo` is also an `IBar`.
>
> For unrelated interfaces, create a composite interface in your test:
>
> ```csharp
> interface IFooBar : IFoo, IBar { }
>
> [KnockOff]
> partial class FooBarStub : IFooBar { }
> ```
>
> This is intentional - it keeps the generator simple and makes your test's requirements explicit.
