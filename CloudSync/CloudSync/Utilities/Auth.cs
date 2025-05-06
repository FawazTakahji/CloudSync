using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using Pathoschild.Http.Client;

namespace CloudSync.Utilities;

public static class Auth
{
    public static (string codeVerifier, string codeChallenge) GeneratePkce(int size = 32)
    {
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        byte[] bytes = new byte[size];
        rng.GetBytes(bytes);

        string codeVerifier = Base64UrlEncode(bytes);

        byte[] buffer = Encoding.UTF8.GetBytes(codeVerifier);
        byte[] hash = SHA256.Create().ComputeHash(buffer);

        string codeChallenge = Base64UrlEncode(hash);

        return (codeVerifier, codeChallenge);
    }

    public static async Task<JObject?> GetTokenResponse(string url, Dictionary<string, string?> dict) =>
        await new FluentClient()
            .PostAsync(url)
            .WithBody(p => p.FormUrlEncoded(dict))
            .AsRawJsonObject();

    public static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

    public static int GetRandomUnusedPort()
    {
        TcpListener listener = new(IPAddress.Any, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}