public async Task<string> UploadAsync(string container, string blobName, Stream data, string contentType, CancellationToken ct = default)
{
    var path = PathFor(container, blobName);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
    await data.CopyToAsync(fs, ct);
    await File.WriteAllTextAsync(path + ".meta", contentType, ct);
    return blobName;
}
