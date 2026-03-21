using System.Text;

namespace AccessControl.Web.Models;

public class ErrorViewModel
{
    public string Title { get; set; } = "Ошибка";
    public string Message { get; set; } = "Во время обработки запроса произошла ошибка.";
    public string? Details { get; set; }
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);
    public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

    public static ErrorViewModel FromException(string message, Exception? exception, string? requestId = null)
    {
        return new ErrorViewModel
        {
            Title = "Не удалось выполнить операцию",
            Message = message,
            Details = BuildDetails(exception),
            RequestId = requestId
        };
    }

    private static string? BuildDetails(Exception? exception)
    {
        if (exception is null)
        {
            return null;
        }

        var builder = new StringBuilder();
        AppendException(builder, exception, 0);
        return builder.ToString().Trim();
    }

    private static void AppendException(StringBuilder builder, Exception exception, int level)
    {
        if (builder.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine(new string('-', 80));
            builder.AppendLine();
        }

        builder.AppendLine($"Exception: {exception.GetType().FullName}");
        builder.AppendLine($"Message: {exception.Message}");

        if (exception is ApiClientException apiException)
        {
            builder.AppendLine($"HTTP status: {(int)apiException.StatusCode} ({apiException.StatusCode})");

            if (!string.IsNullOrWhiteSpace(apiException.ResponseTitle))
            {
                builder.AppendLine($"API title: {apiException.ResponseTitle}");
            }

            if (!string.IsNullOrWhiteSpace(apiException.ResponseDetail))
            {
                builder.AppendLine($"API detail: {apiException.ResponseDetail}");
            }

            if (apiException.ValidationErrors.Count > 0)
            {
                builder.AppendLine("Validation errors:");
                foreach (var entry in apiException.ValidationErrors)
                {
                    foreach (var error in entry.Value)
                    {
                        builder.AppendLine($"  {entry.Key}: {error}");
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(apiException.ResponseBody))
            {
                builder.AppendLine();
                builder.AppendLine("Raw API response:");
                builder.AppendLine(apiException.ResponseBody);
            }
        }

        if (!string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            builder.AppendLine();
            builder.AppendLine("Stack trace:");
            builder.AppendLine(exception.StackTrace);
        }

        if (exception.InnerException is not null)
        {
            AppendException(builder, exception.InnerException, level + 1);
        }
    }
}