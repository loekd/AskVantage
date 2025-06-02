namespace ImageApi.Models;

public readonly record struct ImageOcrResult(Guid ImageId, string Text);