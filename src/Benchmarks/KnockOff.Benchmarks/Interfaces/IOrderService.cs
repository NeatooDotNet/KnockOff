namespace KnockOff.Benchmarks.Interfaces;

/// <summary>
/// Order service interface matching README Scenario 1: Order Processing.
/// </summary>
public interface IOrderService
{
    Order GetOrder(int id);
    bool ValidateOrder(Order order);
    decimal CalculateTotal(Order order);
    void SaveOrder(Order order);
}

/// <summary>
/// Simple order entity matching README Scenario 1.
/// </summary>
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
}

/// <summary>
/// Order processor matching README Scenario 1.
/// </summary>
public class OrderProcessor
{
    private readonly IOrderService _orderService;

    public OrderProcessor(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public void Process(int orderId)
    {
        var order = _orderService.GetOrder(orderId);
        if (_orderService.ValidateOrder(order))
        {
            _ = _orderService.CalculateTotal(order);
            _orderService.SaveOrder(order);
        }
    }
}
