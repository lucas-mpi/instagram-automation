using System.Text;
using System.Text.Json;
using InstagramStoryBot.Configuration;

namespace InstagramStoryBot.Services;

public sealed class InstagramService(HttpClient httpClient, BotOptions options)
{
    private const string GraphApiVersion = "v21.0";
    private const string GraphBaseUrl = $"https://graph.facebook.com/{GraphApiVersion}";

    public async Task<string> PublicarStoryAsync(string publicImageUrl, CancellationToken ct = default)
    {
        if (!options.HasInstagramConfig)
        {
            throw new InvalidOperationException(
                "INSTAGRAM_ACCESS_TOKEN e INSTAGRAM_USER_ID são obrigatórios para publicar no Instagram.");
        }

        // Passo 1: Gerar o Container de Mídia do Story
        string creationId = await CriarContainerStoryAsync(publicImageUrl, ct);
        Console.WriteLine($"[Instagram API] Container de Story criado com sucesso. creation_id = {creationId}");

        // Aguarda alguns segundos para a Meta baixar e processar o arquivo do Supabase Storage
        await AguardarProcessamentoContainerAsync(creationId, ct);

        // Passo 2: Publicar o Container de Mídia
        string mediaId = await PublicarContainerStoryAsync(creationId, ct);
        Console.WriteLine($"[Instagram API] Story publicado com sucesso! media_id = {mediaId}");

        return mediaId;
    }

    private async Task<string> CriarContainerStoryAsync(string publicImageUrl, CancellationToken ct)
    {
        string url = $"{GraphBaseUrl}/{options.InstagramUserId}/media";

        var payload = new
        {
            media_type = "STORIES",
            image_url = publicImageUrl,
            access_token = options.InstagramAccessToken
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var resp = await httpClient.PostAsync(url, content, ct);
        string body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Erro ao criar container de Story no Instagram ({resp.StatusCode}): {body}");
        }

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("id").GetString()
               ?? throw new InvalidOperationException("A Instagram Graph API não retornou um creation_id válido.");
    }

    private async Task AguardarProcessamentoContainerAsync(string creationId, CancellationToken ct)
    {
        string statusUrl = $"{GraphBaseUrl}/{creationId}?fields=status_code&access_token={Uri.EscapeDataString(options.InstagramAccessToken)}";

        for (int tentativa = 1; tentativa <= 6; tentativa++)
        {
            await Task.Delay(TimeSpan.FromSeconds(3), ct);

            using var resp = await httpClient.GetAsync(statusUrl, ct);
            if (!resp.IsSuccessStatusCode)
                continue;

            string body = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);

            if (doc.RootElement.TryGetProperty("status_code", out var statusProp))
            {
                string? status = statusProp.GetString();
                Console.WriteLine($"[Instagram API] Status do container ({tentativa}/6): {status}");

                if (string.Equals(status, "FINISHED", StringComparison.OrdinalIgnoreCase))
                    return;

                if (string.Equals(status, "ERROR", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"O processamento do container de mídia falhou na Meta: {body}");
            }
            else
            {
                return;
            }
        }
    }

    private async Task<string> PublicarContainerStoryAsync(string creationId, CancellationToken ct)
    {
        string url = $"{GraphBaseUrl}/{options.InstagramUserId}/media_publish";

        var payload = new
        {
            creation_id = creationId,
            access_token = options.InstagramAccessToken
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var resp = await httpClient.PostAsync(url, content, ct);
        string body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Erro ao publicar container do Story no Instagram ({resp.StatusCode}): {body}");
        }

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("id").GetString() ?? creationId;
    }
}
