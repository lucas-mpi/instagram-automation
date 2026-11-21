using System.Text.Json.Serialization;

namespace InstagramStoryBot.Models;

public sealed class TemaSeed
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("servico")]
    public string Servico { get; set; } = string.Empty;

    [JsonPropertyName("formato")]
    public string Formato { get; set; } = string.Empty;

    [JsonPropertyName("tema")]
    public string Tema { get; set; } = string.Empty;

    [JsonPropertyName("ultimo_uso")]
    public DateTimeOffset? UltimoUso { get; set; }

    public string ObterNomeServicoLegivel() => Servico.ToLowerInvariant() switch
    {
        "cilios" => "Extensão de Cílios",
        "maquiagem" => "Maquiagem Social e Eventos",
        "escova" => "Escova e Alinhamento Capilar",
        "babyliss" => "Babyliss e Ondas",
        _ => Servico
    };

    public string ObterNomeFormatoLegivel() => Formato.ToLowerInvariant() switch
    {
        "cuidados_pos" => "Cuidados Pós-Procedimento e Durabilidade",
        "mito_ou_verdade" => "Mito ou Verdade",
        "guia_de_escolha" => "Guia de Escolha",
        "checklist_preparacao" => "Checklist de Preparação",
        _ => Formato
    };
}
