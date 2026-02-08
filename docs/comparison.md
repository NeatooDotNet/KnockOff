# Full Comparison: KnockOff vs Moq vs NSubstitute

Side-by-side comparisons for methods, properties, events, delegates, and indexers.

For argument matching, argument capture, and method overload resolution comparisons, see the [README](../README.md#argument-matching).

---

## Methods

| Task | Moq | NSubstitute | KnockOff |
|------|-----|-------------|----------|
| **Return value** | `mock.Setup(x => x.Add(1, 2)).Returns(3);` | `calc.Add(1, 2).Returns(3);` | `stub.Add.Return(3);` |
| **Any argument** | `mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns(10);` | `calc.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(10);` | `stub.Add.Return(10);` |
| **Match values** | `mock.Setup(x => x.Add(1, 2)).Returns(100);` | `calc.Add(1, 2).Returns(100);` | `stub.Add.When(1, 2).Return(100);` |
| **Conditional** | `mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns<int, int>((a, b) => a > 0 ? a + b : 0);` | `calc.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(x => ...);` | `stub.Add.Return((a, b) => a > 0 ? a + b : 0);` |
| **Throw** | `mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Throws<Exception>();` | `calc.Add(Arg.Any<int>(), Arg.Any<int>()).Throws<Exception>();` | `stub.Add.Return((a, b) => throw new Exception());` |
| **Callback** | `mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns(3).Callback<int, int>((a, b) => log.Add(a));` | `calc.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(3).AndDoes(x => ...);` | `stub.Add.Return((a, b) => { log.Add(a); return 3; });` |
| **Sequence** | `mock.SetupSequence(x => x.Add(1, 2)).Returns(1).Returns(2).Returns(3);` | `calc.Add(1, 2).Returns(1, 2, 3);` | `stub.Add.Return(1, 2, 3);` |
| **Async** | `mock.Setup(x => x.GetUserAsync(1)).ReturnsAsync(user);` | `repo.GetUserAsync(1).Returns(user);` | `stub.GetUserAsync.Return(user);` |
| **Verify called** | `mock.Verify(x => x.Add(1, 2));` | `calc.Received().Add(1, 2);` | `stub.Add.Verify();` |
| **Verify count** | `mock.Verify(x => x.Add(It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));` | `calc.Received(3).Add(Arg.Any<int>(), Arg.Any<int>());` | `stub.Add.Verify(Called.Exactly(3));` |

---

## Properties

| Task | Moq | NSubstitute | KnockOff |
|------|-----|-------------|----------|
| **Setup getter** | `mock.Setup(x => x.Mode).Returns("Scientific");` | `calc.Mode.Returns("Scientific");` | `stub.Mode.Get("Scientific");` |
| **Setup setter** | `mock.SetupSet(x => x.Mode = It.IsAny<string>()).Callback<string>(v => captured = v);` | `calc.When(x => x.Mode = Arg.Any<string>()).Do(x => ...);` | `stub.Mode.Set((v) => captured = v);` |
| **Verify getter** | `mock.VerifyGet(x => x.Mode);` | `_ = calc.Received().Mode;` | `stub.Mode.VerifyGet();` |
| **Verify setter** | `mock.VerifySet(x => x.Mode = "Scientific");` | `calc.Received().Mode = "Scientific";` | `stub.Mode.VerifySet();` |
| **Verify count** | `mock.VerifyGet(x => x.Mode, Times.Exactly(3));` | `_ = calc.Received(3).Mode;` | `stub.Mode.VerifyGet(Called.Exactly(3));` |
| **Capture value** | `mock.SetupSet(x => x.Mode = It.IsAny<string>()).Callback<string>(v => captured = v);` | `calc.When(x => x.Mode = Arg.Do<string>(v => ...)).Do(...);` | `stub.Mode.LastSetValue` (built-in) |

---

## Events

| Task | Moq | NSubstitute | KnockOff |
|------|-----|-------------|----------|
| **Raise event** | `mock.Raise(x => x.PoweringUp += null, EventArgs.Empty);` | `calc.PoweringUp += Raise.Event();` | `stub.PoweringUp.Raise(stub, EventArgs.Empty);` |
| **Raise with args** | `mock.Raise(x => x.PoweringUp += null, sender, args);` | `calc.PoweringUp += Raise.EventWith(sender, args);` | `stub.PoweringUp.Raise(sender, args);` |
| **Verify subscription** | *(not available)* | *(not available)* | `stub.PoweringUp.VerifyAdd(Called.Once);` |
| **Verify unsubscription** | *(not available)* | *(not available)* | `stub.PoweringUp.VerifyRemove(Called.Once);` |
| **Check subscribers** | *(not available)* | *(not available)* | `stub.PoweringUp.HasSubscribers` |

---

## Delegates

| Task | Moq | NSubstitute | KnockOff |
|------|-----|-------------|----------|
| **Setup** | `mock.Setup(x => x(It.IsAny<int>())).Returns("result");` | `factory(Arg.Any<int>()).Returns("result");` | `stub.Interceptor.Return("result");` |
| **With logic** | `mock.Setup(x => x(It.Is<int>(v => v > 0))).Returns<int>(x => $"val: {x}");` | `factory(Arg.Is<int>(x => x > 0)).Returns(x => $"val: {x.Arg<int>()}");` | `stub.Interceptor.Return((x) => $"val: {x}");` |
| **Sequence** | `mock.SetupSequence(x => x(It.IsAny<int>())).Returns(1).Returns(2).Returns(3);` | `factory(Arg.Any<int>()).Returns(1, 2, 3);` | `stub.Interceptor.Return(1, 2, 3);` |
| **Async** | `mock.Setup(x => x(1)).ReturnsAsync(42);` | `asyncOp(1).Returns(42);` | `stub.Interceptor.Return(42);` (auto-wraps) |
| **Match values** | `mock.Setup(x => x(42)).Returns("found");` | *(per-parameter Arg.Is)* | `stub.Interceptor.When(42).Return("found");` |
| **Verify** | `mock.Verify(x => x(42));` | `factory.Received()(42);` | `stub.Interceptor.Verify();` |
| **Verify count** | `mock.Verify(x => x(It.IsAny<int>()), Times.Exactly(3));` | `factory.Received(3)(Arg.Any<int>());` | `stub.Interceptor.Verify(Called.Exactly(3));` |
| **Capture** | *(manual with Callback)* | *(manual with Arg.Do)* | `stub.Interceptor.LastArg` (built-in) |

---

## Indexers

| Task | Moq | NSubstitute | KnockOff |
|------|-----|-------------|----------|
| **Setup getter** | `mock.Setup(x => x["key"]).Returns(42);` | `dict["key"].Returns(42);` | `stub.Indexer.Backing["key"] = 42;` |
| **Dynamic getter** | `mock.Setup(x => x[It.IsAny<string>()]).Returns(0);` | `dict[Arg.Any<string>()].Returns(0);` | `stub.Indexer.Get((key) => 0);` |
| **Verify getter** | `mock.Verify(x => x["key"]);` | `_ = dict.Received()["key"];` | `stub.Indexer.VerifyGet();` |
| **Verify setter** | `mock.VerifySet(x => x["key"] = 42);` | `dict.Received()["key"] = 42;` | `stub.Indexer.VerifySet();` |
| **Capture** | *(manual with Callback)* | *(manual with When/Do)* | `stub.Indexer.LastSetEntry` (built-in) |
