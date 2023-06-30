using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace DurableFunctionsDemo;

public interface IBlobService
{
    Task UploadAsync(Stream content, string containerName, string blobFileName);
}

public class BlobService : IBlobService
{
    private readonly BlobServiceClient _blobServiceClient;

    public BlobService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task UploadAsync(Stream content, string containerName, string blobFileName)
    {
        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = blobContainerClient.GetBlobClient(blobFileName);
        await blobContainerClient.CreateIfNotExistsAsync();

        await blobClient.UploadAsync(content);
    }
}

public class BlobStreamWrapper : Stream
{
    private readonly BlobDownloadDetails _blobDownloadDetails;
    private readonly Stream _streamImplementation;

    public BlobStreamWrapper(Stream streamImplementation, BlobDownloadDetails blobDownloadDetails)
    {
        _streamImplementation = streamImplementation;
        _blobDownloadDetails = blobDownloadDetails;
    }

    public override bool CanRead => _streamImplementation.CanRead;

    public override bool CanSeek => _streamImplementation.CanSeek;

    public override bool CanWrite => _streamImplementation.CanWrite;

    public override long Length => _blobDownloadDetails.ContentLength;

    public override long Position
    {
        get => _streamImplementation.Position;
        set => _streamImplementation.Position = value;
    }

    public override void Flush()
    {
        _streamImplementation.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _streamImplementation.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _streamImplementation.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _streamImplementation.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _streamImplementation.Write(buffer, offset, count);
    }
}