namespace AskVantage.Frontend.Client.Models;

public readonly record struct ImageOcrResult
{
    public Guid ImageId { get; init; }

    public string Text { get; init; }
}