using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using InstagramStoryBot.Configuration;
using InstagramStoryBot.Models;

namespace InstagramStoryBot.Services;

public sealed class SupabaseService(HttpClient httpClient, BotOptions options)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<TemaSeed> SelecionarProximoTemaAsync(CancellationToken ct = default)
    {
        if (options.HasSupabaseConfig)
        {
            return await SelecionarTemaDoSupabaseAsync(ct);
        }

        Console.WriteLine("[Supabase] Variáveis SUPABASE_URL/SUPABASE_KEY não configuradas. Usando banco local (Data/temas-seed.json).");
        return await SelecionarTemaLocalAsync(ct);
    }

    private async Task<TemaSeed> SelecionarTemaDoSupabaseAsync(CancellationToken ct)
    {
        string url = $"{options.SupabaseUrl}/rest/v1/temas?select=*&ativo=eq.true";
        using var req = CriarRequest(HttpMethod.Get, url);
        using var resp = await httpClient.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var todosTemas = await resp.Content.ReadFromJsonAsync<List<TemaSeed>>(JsonOpts, ct)
                         ?? throw new InvalidOperationException("A tabela 'temas' do Supabase retornou vazia.");

        return EscolherTemaComRotacaoDeServico(todosTemas, options.ForcedTemaId);
    }

    private async Task<TemaSeed> SelecionarTemaLocalAsync(CancellationToken ct)
    {
        string seedPath = ResolverCaminhoSeedLocal();
        await using var stream = File.OpenRead(seedPath);
        var todosTemas = await JsonSerializer.DeserializeAsync<List<TemaSeed>>(stream, JsonOpts, ct)
                         ?? throw new InvalidOperationException("Arquivo temas-seed.json está vazio.");

        var temasAtivos = todosTemas.Where(t => t.Ativo).ToList();
        return EscolherTemaComRotacaoDeServico(temasAtivos, options.ForcedTemaId);
    }

    private static TemaSeed EscolherTemaComRotacaoDeServico(List<TemaSeed> todosTemas, int? forcedTemaId)
    {
        if (todosTemas.Count == 0)
            throw new InvalidOperationException("Nenhum tema cadastrado no banco de pautas.");

        if (forcedTemaId.HasValue)
        {
            var forçado = todosTemas.FirstOrDefault(t => t.Id == forcedTemaId.Value);
            if (forçado is not null)
                return forçado;
        }

        // Identifica qual foi o último serviço publicado (o registro com ultimo_uso mais recente)
        string? ultimoServicoPublicado = todosTemas
            .Where(t => t.UltimoUso.HasValue)
            .OrderByDescending(t => t.UltimoUso!.Value)
            .Select(t => t.Servico)
            .FirstOrDefault();

        // Ordena por ultimo_uso mais antigo (nulls primeiro) e depois por Id
        var candidatosOrdenados = todosTemas
            .OrderBy(t => t.UltimoUso.HasValue ? 1 : 0)
            .ThenBy(t => t.UltimoUso)
            .ThenBy(t => t.Id)
            .ToList();

        if (!string.IsNullOrEmpty(ultimoServicoPublicado))
        {
            var diferenteDoAnterior = candidatosOrdenados.FirstOrDefault(t =>
                !t.Servico.Equals(ultimoServicoPublicado, StringComparison.OrdinalIgnoreCase));

            if (diferenteDoAnterior is not null)
                return diferenteDoAnterior;
        }

        return candidatosOrdenados[0];
    }

    public async Task AtualizarUltimoUsoAsync(int temaId, CancellationToken ct = default)
    {
        if (!options.HasSupabaseConfig)
        {
            Console.WriteLine($"[Supabase] Dry/Local mode: ultimo_uso do tema #{temaId} seria atualizado agora.");
            return;
        }

        string url = $"{options.SupabaseUrl}/rest/v1/temas?id=eq.{temaId}";
        using var req = CriarRequest(HttpMethod.Patch, url);
        req.Headers.Add("Prefer", "return=minimal");

        var payload = new { ultimo_uso = DateTimeOffset.UtcNow.ToString("O") };
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var resp = await httpClient.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        Console.WriteLine($"[Supabase] Tema #{temaId} atualizado com ultimo_uso = {payload.ultimo_uso}.");
    }

    public async Task<(string PublicUrl, string FileName)> UploadImagemStoryAsync(string localFilePath, CancellationToken ct = default)
    {
        if (!options.HasSupabaseConfig)
            throw new InvalidOperationException("SUPABASE_URL e SUPABASE_KEY são obrigatórios para fazer upload da imagem.");

        string fileName = $"story_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.png";
        string uploadUrl = $"{options.SupabaseUrl}/storage/v1/object/{options.SupabaseBucket}/{fileName}";

        byte[] fileBytes = await File.ReadAllBytesAsync(localFilePath, ct);

        using var req = CriarRequest(HttpMethod.Post, uploadUrl);
        req.Headers.Add("x-upsert", "true");
        req.Content = new ByteArrayContent(fileBytes);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        using var resp = await httpClient.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            string erroBody = await resp.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Erro ao fazer upload no Supabase Storage ({resp.StatusCode}): {erroBody}");
        }

        string publicUrl = $"{options.SupabaseUrl}/storage/v1/object/public/{options.SupabaseBucket}/{fileName}";
        Console.WriteLine($"[Supabase Storage] Imagem temporária enviada com sucesso ({fileName}).");
        return (publicUrl, fileName);
    }

    public async Task RemoverImagemStoryAsync(string fileName, CancellationToken ct = default)
    {
        if (!options.HasSupabaseConfig || string.IsNullOrWhiteSpace(fileName))
            return;

        string deleteUrl = $"{options.SupabaseUrl}/storage/v1/object/{options.SupabaseBucket}/{fileName}";
        using var req = CriarRequest(HttpMethod.Delete, deleteUrl);
        using var resp = await httpClient.SendAsync(req, ct);

        if (resp.IsSuccessStatusCode)
        {
            Console.WriteLine($"[Supabase Storage] Imagem temporária removida do bucket ({fileName}).");
        }
        else
        {
            Console.WriteLine($"[Supabase Storage] Aviso: Não foi possível remover a imagem temporária ({resp.StatusCode}).");
        }
    }

    private HttpRequestMessage CriarRequest(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("apikey", options.SupabaseKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.SupabaseKey);
        return req;
    }

    private static string ResolverCaminhoSeedLocal()
    {
        string[] candidatos =
        [
            Path.Combine(AppContext.BaseDirectory, "Data", "temas-seed.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "temas-seed.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "InstagramStoryBot", "Data", "temas-seed.json")
        ];

        foreach (string path in candidatos)
        {
            if (File.Exists(path))
                return path;
        }

        throw new FileNotFoundException("Não foi possível localizar Data/temas-seed.json.");
    }
}
