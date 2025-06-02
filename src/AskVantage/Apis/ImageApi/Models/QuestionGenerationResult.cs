using System.Collections.Immutable;

namespace ImageApi.Models;

public readonly record struct QuestionGenerationResult(
    Guid Id,
    Guid RequestId,
    string OriginalText,
    string TextTitle,
    ImmutableList<QuestionAndAnswer> QuestionsAndAnswers);

public readonly record struct QuestionAndAnswer(
    string Question,
    string Answer,
    string Reference);