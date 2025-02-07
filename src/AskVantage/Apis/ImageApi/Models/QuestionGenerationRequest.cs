using System.ComponentModel.DataAnnotations;

namespace ImageApi.Models;

public readonly record struct QuestionGenerationRequest
{
    public Guid RequestId { get; init; }

    [Required]
    [StringLength(10*1024*1024, MinimumLength = 20)]
    public string Text { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 4)]
    public string TextTitle { get; init; }
}
