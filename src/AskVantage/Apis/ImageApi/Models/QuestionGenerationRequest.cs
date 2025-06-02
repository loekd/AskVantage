using System.ComponentModel.DataAnnotations;

namespace ImageApi.Models;

public readonly record struct QuestionGenerationRequest(
    Guid RequestId,
    [property: Required]
    [property: StringLength(10 * 1024 * 1024, MinimumLength = 20)]
    string Text,
    [property: Required]
    [property: StringLength(100, MinimumLength = 4)]
    string TextTitle
);