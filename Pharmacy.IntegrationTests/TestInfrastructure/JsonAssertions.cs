using System.Text.Json;

namespace Pharmacy.IntegrationTests.TestInfrastructure;

public static class JsonAssertions
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static async Task<bool> PagedItemsContainIdAsync(HttpResponseMessage response, int id)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        return document.RootElement
            .GetProperty("items")
            .EnumerateArray()
            .Any(item => item.GetProperty("id").GetInt32() == id);
    }

    public static async Task<int> ReadIdAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        return document.RootElement.GetProperty("id").GetInt32();
    }
}
