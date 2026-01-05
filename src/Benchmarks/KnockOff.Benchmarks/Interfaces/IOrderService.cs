namespace KnockOff.Benchmarks.Interfaces;

/// <summary>
/// Realistic business interface for order management.
/// Used to measure real-world usage patterns.
/// </summary>
public interface IOrderService
{
    Order? GetOrder(int orderId);
    IEnumerable<Order> GetOrdersByCustomer(int customerId);
    void SaveOrder(Order order);
    void DeleteOrder(int orderId);
    bool ValidateOrder(Order order);
    decimal CalculateTotal(Order order);
}

/// <summary>
/// Simple order entity for realistic benchmark scenarios.
/// </summary>
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

/// <summary>
/// Order line item.
/// </summary>
public class OrderItem
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Simple order processor for realistic benchmarks.
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
        if (order != null && _orderService.ValidateOrder(order))
        {
            order.Amount = _orderService.CalculateTotal(order);
            _orderService.SaveOrder(order);
        }
    }
}
