using System.Text.RegularExpressions;
using InstagramStoryBot.Configuration;
using InstagramStoryBot.Models;
using Microsoft.Playwright;

namespace InstagramStoryBot.Services;

public sealed partial class StoryRendererService(BotOptions options)
{
    public async Task<string> RenderizarStoryPngAsync(TemaSeed tema, StoryContent content, string outputPath)
    {
        string templatePath = ResolverCaminhoTemplate();
        string htmlTemplate = await File.ReadAllTextAsync(templatePath);

        string htmlFinal = htmlTemplate
            .Replace("{{SERVICO}}", tema.Servico.ToLowerInvariant())
            .Replace("{{CATEGORIA}}", content.CategoriaTag)
            .Replace("{{TITULO}}", content.TituloCard)
            .Replace("{{SUBTITULO}}", content.SubtituloCard)
            .Replace("{{TOPICO_1}}", FormatarTopicoHtml(content.TopicosCard.ElementAtOrDefault(0) ?? string.Empty))
            .Replace("{{TOPICO_2}}", FormatarTopicoHtml(content.TopicosCard.ElementAtOrDefault(1) ?? string.Empty))
            .Replace("{{TOPICO_3}}", FormatarTopicoHtml(content.TopicosCard.ElementAtOrDefault(2) ?? string.Empty))
            .Replace("{{CTA}}", content.CtaBox)
            .Replace("{{HANDLE}}", options.InstagramHandle);

        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Salva também o HTML renderizado ao lado do PNG para fácil inspeção/debug
        string htmlPreviewPath = Path.ChangeExtension(outputPath, ".html");
        await File.WriteAllTextAsync(htmlPreviewPath, htmlFinal);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var page = await browser.NewPageAsync();
        await page.SetViewportSizeAsync(1080, 1920);
        await page.SetContentAsync(htmlFinal);

        // Aguarda carregamento das fontes do Google Fonts (NetworkIdle)
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = outputPath,
            Type = ScreenshotType.Png
        });

        Console.WriteLine($"[Playwright] Story renderizado em 1080x1920px: {outputPath}");
        return outputPath;
    }

    private static string FormatarTopicoHtml(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        // Se o modelo já devolveu com <strong>, mantém; caso contrário, destaca o texto antes dos dois-pontos ':'
        if (texto.Contains("<strong>", StringComparison.OrdinalIgnoreCase))
            return texto;

        int colonIndex = texto.IndexOf(':');
        if (colonIndex > 0 && colonIndex < 45)
        {
            string prefixo = texto[..(colonIndex + 1)];
            string restante = texto[(colonIndex + 1)..];
            return $"<strong>{prefixo}</strong>{restante}";
        }

        return texto;
    }

    private static string ResolverCaminhoTemplate()
    {
        string[] candidatos =
        [
            Path.Combine(AppContext.BaseDirectory, "Templates", "story-template.html"),
            Path.Combine(Directory.GetCurrentDirectory(), "Templates", "story-template.html"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "InstagramStoryBot", "Templates", "story-template.html")
        ];

        foreach (string path in candidatos)
        {
            if (File.Exists(path))
                return path;
        }

        throw new FileNotFoundException("Não foi possível localizar Templates/story-template.html.");
    }
}
