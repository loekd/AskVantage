using Dapr.Client;

namespace ImageApi.Services;

/// <summary>
/// Represents a question with its answer and a reference to where the generator found that answer.
/// </summary>
/// <param name="Question"></param>
/// <param name="Answer"></param>
/// <param name="Reference"></param>
public readonly record struct QuestionState(string Question, string Answer, string Reference);

/// <summary>
/// Represents a text with its title, the text itself and a list of questions with their answers.
/// </summary>
/// <param name="Title"></param>
/// <param name="Text"></param>
/// <param name="Questions"></param>
public readonly record struct TextState(string Title, string Text, QuestionState[] Questions);

/// <summary>
/// Manages Texts with Questions and Answers.
/// </summary>
public interface ITextStateService
{
    /// <summary>
    /// Delete everything from the store.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DeleteAllTexts(CancellationToken cancellationToken);
    /// <summary>
    /// Deletes a single text, with its questions and answers from the store.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DeleteSingleText(string key, CancellationToken cancellationToken);
    /// <summary>
    /// Get all texts with their questions and answers.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<TextState>> GetAllTexts(CancellationToken cancellationToken);
    /// <summary>
    /// Get a single text with its questions and answers.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<TextState?> GetSingleText(string title, CancellationToken cancellationToken);
    /// <summary>
    /// Save a text with its questions and answers. If a text already exists with the same title, any new questions will be added to it.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SaveText(TextState text, CancellationToken cancellationToken);
}

public class DaprTextStateService(DaprClient daprClient, ILogger<DaprTextStateService> logger) : ITextStateService
{
    private const string StateStoreName = "imageapistatestorecomponent";
    private const string allKeysKey = "allKeys";

    public async Task DeleteAllTexts(CancellationToken cancellationToken)
    {
        var allKeys = await GetAllKeys(cancellationToken);
        var items = new List<BulkDeleteStateItem>(allKeys.Length);
        foreach (var key in allKeys)
        {
            items.Add(new BulkDeleteStateItem(key, null));
        }
        await daprClient.DeleteBulkStateAsync(StateStoreName, items, cancellationToken: cancellationToken);
        await SetAllKeys([], cancellationToken);
    }

    public async Task DeleteSingleText(string key, CancellationToken cancellationToken)
    {
        var allKeys = await GetAllKeys(cancellationToken);
        if (!allKeys.Contains(CreateKey(key)))
        {
            return;
        }

        await daprClient.DeleteStateAsync(StateStoreName, key, cancellationToken: cancellationToken);
        await SetAllKeys(allKeys.Where(k => !string.Equals(k, key, StringComparison.OrdinalIgnoreCase)).ToArray(), cancellationToken);
    }

    public async Task<IEnumerable<TextState>> GetAllTexts(CancellationToken cancellationToken)
    {
        try
        {
            //get all items from the state store:
            var allKeys = await GetAllKeys(cancellationToken);
            if (allKeys.Length > 0)
            {
                var state = await daprClient.GetBulkStateAsync<TextState>(StateStoreName, allKeys, 2, cancellationToken: cancellationToken);
                return state.Select(s => s.Value);
            }
            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get Text State from Dapr.");
            throw;
        }
    }

    public async Task<TextState?> GetSingleText(string title, CancellationToken cancellationToken)
    {
        try
        {
            var state = await daprClient.GetStateEntryAsync<TextState>(StateStoreName, CreateKey(title), cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(state.ETag))
                return null;
            return state.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to get Text State for '{title}' from Dapr.");
            throw;
        }
    }

    public async Task SaveText(TextState text, CancellationToken cancellationToken)
    {
        try
        {
            string key = CreateKey(text.Title);
            var state = await GetSingleText(key, cancellationToken: cancellationToken);
            //create new state:
            if (state is null)
            {
                state = text;

                //save key in list of all keys
                var allKeys = await GetAllKeys(cancellationToken);
                await SetAllKeys([.. allKeys, key], cancellationToken);
            }
            else
            {
                //add new questions to existing state:
                foreach (var question in text.Questions)
                {
                    if (!state.Value.Questions.Any(q => string.Equals(q.Question, question.Question, StringComparison.OrdinalIgnoreCase)))
                    {
                        state = state.Value with { Questions = state.Value.Questions.Append(question)!.ToArray() };
                    }
                }
            }
            StateOptions options = new()
            {
                Concurrency = ConcurrencyMode.LastWrite,
                Consistency = ConsistencyMode.Strong
            };
            //save questions
            await daprClient.SaveStateAsync(StateStoreName, key, state, options, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save Text State to Dapr.");
            throw;
        }
    }

    private async Task<string[]> GetAllKeys(CancellationToken cancellationToken)
    {
        var allKeys = await daprClient.GetStateAsync<string[]>(StateStoreName, allKeysKey, cancellationToken: cancellationToken);
        return allKeys ?? [];
    }

    private async Task SetAllKeys(string[] allKeys, CancellationToken cancellationToken)
    {
        await daprClient.SaveStateAsync(StateStoreName, allKeysKey, allKeys, cancellationToken: cancellationToken);
    }

    private static string CreateKey(string title)
    {
        return title.ToLowerInvariant();
    }
}