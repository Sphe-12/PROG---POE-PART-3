using System.Net;
using System.Text;
using System.Text.Json;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

/// <summary>The ONLY channel between the MVC app and the Part 2 API. No DB access, no Azure access.</summary>
public class ApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public ApiClient(HttpClient http, IConfiguration config, IHttpContextAccessor accessor)
    {
        _accessor = accessor;
        http.BaseAddress = new Uri(config["Api:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5080");
        http.DefaultRequestHeaders.Accept.Clear();
        http.DefaultRequestHeaders.Accept.Add(new("application/json"));
        _http = http;
    }

    private ISession Session => _accessor.HttpContext!.Session;

    private void AttachSession(HttpRequestMessage req)
    {
        var cookie = Session.GetString("ApiSession");
        if (!string.IsNullOrEmpty(cookie))
            req.Headers.TryAddWithoutValidation("Cookie", $".RaceDay.Session={cookie}");
    }

    private static async Task<ApiException> ToError(HttpResponseMessage res)
    {
        var body = await res.Content.ReadAsStringAsync();
        var message = "Something went wrong. Please try again.";
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.String)
                message = doc.RootElement.GetString() ?? message;
            else if (doc.RootElement.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                message = t.GetString() ?? message;
        }
        catch { if (!string.IsNullOrWhiteSpace(body) && body.Length < 300) message = body.Trim(); }
        if (res.StatusCode == HttpStatusCode.Unauthorized) message = "Your session has expired. Please sign in again.";
        if (res.StatusCode == HttpStatusCode.Forbidden) message = "You do not have permission to do that.";
        return new ApiException((int)res.StatusCode, message);
    }

    public async Task<T?> GetAsync<T>(string uri, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, uri); AttachSession(req);
        var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode) throw await ToError(res);
        if (res.StatusCode == HttpStatusCode.NoContent) return default;
        return JsonSerializer.Deserialize<T>(await res.Content.ReadAsStringAsync(ct), Json);
    }

    public async Task<(byte[] Data, string ContentType)?> GetImageAsync(string uri, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, uri); AttachSession(req);
        var res = await _http.SendAsync(req, ct);
        if (res.StatusCode == HttpStatusCode.NotFound) return null;
        if (!res.IsSuccessStatusCode) throw await ToError(res);
        var data = await res.Content.ReadAsByteArrayAsync(ct);
        var contentType = res.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        return (data, contentType);
    }

    public async Task<TResponse?> SendAsync<TResponse>(HttpMethod method, string uri, object? body = null, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(method, uri); AttachSession(req);
        if (body is not null)
            req.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
        var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode) throw await ToError(res);
        if (res.StatusCode == HttpStatusCode.NoContent) return default;
        return JsonSerializer.Deserialize<TResponse>(await res.Content.ReadAsStringAsync(ct), Json);
    }

    public async Task SendAsync(HttpMethod method, string uri, object? body = null, CancellationToken ct = default)
        => await SendAsync<object>(method, uri, body, ct);

    public async Task<TResponse?> PostFileAsync<TResponse>(string uri, IFormFile file, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, uri); AttachSession(req);
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new(file.ContentType);
        content.Add(fileContent, "file", file.FileName);
        req.Content = content;
        var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode) throw await ToError(res);
        var json = await res.Content.ReadAsStringAsync(ct);
        return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<TResponse>(json, Json);
    }

    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { email, password }, Json), Encoding.UTF8, "application/json")
        };
        var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode) throw await ToError(res);
        if (res.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            const string marker = ".RaceDay.Session=";
            foreach (var c in cookies)
            {
                var i = c.IndexOf(marker, StringComparison.Ordinal);
                if (i < 0) continue;
                var rest = c[(i + marker.Length)..];
                var end = rest.IndexOf(';');
                Session.SetString("ApiSession", end >= 0 ? rest[..end] : rest);
            }
        }
        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<LoginResult>(json, Json) ?? throw new ApiException(500, "Unexpected login response.");
    }

    public async Task LogoutApiAsync()
    {
        try { await SendAsync(HttpMethod.Post, "/api/auth/logout"); } catch { /* best effort */ }
    }
}
