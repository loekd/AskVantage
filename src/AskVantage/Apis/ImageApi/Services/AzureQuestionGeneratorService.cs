using Azure;
using Azure.AI.OpenAI;
using OpenAI.Assistants;
using System.ClientModel;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace ImageApi.Services;

public class AzureQuestionGeneratorService(AzureOpenAIClient openAIClient, ILogger<AzureQuestionGeneratorService> logger) : IQuestionGeneratorService
{
    private const string AssistantName = "questionGenerator";

    public async Task<IEnumerable<QuestionAnswerResponse>> GenerateQuestions(string input, CancellationToken cancellationToken)
    {
#pragma warning disable OPENAI001
        var assistantClient = openAIClient.GetAssistantClient();
        var assistant = await EnsureAssistantCreated(assistantClient, cancellationToken);

        //create thread
        ThreadCreationOptions threadOptions = new()
        {
            InitialMessages = { input }
        };
        var threadResponse = await assistantClient.CreateThreadAsync(threadOptions, cancellationToken);
        RunCreationOptions runOptions = new();

        var builder = new StringBuilder();

        await foreach (var streamingUpdate in assistantClient.CreateRunStreamingAsync(threadResponse.Value.Id, assistant.Id, runOptions, cancellationToken))
        {
            switch (streamingUpdate.UpdateKind)
            {
                case StreamingUpdateReason.RunCreated:
                    logger.LogInformation("--- Run started! ---");
                    break;
                case StreamingUpdateReason.RunFailed when streamingUpdate is RunUpdate runUpdate:
                    logger.LogError("Run failed. Error: {errorMessage}", runUpdate.Value.LastError.Message);
                    break;
                default:
                {
                    if (streamingUpdate is MessageContentUpdate contentUpdate)
                    {                
                        builder.Append(contentUpdate.Text);
                    }

                    break;
                }
            }
        }

        _ = await assistantClient.DeleteThreadAsync(threadResponse.Value.Id, cancellationToken);

        string text = builder.ToString();
        text = text.Replace("```json", string.Empty);
        text = text.Replace("```", string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
            text = "[]";

        return JsonSerializer.Deserialize<QuestionAnswerResponse[]>(text) ?? [];        
    }


    private async Task<Assistant> EnsureAssistantCreated(AssistantClient assistantClient, CancellationToken cancellationToken)
    {
        Assistant? assistant = null;
        try
        {

            var assistantsResponse = assistantClient.GetAssistantsAsync(cancellationToken: cancellationToken);
            await foreach (var r in assistantsResponse)
            {
                if (r.Name == AssistantName)
                {
                    assistant = r;
                    break;
                }
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            logger.LogWarning("Assistant not found, creating new assistant");
            // The assistant does not exist, so create it
        }

        if (assistant != null)
        {
            logger.LogTrace("Assistant found, re-using it.");
            return assistant;
        }
        
        const string modelName = "gpt-4o";
        logger.LogTrace("Creating new assistant using model name: {modelName}", modelName);

        var assistantResponse = await assistantClient.CreateAssistantAsync(modelName, new AssistantCreationOptions
        {
            Name = AssistantName,
            Instructions = """
                           You are an assistant that will help students learn. Students will upload text that was created by taking a photograph of a book. The text may contain typo's.
                                                        You will create 3 relevant questions that the student will likely need to answer about the text. Also add the answers and if possible, a reference to where in the input the answer could be found.
                                                        Never make up any facts.
                                                        JSON Schema: [{"question": "some question", "answer": "the answer", "reference": "reference to answer"}]
                           """,
            Tools = { }
        }, cancellationToken);

        return assistantResponse.Value;
    }
}
