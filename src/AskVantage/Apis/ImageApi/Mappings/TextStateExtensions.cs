using System.Collections.Immutable;
using ImageApi.Models;
using ImageApi.Services;

namespace ImageApi.Mappings;

public static class TextStateExtensions
{
    public static QuestionGenerationResult BuildResponse(this TextState textState, Guid? requestId = null)
    {
        return new QuestionGenerationResult
        {
            RequestId = requestId ?? Guid.Empty,
            OriginalText = textState.Text,
            TextTitle = textState.Title,
            QuestionsAndAnswers = textState.Questions.Select(r => new QuestionAndAnswer
            {
                Question = r.Question,
                Answer = r.Answer,
                Reference = r.Reference
            }).ToImmutableList()
        };
    }
}