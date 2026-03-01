namespace SecondHandShop.Application.Abstractions.ImageProcessing;

public interface IBackgroundRemovalService
{
    Task<byte[]> RemoveBackgroundAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
