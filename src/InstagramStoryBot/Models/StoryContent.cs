using System.Text.Json.Serialization;

namespace InstagramStoryBot.Models;

public sealed class StoryContent
{
    [JsonPropertyName("categoria_tag")]
    public string CategoriaTag { get; set; } = string.Empty;

    [JsonPropertyName("titulo_card")]
    public string TituloCard { get; set; } = string.Empty;

    [JsonPropertyName("subtitulo_card")]
    public string SubtituloCard { get; set; } = string.Empty;

    [JsonPropertyName("topicos_card")]
    public string[] TopicosCard { get; set; } = [];

    [JsonPropertyName("cta_box")]
    public string CtaBox { get; set; } = string.Empty;

    [JsonPropertyName("legenda_instagram")]
    public string? LegendaInstagram { get; set; }

    [JsonPropertyName("hashtags")]
    public string[]? Hashtags { get; set; }

    public bool ValidarLimites(out List<string> erros)
    {
        erros = [];

        if (string.IsNullOrWhiteSpace(CategoriaTag) || CategoriaTag.Length > 32)
            erros.Add($"categoria_tag inválida ou longa demais ({CategoriaTag?.Length ?? 0}/32 chars).");

        if (string.IsNullOrWhiteSpace(TituloCard) || TituloCard.Length > 65)
            erros.Add($"titulo_card inválido ou longo demais ({TituloCard?.Length ?? 0}/65 chars).");

        if (string.IsNullOrWhiteSpace(SubtituloCard) || SubtituloCard.Length > 90)
            erros.Add($"subtitulo_card inválido ou longo demais ({SubtituloCard?.Length ?? 0}/90 chars).");

        if (TopicosCard is null || TopicosCard.Length != 3)
        {
            erros.Add("topicos_card deve conter exatamente 3 itens.");
        }
        else
        {
            for (int i = 0; i < TopicosCard.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(TopicosCard[i]) || TopicosCard[i].Length > 140)
                    erros.Add($"topicos_card[{i}] inválido ou longo demais ({TopicosCard[i]?.Length ?? 0}/140 chars).");
            }
        }

        if (string.IsNullOrWhiteSpace(CtaBox) || CtaBox.Length > 65)
            erros.Add($"cta_box inválido ou longo demais ({CtaBox?.Length ?? 0}/65 chars).");

        return erros.Count == 0;
    }
}
