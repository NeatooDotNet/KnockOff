# Full Comparison: KnockOff vs Moq vs NSubstitute

Side-by-side comparisons for methods, argument matching, properties, events, delegates, and indexers.

---

## Methods

| Task | Moq | NSubstitute | KnockOff |
|------|-----|-------------|----------|
| **Return value** | `mock.Setup(x => x.Add(1, 2)).Returns(3);` | `calc.Add(1, 2).Returns(3);` | `stub.Add.Returns(3);` |
| **Any argument** | `mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns(10);` | `calc.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(10);` | `stub.Add.Returns(10);` |
| **Match values** | `mock.Setup(x => x.Add(1, 2)).Returns(100);` | `calc.Add(1, 2).Returns(100);` | `stub.Add.When(1, 2).Returns(100);` |
| **Conditional** | `mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns<int, int>((a, b) => a > 0 ? a + b : 0);` | `calc.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(x => ...);` | `stub.Add.OnCall((a, b) => a > 0 ? a + b : 0);` |
| **Throw** | `mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Throws<Exception>();` | `calc.Add(Arg.Any<int>(), Arg.Any<int>()).Throws<Exception>();` | `stub.Add.OnCall((a, b) => throw new Exception());` |
| **Callback** | `mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns(3).Callback<int, int>((a, b) => log.Add(a));` | `calc.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(3).AndDoes(x => ...);` | `stub.Add.OnCall((a, b) => { log.Add(a); return 3; });` |
| **Sequence** | `mock.SetupSequence(x => x.Add(1, 2)).Returns(1).Returns(2).Returns(3);` | `calc.Add(1, 2).Returns(1, 2, 3);` | `stub.Add.Returns(1, 2, 3);` |
| **Async** | `mock.Setup(x => x.GetUserAsync(1)).ReturnsAsync(user);` | `repo.GetUserAsync(1).Returns(user);` | `stub.GetUserAsync.Returns(user);` |
| **Verify called** | `mock.Verify(x => x.Add(1, 2));` | `calc.Received().Add(1, 2);` | `stub.Add.Verify();` |
| **Verify count** | `mock.Verify(x => x.Add(It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));` | `calc.Received(3).Add(Arg.Any<int>(), Arg.Any<int>());` | `stub.Add.Verify(Times.Exactly(3));` |

---

## Argument Matching

**Moq:**
```cs
// Moq - It.Is<T> per parameter
mock.Setup(x => x.Add(It.Is<int>(a => a > 0), It.IsAny<int>())).Returns(100);
```

**NSubstitute:**
<!-- snippet: readme-argmatch-nsub-matchers -->
```cs
// NSubstitute - Arg.Is<T> per parameter (permanent matchers)
calc.Add(Arg.Is<int>(a => a > 0), Arg.Any<int>()).Returns(100);
```
<!-- endSnippet -->

**KnockOff:**
<!-- snippet: readme-argmatch-knockoff-oncall -->
```cs
// KnockOff - OnCall with conditional (permanent, matches all calls)
stub.Add.OnCall((a, b) => a > 0 ? 100 : 0);
```
<!-- endSnippet -->

<!-- snippet: readme-argmatch-knockoff-when -->
```cs
// KnockOff - When() for sequential matching (first match returns 100, then falls through)
stub.Add.When((a, b) => a > 0).Returns(100).ThenCall((a, b) => a + b);
```
<!-- endSnippet -->

**Multiple specific values:**

**Moq:**
```cs
mock.Setup(x => x.Add(1, 2)).Returns(100);
mock.Setup(x => x.Add(3, 4)).Returns(200);
```

<!-- snippet: readme-argmatch-nsub-specific -->
```cs
// Multiple specific values
calc.Add(1, 2).Returns(100);
calc.Add(3, 4).Returns(200);
```
<!-- endSnippet -->

<!-- snippet: readme-argmatch-knockoff-specific -->
```cs
stub.Add.When(1, 2).Returns(100);
stub.Add.When(3, 4).Returns(200);
```
<!-- endSnippet -->

**Note:** Moq and NSubstitute matchers are permanent — they match all qualifying calls. KnockOff's `When()` is sequential — matchers are consumed in order. Use `OnCall()` with conditionals for permanent matching behavior.

---

## Argument Capture

**Moq:**
```cs
// Moq - requires Callback setup
int capturedA = 0, capturedB = 0;
mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>()))
    .Callback<int, int>((a, b) => { capturedA = a; capturedB = b; });
mock.Object.Add(1, 2);
```

**NSubstitute:**
<!-- snippet: readme-argcapture-nsub -->
```cs
// NSubstitute - requires Arg.Do in setup
int capturedA = 0, capturedB = 0;
calc.Add(Arg.Do<int>(x => capturedA = x), Arg.Do<int>(x => capturedB = x));
calc.Add(1, 2);
```
<!-- endSnippet -->

**KnockOff:**
<!-- snippet: readme-argcapture-knockoff -->
```cs
// KnockOff - built-in, no pre-setup
var tracking = stub.Add.OnCall((a, b) => a + b);
ICalculator calc = stub;
calc.Add(1, 2);
var (a, b) = tracking.LastArgs;  // Named tuple: a = 1, b = 2
```
<!-- endSnippet -->

