using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Asp.Versioning;

namespace McpServer.Controllers;

/// <summary>
/// Forwards the React client's chat completion requests to the configured Azure
/// OpenAI deployment with the server's key, so the browser never holds the key.
/// The body is passed on as it is and Azure's response returned as it is.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/chat/completions")]
[Produces("application/json")]
public class ChatCompletionsController : ControllerBase
{
    // Limits on what a caller can ask for, since the server pays
    private const int MaxTokens = 4000;
    private const int MaxChoices = 1;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AzureOpenAIOptions _options;
    private readonly ILogger<ChatCompletionsController> _logger;

    public ChatCompletionsController(
        IHttpClientFactory httpClientFactory,
        IOptions<AzureOpenAIOptions> options,
        ILogger<ChatCompletionsController> logger
    )
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCompletion([FromBody] JsonElement body)
    {
        if (
            string.IsNullOrEmpty(_options.Endpoint)
            || string.IsNullOrEmpty(_options.ApiKey)
            || string.IsNullOrEmpty(_options.ChatCompletionDeploymentName)
        )
        {
            return StatusCode(
                503,
                new
                {
                    success = false,
                    error = "Azure OpenAI isn't configured: set AOAI_ENDPOINT, AOAI_APIKEY and CHATCOMPLETION_DEPLOYMENTNAME on the server"
                }
            );
        }

        if (TooLarge(body, "max_tokens", MaxTokens) || TooLarge(body, "n", MaxChoices))
        {
            return BadRequest(
                new
                {
                    success = false,
                    error = $"max_tokens can be at most {MaxTokens} and n at most {MaxChoices}"
                }
            );
        }

        var url =
            $"{_options.Endpoint.TrimEnd('/')}/openai/deployments/{_options.ChatCompletionDeploymentName}"
            + $"/chat/completions?api-version={_options.ApiVersion}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("api-key", _options.ApiKey);

        try
        {
            using var response = await _httpClientFactory
                .CreateClient()
                .SendAsync(request, HttpContext.RequestAborted);
            var content = await response.Content.ReadAsStringAsync(HttpContext.RequestAborted);
            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = content,
                ContentType = "application/json"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Azure OpenAI");
            return StatusCode(502, new { success = false, error = "Azure OpenAI couldn't be reached" });
        }
        catch (TaskCanceledException ex) when (!HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogError(ex, "Azure OpenAI timed out");
            return StatusCode(504, new { success = false, error = "Azure OpenAI didn't answer in time" });
        }
    }

    /// <summary>Whether a number in the body is over its limit.</summary>
    private static bool TooLarge(JsonElement body, string name, int limit) =>
        body.ValueKind == JsonValueKind.Object
        && body.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Number
        && (!value.TryGetInt32(out var number) || number > limit);
}
