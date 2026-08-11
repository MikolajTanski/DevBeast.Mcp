namespace App.Application.Orders.Dtos;

public sealed record ProductDto(Guid Id, string Name, decimal Price);
