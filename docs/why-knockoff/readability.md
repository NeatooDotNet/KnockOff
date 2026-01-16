# Readable Test Stubs

KnockOff's primary goal is **readable tests**. When you revisit a test six months later, you should understand it immediately.

## The Problem with Moq Syntax

Moq uses lambda expressions everywhere:

<!-- snippet: readability-moq-problem-syntax -->
```cs
// Moq: Lambda in Setup
mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns(user);

// Moq: Lambda in Verify
mock.Verify(x => x.Save(It.IsAny<RdOrder>()), Times.Once);

// Moq: Lambda in Callback to capture
RdOrder? capturedOrder = null;
mock.Setup(x => x.Save(It.IsAny<RdOrder>()))
    .Callback<RdOrder>(o => capturedOrder = o);
```
<!-- endSnippet -->

This syntax:
- Requires understanding expression trees
- Hides what's actually happening behind ceremony
- Makes argument capture verbose
- Creates visual noise with `It.IsAny<T>()` markers

## KnockOff's Approach

Direct property access replaces lambda expressions:

<!-- snippet: readability-knockoff-approach -->
```cs
// KnockOff: Direct assignment
stub.GetUser.OnCall = (ko, id) => user;

// KnockOff: Direct property access
Assert.Equal(1, orderStub.Process.CallCount);

// KnockOff: Automatic argument tracking
var capturedOrder = orderStub.Process.LastCallArg;
```
<!-- endSnippet -->

## Side-by-Side Comparison

### Setup Return Values

**Moq:**

<!-- snippet: readability-moq-setup-returns -->
```cs
mock.Setup(x => x.GetUser(It.IsAny<int>()))
    .Returns((int id) => new RdUser { Id = id, Name = "Test" });
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: readability-knockoff-setup-returns -->
```cs
stub.GetUser.OnCall = (ko, id) => new RdUser { Id = id, Name = "Test" };
```
<!-- endSnippet -->

### Verify Method Called

**Moq:**

<!-- snippet: readability-moq-verify -->
```cs
mock.Verify(x => x.Save(It.IsAny<RdOrder>()), Times.Once);
mock.Verify(x => x.Delete(It.IsAny<int>()), Times.Never);
mock.Verify(x => x.GetAll(), Times.AtLeastOnce);
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: readability-knockoff-verify -->
```cs
Assert.Equal(1, stub.Save.CallCount);
Assert.Equal(0, stub.Delete.CallCount);
Assert.True(stub.GetAll.WasCalled);
```
<!-- endSnippet -->

### Capture Arguments

**Moq:**

<!-- snippet: readability-moq-capture-arguments -->
```cs
RdOrder? captured = null;
mock.Setup(x => x.Process(It.IsAny<RdOrder>()))
    .Callback<RdOrder>(o => captured = o);

// ... run test ...

Assert.Equal(expected, captured);
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: readability-knockoff-capture-arguments -->
```cs
// ... run test ...

Assert.Equal(expected, stub.Process.LastCallArg);
```
<!-- endSnippet -->

### Multiple Arguments

**Moq:**

<!-- snippet: readability-moq-multiple-arguments -->
```cs
string? to = null;
string? subject = null;
mock.Setup(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
    .Callback<string, string, string>((t, s, b) => { to = t; subject = s; });
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: readability-knockoff-multiple-arguments -->
```cs
// Named tuple access - no setup required
var args = stub.SendEmail.LastCallArgs;
Assert.Equal("user@example.com", args?.to);
Assert.Equal("Welcome", args?.subject);
```
<!-- endSnippet -->

### Properties

**Moq:**

<!-- snippet: readability-moq-properties -->
```cs
mock.Setup(x => x.IsConnected).Returns(true);
mock.SetupSet(x => x.Name = It.IsAny<string>()).Verifiable();
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: readability-knockoff-properties -->
```cs
stub.IsConnected.Value = true;
// Setter tracking is automatic
```
<!-- endSnippet -->

## Line Count Comparison

A typical test with multiple dependencies:

**Moq:**

<!-- snippet: readability-moq-line-count -->
```cs
var orderRepo = new Mock<IRdOrderRepository>();
orderRepo.Setup(x => x.GetById(It.IsAny<int>()))
    .Returns(new RdOrder { Id = 1, Amount = 100m });

var payment = new Mock<IRdPaymentService>();
payment.Setup(x => x.Process(It.IsAny<int>(), It.IsAny<decimal>()))
    .Returns(new RdPaymentResult { Success = true });

var notification = new Mock<IRdNotificationService>();

// var processor = new OrderProcessor(
//     orderRepo.Object, payment.Object, notification.Object);
// var result = processor.Process(1);
// Assert.True(result);

orderRepo.Verify(x => x.Save(It.IsAny<RdOrder>()), Times.Once);
notification.Verify(x => x.SendConfirmation(It.IsAny<int>()), Times.Once);
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: readability-knockoff-line-count -->
```cs
var orderRepo = new RdOrderRepositoryKnockOff();
orderRepo.GetById.OnCall = (ko, id) => new RdOrder { Id = 1, Amount = 100m };

var payment = new RdPaymentServiceKnockOff();
payment.Process.OnCall = (ko, id, amount) => new RdPaymentResult { Success = true };

var notification = new RdNotificationServiceKnockOff();

// var processor = new OrderProcessor(orderRepo, payment, notification);
// var result = processor.Process(1);
// Assert.True(result);

Assert.Equal(1, orderRepo.Save.CallCount);
Assert.Equal(1, notification.SendConfirmation.CallCount);
```
<!-- endSnippet -->

The difference grows with test complexity. More dependencies, more verification, more argument capture — KnockOff stays readable while Moq accumulates ceremony.

## Why This Matters

1. **Faster comprehension** — Less syntax to parse means faster understanding
2. **Easier debugging** — Direct property access means standard debugging works
3. **Better diffs** — Simpler syntax produces cleaner code review diffs
4. **Lower barrier** — New team members learn faster without expression tree knowledge

## Next

- [The Duality Pattern](duality-pattern.md) — Two ways to customize behavior
- [Compile-Time Safety](compile-time-safety.md) — Catch errors before runtime
