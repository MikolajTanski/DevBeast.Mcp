namespace App.Application.Orders.Dtos;

public class OrderDto
{
    public Guid Id { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
}
