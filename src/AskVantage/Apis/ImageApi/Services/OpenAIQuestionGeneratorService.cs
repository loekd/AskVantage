using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;

namespace ImageApi.Services;

public partial class OpenAIQuestionGeneratorService(IServiceScopeFactory serviceScopeFactory)
    : IQuestionGeneratorService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IEnumerable<QuestionAnswerResponse>> GenerateQuestions(string input,
        CancellationToken cancellationToken)
    {
        var arguments = new KernelArguments
        {
            { "input_text", input }
        };
        using var scope = serviceScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var kernel = serviceProvider.GetRequiredService<Kernel>();
        var functionResult = await kernel.InvokeAsync(PluginNames.Prompty, FunctionNames.GenerateQuestions,
            arguments, cancellationToken);

        QuestionAnswerResponse[]? recipe = null;
        string resultUnfiltered = functionResult.GetValue<string>() ?? string.Empty;
        var match = JsonRegex().Match(resultUnfiltered);
        if (match.Success)
        {
            string json = match.Value;
            // Ensure the result is always a JSON array
            if (!json.TrimStart().StartsWith('[')) json = $"[{json}]";
            recipe = JsonSerializer.Deserialize<QuestionAnswerResponse[]>(json, JsonSerializerOptions);
        }


        return recipe ?? throw new InvalidOperationException("Failed to generate questions");
    }

    [GeneratedRegex(@"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))*\}(?(open)(?!))")]
    private static partial Regex JsonRegex();
}

internal static class PluginNames
{
    public const string Prompty = "Prompty";
}

internal static class FunctionNames
{
    public const string GenerateQuestions = "GenerateQuestions";
}