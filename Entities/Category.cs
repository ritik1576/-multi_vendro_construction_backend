using System.ComponentModel.DataAnnotations.Schema;

public class Category
{
    public long Id { get; set; }

    [Column("parent_id")]
    public long? ParentId { get; set; }

    public string? Name { get; set; }

    public string? Slug { get; set; }

    public string? Image { get; set; }

    public string? Status { get; set; }
}