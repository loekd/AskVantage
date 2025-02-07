using System.Collections.Immutable;

namespace AskVantage.Client.Models;

public readonly record struct QuestionGenerationRequest
{
    public Guid RequestId { get; init; }

    public string Text { get; init; }

    public string TextTitle { get; init; }
}
public readonly record struct QuestionGenerationResult
{
    public Guid Id { get; init; }

    public Guid RequestId { get; init; }

    public string OriginalText { get; init; }

    public string TextTitle { get; init; }

    public QuestionAndAnswer[] QuestionsAndAnswers { get; init; }
}

public readonly record struct QuestionAndAnswer
{
    public string Question { get; init; }

    public string Answer { get; init; }

    public string Reference { get; init; }
}
