namespace ImageApi.Models;

public readonly record struct ImageOcrResult
{
    public Guid ImageId { get; init; }

    public string Text { get; init; }
}