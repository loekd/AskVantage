using System.ComponentModel.DataAnnotations;

namespace ImageApi.Models;

public readonly record struct Image(
    [Required] Guid Id,
    [Required] string Name,
    [Required]
    [MinLength(10)]
    [MaxLength(10 * 1024 * 1024)]
    byte[] Content
);