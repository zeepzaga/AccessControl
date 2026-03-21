using System.Net;
using System.Text.Json;

namespace AccessControl.Web;

public sealed class ApiClientException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? ResponseTitle { get; }
    public string? ResponseDetail { get; }
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }
    public string? ResponseBody { get; }

    private ApiClientException(
        string message,
        HttpStatusCode statusCode,
        string? responseTitle,
        string? responseDetail,
        IReadOnlyDictionary<string, string[]> validationErrors,
        string? responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseTitle = responseTitle;
        ResponseDetail = responseDetail;
        ValidationErrors = validationErrors;
        ResponseBody = responseBody;
    }

    public static async Task<ApiClientException> FromResponseAsync(HttpResponseMessage response)
    {
        var responseBody = response.Content is null
            ? null
            : await response.Content.ReadAsStringAsync();

        string? title = null;
        string? detail = null;
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            try
            {
                using var document = JsonDocument.Parse(responseBody);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                    {
                        title = titleProp.GetString();
                    }

                    if (root.TryGetProperty("detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
                    {
                        detail = detailProp.GetString();
                    }

                    if (root.TryGetProperty("message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(detail))
                    {
                        detail = messageProp.GetString();
                    }

                    if (root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var property in errorsProp.EnumerateObject())
                        {
                            if (property.Value.ValueKind != JsonValueKind.Array)
                            {
                                continue;
                            }

                            errors[property.Name] = property.Value
                                .EnumerateArray()
                                .Where(item => item.ValueKind == JsonValueKind.String)
                                .Select(item => item.GetString())
                                .Where(item => !string.IsNullOrWhiteSpace(item))
                                .Cast<string>()
                                .ToArray();
                        }
                    }
                }
            }
            catch
            {
                // Raw body will still be available in technical details.
            }
        }

        var message = title
            ?? detail
            ?? $"API returned {(int)response.StatusCode} {response.ReasonPhrase}";

        return new ApiClientException(message, response.StatusCode, title, detail, errors, responseBody);
    }
}