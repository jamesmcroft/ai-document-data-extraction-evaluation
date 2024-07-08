using System.Text.Json;

namespace EvaluationTests.Shared.Storage;

public class TestOutputStorage
{
    private readonly string _path;

    public TestOutputStorage(string testName, string endpointKey, bool asMarkdown)
    {
        var techniquePart = asMarkdown ? "Markdown" : "Vision";
        _path = $"Output/{testName}/{endpointKey}/{techniquePart}";

        if (!Directory.Exists(_path))
        {
            Directory.CreateDirectory(_path);
        }
    }

    public async Task SaveBytesAsync(byte[] data, string fileName)
    {
        var filePath = Path.Combine(_path, fileName);
        await File.WriteAllBytesAsync(filePath, data);
    }

    public async Task SaveJsonAsync<T>(T data, string fileName)
        where T : class
    {
        var filePath = Path.Combine(_path, fileName);
        await File.WriteAllTextAsync(filePath,
            JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }
}
