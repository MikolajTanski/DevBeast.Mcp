namespace App.Domain.Orders;

using Microsoft.EntityFrameworkCore;

public class Order
{
    public Guid Id { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
}
