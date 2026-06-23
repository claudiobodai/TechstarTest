namespace TechstarTest.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    protected Product() { }

    public Product(string name, decimal price)
    {
        Name = name;
        Price = price;
    }
}
