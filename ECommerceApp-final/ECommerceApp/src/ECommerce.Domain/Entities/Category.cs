namespace ECommerce.Domain.Entities;

public class Category : Entity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private Category() { }

    public static Category Create(string name, string description = "")
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description
        };
    }
}
