using System.ComponentModel.DataAnnotations;

namespace AskVantage.Frontend.Client.Models;

public readonly record struct Image
{
    [Required] public Guid Id { get; init; }

    [Required] public string Name { get; init; }

    [Required]
    [MinLength(10)]
    [MaxLength(10 * 1024 * 1024)]
    public byte[] Content { get; init; }
}