/// <summary>
/// Code samples for docs/why-knockoff/readability.md
/// </summary>

using Moq;

namespace KnockOff.Documentation.Samples.WhyKnockOff;

// ============================================================================
// Domain Types
// ============================================================================

public class RdUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class RdOrder
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RdPaymentResult
{
    public bool Success { get; set; }
}

public interface IRdUserService
{
    RdUser GetUser(int id);
    void Save(RdOrder order);
}

public interface IRdOrderService
{
    void Save(RdOrder order);
    void Delete(int id);
    IEnumerable<RdOrder> GetAll();
    void Process(RdOrder order);
}

public interface IRdEmailService
{
    void SendEmail(string to, string subject, string body);
}

public interface IRdConnectionService
{
    bool IsConnected { get; set; }
    string Name { get; set; }
}

public interface IRdOrderRepository
{
    RdOrder GetById(int id);
    void Save(RdOrder order);
}

public interface IRdPaymentService
{
    RdPaymentResult Process(int orderId, decimal amount);
}

public interface IRdNotificationService
{
    void SendConfirmation(int orderId);
}

// ============================================================================
// KnockOff Stubs
// ============================================================================

[KnockOff]
public partial class RdUserServiceKnockOff : IRdUserService { }

[KnockOff]
public partial class RdOrderServiceKnockOff : IRdOrderService { }

[KnockOff]
public partial class RdEmailServiceKnockOff : IRdEmailService { }

[KnockOff]
public partial class RdConnectionServiceKnockOff : IRdConnectionService { }

[KnockOff]
public partial class RdOrderRepositoryKnockOff : IRdOrderRepository { }

[KnockOff]
public partial class RdPaymentServiceKnockOff : IRdPaymentService { }

[KnockOff]
public partial class RdNotificationServiceKnockOff : IRdNotificationService { }

// ============================================================================
// Sample code for docs
// ============================================================================

public static class ReadabilitySamples
{
    public static void MoqProblemSyntax()
    {
        var user = new RdUser();
        var mock = new Mock<IRdUserService>();

        #region readability-moq-problem-syntax
        // Moq: Lambda in Setup
        mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns(user);

        // Moq: Lambda in Verify
        mock.Verify(x => x.Save(It.IsAny<RdOrder>()), Times.Once);

        // Moq: Lambda in Callback to capture
        RdOrder? capturedOrder = null;
        mock.Setup(x => x.Save(It.IsAny<RdOrder>()))
            .Callback<RdOrder>(o => capturedOrder = o);
        #endregion

        _ = capturedOrder;
    }

    public static void KnockOffApproach()
    {
        var user = new RdUser();
        var stub = new RdUserServiceKnockOff();
        var orderStub = new RdOrderServiceKnockOff();
        IRdOrderService orderService = orderStub;

        orderService.Process(new RdOrder());

        #region readability-knockoff-approach
        // KnockOff: Direct assignment
        stub.GetUser.OnCall = (ko, id) => user;

        // KnockOff: Direct property access
        Assert.Equal(1, orderStub.Process.CallCount);

        // KnockOff: Automatic argument tracking
        var capturedOrder = orderStub.Process.LastCallArg;
        #endregion

        _ = capturedOrder;
    }

    public static void MoqSetupReturns()
    {
        var mock = new Mock<IRdUserService>();

        #region readability-moq-setup-returns
        mock.Setup(x => x.GetUser(It.IsAny<int>()))
            .Returns((int id) => new RdUser { Id = id, Name = "Test" });
        #endregion
    }

    public static void KnockOffSetupReturns()
    {
        var stub = new RdUserServiceKnockOff();

        #region readability-knockoff-setup-returns
        stub.GetUser.OnCall = (ko, id) => new RdUser { Id = id, Name = "Test" };
        #endregion
    }

    public static void MoqVerify()
    {
        var mock = new Mock<IRdOrderService>();
        mock.Object.Save(new RdOrder());
        mock.Object.GetAll();

        #region readability-moq-verify
        mock.Verify(x => x.Save(It.IsAny<RdOrder>()), Times.Once);
        mock.Verify(x => x.Delete(It.IsAny<int>()), Times.Never);
        mock.Verify(x => x.GetAll(), Times.AtLeastOnce);
        #endregion
    }

    public static void KnockOffVerify()
    {
        var stub = new RdOrderServiceKnockOff();
        IRdOrderService service = stub;
        service.Save(new RdOrder());
        _ = service.GetAll();

        #region readability-knockoff-verify
        Assert.Equal(1, stub.Save.CallCount);
        Assert.Equal(0, stub.Delete.CallCount);
        Assert.True(stub.GetAll.WasCalled);
        #endregion
    }

    public static void MoqCaptureArguments()
    {
        var mock = new Mock<IRdOrderService>();
        var expected = new RdOrder();

        #region readability-moq-capture-arguments
        RdOrder? captured = null;
        mock.Setup(x => x.Process(It.IsAny<RdOrder>()))
            .Callback<RdOrder>(o => captured = o);

        // ... run test ...

        Assert.Equal(expected, captured);
        #endregion
    }

    public static void KnockOffCaptureArguments()
    {
        var stub = new RdOrderServiceKnockOff();
        IRdOrderService service = stub;
        var expected = new RdOrder();
        service.Process(expected);

        #region readability-knockoff-capture-arguments
        // ... run test ...

        Assert.Equal(expected, stub.Process.LastCallArg);
        #endregion
    }

    public static void MoqMultipleArguments()
    {
        var mock = new Mock<IRdEmailService>();

        #region readability-moq-multiple-arguments
        string? to = null;
        string? subject = null;
        mock.Setup(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((t, s, b) => { to = t; subject = s; });
        #endregion

        _ = (to, subject);
    }

    public static void KnockOffMultipleArguments()
    {
        var stub = new RdEmailServiceKnockOff();
        IRdEmailService service = stub;
        service.SendEmail("user@example.com", "Welcome", "Hello");

        #region readability-knockoff-multiple-arguments
        // Named tuple access - no setup required
        var args = stub.SendEmail.LastCallArgs;
        Assert.Equal("user@example.com", args?.to);
        Assert.Equal("Welcome", args?.subject);
        #endregion
    }

    public static void MoqProperties()
    {
        var mock = new Mock<IRdConnectionService>();

        #region readability-moq-properties
        mock.Setup(x => x.IsConnected).Returns(true);
        mock.SetupSet(x => x.Name = It.IsAny<string>()).Verifiable();
        #endregion
    }

    public static void KnockOffProperties()
    {
        var stub = new RdConnectionServiceKnockOff();

        #region readability-knockoff-properties
        stub.IsConnected.Value = true;
        // Setter tracking is automatic
        #endregion
    }

    public static void MoqLineCountExample()
    {
        #region readability-moq-line-count
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
        #endregion
    }

    public static void KnockOffLineCountExample()
    {
        #region readability-knockoff-line-count
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
        #endregion
    }
}

// Minimal Assert for compilation
file static class Assert
{
    public static void Equal<T>(T expected, T actual) { }
    public static void True(bool condition) { }
}