---

## Properties

| Task | Moq | NSubstitute | KnockOff |
|------|-----|-------------|----------|
| **Setup getter** | `mock.Setup(x => x.Mode).Returns("Scientific");` | `calc.Mode.Returns("Scientific");` | `stub.Mode.OnGet("Scientific");` |
| **Setup setter** | `mock.SetupSet(x => x.Mode = It.IsAny<string>()).Callback<string>(v => captured = v);` | `calc.When(x => x.Mode = Arg.Any<string>()).Do(x => ...);` | `stub.Mode.OnSet((v) => captured = v);` |
| **Verify getter** | `mock.VerifyGet(x => x.Mode);` | `_ = calc.Received().Mode;` | `stub.Mode.VerifyGet();` |
| **Verify setter** | `mock.VerifySet(x => x.Mode = "Scientific");` | `calc.Received().Mode = "Scientific";` | `stub.Mode.VerifySet();` |
| **Verify count** | `mock.VerifyGet(x => x.Mode, Times.Exactly(3));` | `_ = calc.Received(3).Mode;` | `stub.Mode.VerifyGet(Times.Exactly(3));` |
| **Capture value** | `mock.SetupSet(x => x.Mode = It.IsAny<string>()).Callback<string>(v => captured = v);` | `calc.When(x => x.Mode = Arg.Do<string>(v => ...)).Do(...);` | `stub.Mode.LastSetValue` (built-in) |

---

## Events

| Task | Moq | NSubstitute | KnockOff |
|------|-----|-------------|----------|
| **Raise event** | `mock.Raise(x => x.PoweringUp += null, EventArgs.Empty);` | `calc.PoweringUp += Raise.Event();` | `stub.PoweringUp.Raise(stub, EventArgs.Empty);` |
| **Raise with args** | `mock.Raise(x => x.PoweringUp += null, sender, args);` | `calc.PoweringUp += Raise.EventWith(sender, args);` | `stub.PoweringUp.Raise(sender, args);` |
| **Verify subscription** | *(not available)* | *(not available)* | `stub.PoweringUp.VerifyAdd(Times.Once);` |
| **Verify unsubscription** | *(not available)* | *(not available)* | `stub.PoweringUp.VerifyRemove(Times.Once);` |
| **Check subscribers** | *(not available)* | *(not available)* | `stub.PoweringUp.HasSubscribers` |

---

## Delegates

| Task | Moq | NSubstitute | KnockOff |
|------|-----|-------------|----------|
| **Setup** | `mock.Setup(x => x(It.IsAny<int>())).Returns("result");` | `factory(Arg.Any<int>()).Returns("result");` | `stub.Interceptor.Returns("result");` |
| **With logic** | `mock.Setup(x => x(It.Is<int>(v => v > 0))).Returns<int>(x => $"val: {x}");` | `factory(Arg.Is<int>(x => x > 0)).Returns(x => $"val: {x.Arg<int>()}");` | `stub.Interceptor.OnCall((x) => $"val: {x}");` |
| **Sequence** | `mock.SetupSequence(x => x(It.IsAny<int>())).Returns(1).Returns(2).Returns(3);` | `factory(Arg.Any<int>()).Returns(1, 2, 3);` | `stub.Interceptor.Returns(1, 2, 3);` |
| **Async** | `mock.Setup(x => x(1)).ReturnsAsync(42);` | `asyncOp(1).Returns(42);` | `stub.Interceptor.Returns(42);` (auto-wraps) |
| **Match values** | `mock.Setup(x => x(42)).Returns("found");` | *(per-parameter Arg.Is)* | `stub.Interceptor.When(42).Returns("found");` |
| **Verify** | `mock.Verify(x => x(42));` | `factory.Received()(42);` | `stub.Interceptor.Verify();` |
| **Verify count** | `mock.Verify(x => x(It.IsAny<int>()), Times.Exactly(3));` | `factory.Received(3)(Arg.Any<int>());` | `stub.Interceptor.Verify(Times.Exactly(3));` |
| **Capture** | *(manual with Callback)* | *(manual with Arg.Do)* | `stub.Interceptor.LastArg` (built-in) |

---

## Indexers

| Task | Moq | NSubstitute | KnockOff |
|------|-----|-------------|----------|
| **Setup getter** | `mock.Setup(x => x["key"]).Returns(42);` | `dict["key"].Returns(42);` | `stub.Indexer.Backing["key"] = 42;` |
| **Dynamic getter** | `mock.Setup(x => x[It.IsAny<string>()]).Returns(0);` | `dict[Arg.Any<string>()].Returns(0);` | `stub.Indexer.OnGet((key) => 0);` |
| **Verify getter** | `mock.Verify(x => x["key"]);` | `_ = dict.Received()["key"];` | `stub.Indexer.VerifyGet();` |
| **Verify setter** | `mock.VerifySet(x => x["key"] = 42);` | `dict.Received()["key"] = 42;` | `stub.Indexer.VerifySet();` |
| **Capture** | *(manual with Callback)* | *(manual with When/Do)* | `stub.Indexer.LastSetEntry` (built-in) |
