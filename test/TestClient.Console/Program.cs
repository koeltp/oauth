using System.Net.Http.Headers;
using System.Text.Json;

var oauthBaseUrl = "https://localhost:7266";
var clientId = "test-client-id";
var clientSecret = "test-client-secret";

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

while (true)
{
    Console.Clear();
    Console.WriteLine("===================================");
    Console.WriteLine("  OAuth Client Credentials 测试套件");
    Console.WriteLine("===================================");
    Console.WriteLine();
    Console.WriteLine($"  OAuth 服务器: {oauthBaseUrl}");
    Console.WriteLine($"  ClientId:     {clientId}");
    Console.WriteLine();
    Console.WriteLine("  [1] Client Credentials - 正常流程");
    Console.WriteLine("  [2] Client Credentials - 无效 client_secret");
    Console.WriteLine("  [3] Client Credentials - 不同 Scope 测试");
    Console.WriteLine("  [4] 无效 Token 调用 UserInfo");
    Console.WriteLine("  [5] 手动输入 Token 测试 UserInfo");
    Console.WriteLine("  [9] 所有测试一键运行");
    Console.WriteLine();
    Console.WriteLine("  [0] 退出");
    Console.WriteLine();
    Console.Write("  请选择: ");
    var choice = Console.ReadLine()?.Trim();

    switch (choice)
    {
        case "1": await TestClientCredentialsAsync(httpClient, oauthBaseUrl, clientId, clientSecret); break;
        case "2": await TestInvalidSecretAsync(httpClient, oauthBaseUrl, clientId); break;
        case "3": await TestScopesAsync(httpClient, oauthBaseUrl, clientId, clientSecret); break;
        case "4": await TestInvalidTokenAsync(httpClient, oauthBaseUrl); break;
        case "5": await TestManualTokenAsync(httpClient, oauthBaseUrl); break;
        case "9":
            await TestClientCredentialsAsync(httpClient, oauthBaseUrl, clientId, clientSecret);
            await TestInvalidSecretAsync(httpClient, oauthBaseUrl, clientId);
            await TestScopesAsync(httpClient, oauthBaseUrl, clientId, clientSecret);
            await TestInvalidTokenAsync(httpClient, oauthBaseUrl);
            break;
        case "0": return;
        default:
            Console.WriteLine("无效选择，按任意键继续...");
            Console.ReadKey();
            break;
    }
}

// ==================== 测试 1: Client Credentials 正常流程 ====================
static async Task TestClientCredentialsAsync(HttpClient httpClient, string baseUrl, string clientId, string clientSecret)
{
    PrintHeader("Client Credentials - 正常流程");

    var tokenParams = new List<KeyValuePair<string, string>>
    {
        new("grant_type", "client_credentials"),
        new("client_id", clientId),
        new("client_secret", clientSecret),
        new("scope", "api")
    };

    var tokenResponse = await httpClient.PostAsync($"{baseUrl}/connect/token",
        new FormUrlEncodedContent(tokenParams));
    var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

    if (!tokenResponse.IsSuccessStatusCode)
    {
        PrintResult("请求 Token 失败", false, (int)tokenResponse.StatusCode, tokenBody);
        WaitForContinue();
        return;
    }

    PrintResult("Token 获取成功", true, 200, PrettyJson(tokenBody));
    var tokenDoc = JsonDocument.Parse(tokenBody);
    var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();
    var tokenType = tokenDoc.RootElement.TryGetProperty("token_type", out var tt) ? tt.GetString() : "Bearer";

    Console.WriteLine("\n  >>> Step 2: 用 access_token 调用 UserInfo API...");
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(tokenType ?? "Bearer", accessToken);
    var userInfoResponse = await httpClient.GetAsync($"{baseUrl}/connect/userinfo");
    var userInfoBody = await userInfoResponse.Content.ReadAsStringAsync();

    if (userInfoResponse.IsSuccessStatusCode)
    {
        PrintResult("UserInfo 获取成功", true, 200, PrettyJson(userInfoBody));
    }
    else
    {
        PrintResult("UserInfo 获取失败", false, (int)userInfoResponse.StatusCode, userInfoBody);
    }

    WaitForContinue();
}

