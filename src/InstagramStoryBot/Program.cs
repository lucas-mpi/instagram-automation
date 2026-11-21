using System.Text.Json;
using InstagramStoryBot.Configuration;
using InstagramStoryBot.Services;

Console.WriteLine("==============================================================");
Console.WriteLine("  Automação de Conteúdo para Estética - Instagram Stories Bot");
Console.WriteLine("==============================================================");

var options = BotOptions.Load(args);
using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

var supabaseService = new SupabaseService(httpClient, options);
var geminiService = new GeminiService(httpClient, options);
var rendererService = new StoryRendererService(options);
var instagramService = new InstagramService(httpClient, options);

// 1. Selecionar próxima pauta do banco (com rotação de serviço e ultimo_uso mais antigo)
var tema = await supabaseService.SelecionarProximoTemaAsync();
Console.WriteLine();
Console.WriteLine($"[1/4] Pauta Selecionada: #{tema.Id} ({tema.ObterNomeServicoLegivel()} | {tema.ObterNomeFormatoLegivel()})");
Console.WriteLine($"      Tema: {tema.Tema}");

// 2. Gerar conteúdo estruturado via Gemini API (Structured Outputs)
Console.WriteLine();
Console.WriteLine("[2/4] Gerando roteiro JSON com Google Gemini...");
var storyContent = await geminiService.GerarConteudoStoryAsync(tema);

Console.WriteLine(JsonSerializer.Serialize(storyContent, new JsonSerializerOptions { WriteIndented = true }));

// 3. Renderizar HTML/CSS em imagem PNG (1080x1920px) via Playwright
Console.WriteLine();
Console.WriteLine("[3/4] Renderizando imagem 1080x1920px com Playwright...");
string outputFileName = $"story_tema_{tema.Id:D2}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
string localPngPath = Path.Combine(options.OutputDirectory, outputFileName);
await rendererService.RenderizarStoryPngAsync(tema, storyContent, localPngPath);

// 4. Upload no Supabase Storage + Publicação via Instagram Graph API + Atualização de ultimo_uso
Console.WriteLine();
if (options.DryRun || !options.HasSupabaseConfig || !options.HasInstagramConfig)
{
    Console.WriteLine("[4/4] Modo Dry-Run / Local ativo (ou credenciais de publicação ausentes).");
    Console.WriteLine($"      Imagem gerada localmente em: {localPngPath}");
    return;
}

Console.WriteLine("[4/4] Fazendo upload para o Supabase Storage e publicando no Instagram...");
string publicImageUrl = await supabaseService.UploadImagemStoryAsync(localPngPath);
await instagramService.PublicarStoryAsync(publicImageUrl);
await supabaseService.AtualizarUltimoUsoAsync(tema.Id);

Console.WriteLine();
Console.WriteLine("Fluxo finalizado com sucesso!");
