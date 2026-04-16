namespace SecondHandShop.Application.Abstractions.ImageProcessing;

public sealed class BackgroundRemovalResult(Stream content, string contentType) : IDisposable, IAsyncDisposable
{
    public Stream Content { get; } = content;
    public string ContentType { get; } = contentType;

    public void Dispose()
    {
        Content.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        if (Content is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }

        Content.Dispose();
        return ValueTask.CompletedTask;
    }
}

public interface IBackgroundRemovalService
{
    Task<BackgroundRemovalResult> RemoveBackgroundAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