// ==================== 测试 2: 无效 client_secret ====================
static async Task TestInvalidSecretAsync(HttpClient httpClient, string baseUrl, string clientId)
{
    PrintHeader("Client Credentials - 无效 client_secret");

    var tokenParams = new List<KeyValuePair<string, string>>
    {
        new("grant_type", "client_credentials"),
        new("client_id", clientId),
        new("client_secret", "wrong-secret-12345"),
        new("scope", "api")
    };

    var tokenResponse = await httpClient.PostAsync($"{baseUrl}/connect/token",
        new FormUrlEncodedContent(tokenParams));
    var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

    if (!tokenResponse.IsSuccessStatusCode)
    {
        PrintResult("正确！服务端拒绝了无效 secret", true, (int)tokenResponse.StatusCode, tokenBody);
    }
    else
    {
        PrintResult("注意！无效 secret 竟通过了", false, 200, tokenBody);
    }

    WaitForContinue();
}

// ==================== 测试 3: 不同 Scope 测试 ====================
static async Task TestScopesAsync(HttpClient httpClient, string baseUrl, string clientId, string clientSecret)
{
    PrintHeader("Client Credentials - 不同 Scope 测试");

    var scopesToTest = new[] { "api", "openid", "openid profile", "openid email", "unknown_scope" };

    foreach (var scope in scopesToTest)
    {
        Console.Write($"  >>> Scope = \"{scope}\" ... ");

        var tokenParams = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials"),
            new("client_id", clientId),
            new("client_secret", clientSecret),
            new("scope", scope)
        };

        var response = await httpClient.PostAsync($"{baseUrl}/connect/token",
            new FormUrlEncodedContent(tokenParams));
        var body = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var doc = JsonDocument.Parse(body);
            var scopes = doc.RootElement.TryGetProperty("scope", out var s) ? s.GetString() : "无";
            Console.WriteLine($"成功 | 返回 scope: {scopes}");
        }
        else
        {
            Console.WriteLine($"失败 ({(int)response.StatusCode})");
        }
    }

    WaitForContinue();
}

// ==================== 测试 4: 无效 Token 调用 UserInfo ====================
static async Task TestInvalidTokenAsync(HttpClient httpClient, string baseUrl)
{
    PrintHeader("无效 Token 测试");

    var fakeTokens = new[]
    {
        "eyJhbGciOiJIUzI1NiJ9.fake.xyz",
        "invalid-token-format",
        ""
    };

    foreach (var token in fakeTokens)
    {
        Console.Write($"  >>> Token = \"{token[..Math.Min(20, token.Length)]}...\" => ");

        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);

        var response = await httpClient.GetAsync($"{baseUrl}/connect/userinfo");
        var body = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"{(int)response.StatusCode} (期望: 401)");
    }

    httpClient.DefaultRequestHeaders.Authorization = null;
    Console.WriteLine("  >>> 无 Authorization Header => ");
    var noAuthResponse = await httpClient.GetAsync($"{baseUrl}/connect/userinfo");
    var noAuthBody = await noAuthResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"  {(int)noAuthResponse.StatusCode} (期望: 401)");

    WaitForContinue();
}

// ==================== 测试 5: 手动输入 Token 测试 UserInfo ====================
static async Task TestManualTokenAsync(HttpClient httpClient, string baseUrl)
{
    PrintHeader("手动输入 Token 测试");

    Console.Write("  请输入 access_token: ");
    var token = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(token))
    {
        Console.WriteLine("  未输入 token，跳过.");
        WaitForContinue();
        return;
    }

    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    var response = await httpClient.GetAsync($"{baseUrl}/connect/userinfo");
    var body = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        PrintResult("Token 有效，UserInfo:", true, 200, PrettyJson(body));
    }
    else
    {
        PrintResult("Token 无效或已过期", false, (int)response.StatusCode, body);
    }

    WaitForContinue();
}

// ==================== 工具方法 ====================
static void PrintHeader(string title)
{
    Console.Clear();
    Console.WriteLine("--- " + title + " ---");
    Console.WriteLine();
}

static void PrintResult(string message, bool success, int statusCode, string body)
{
    var icon = success ? "[OK]" : "[FAIL]";
    Console.WriteLine($"  {icon} {message}");
    Console.WriteLine($"  状态码: {statusCode}");
    Console.WriteLine($"  响应体:");
    Console.WriteLine($"  {body.Replace("\n", "\n  ")}");
    Console.WriteLine();
}

static string PrettyJson(string json)
{
    try
    {
        return JsonSerializer.Serialize(
            JsonDocument.Parse(json).RootElement,
            new JsonSerializerOptions { WriteIndented = true }
        );
    }
    catch
    {
        return json;
    }
}

static void WaitForContinue()
{
    Console.WriteLine();
    Console.Write("  按任意键继续...");
    Console.ReadKey();
    Console.WriteLine();
}