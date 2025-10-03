namespace SchemaTest.Api.Models;

public class Customer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Customer()
    {
    }

    public Customer(string name, string email)
    {
        Name = name;
        Email = email;
    }
}
