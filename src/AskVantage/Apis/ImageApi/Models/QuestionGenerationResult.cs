using System.Collections.Immutable;

namespace ImageApi.Models;

public readonly record struct QuestionGenerationResult
{
    public Guid Id { get; init; }

    public Guid RequestId { get; init; }

    public string OriginalText { get; init; }

    public string TextTitle { get; init; }

    public ImmutableList<QuestionAndAnswer> QuestionsAndAnswers{ get; init; }
}

public readonly record struct QuestionAndAnswer
{
    public string Question { get; init; }

    public string Answer { get; init; }

    public string Reference { get; init; }
}