namespace Chaetsere.Domain.Entities;

/// <summary>Global service categories: nails, hair, brows, makeup, spa, epilation.</summary>
public class Category
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Icon { get; set; }
    public int SortOrder { get; set; }

    public List<ServiceTemplate> Templates { get; set; } = [];
}

/// <summary>Suggested services shown in the salon onboarding wizard.</summary>
public class ServiceTemplate
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public required string Name { get; set; }
    public int DurationMinutes { get; set; }
    public int BufferMinutes { get; set; }
    public decimal DefaultPrice { get; set; }
    public int SortOrder { get; set; }

    public Category Category { get; set; } = null!;
}

public class District
{
    public int Id { get; set; }
    public required string City { get; set; }
    public required string Name { get; set; }
}
