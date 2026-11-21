using System.Text;
using System.Text.Json;
using InstagramStoryBot.Configuration;
using InstagramStoryBot.Models;

namespace InstagramStoryBot.Services;

public sealed class GeminiService(HttpClient httpClient, BotOptions options)
{
    private const string SystemPrompt = """
        Você é um estrategista de conteúdo e copywriter especializado no mercado de beleza e estética (extensão de cílios, maquiagem, escova e babyliss).

        ### Diretrizes de Tom e Estilo
        - **Voz:** Profissional, acolhedora, segura e didática. Escreva como uma especialista conversando com sua cliente no estúdio.
        - **Anti-IA (Proibições estritas):**
          - NÃO use clichês de marketing digital (ex: "Você sabia?", "Arrasta para o lado", "Segredo revelado", "Transforme seu olhar").
          - NÃO use adjetivos exagerados ou vazios (ex: "incrível", "revolucionário", "mágico", "perfeito", "deslumbrante").
          - NÃO use hashtags genéricas em inglês (#beauty, #makeup, #lashes) nem blocos gigantes de tags.
          - NÃO use emojis no texto que vai dentro da imagem (card).

        ### Regras de Estrutura e Limite de Caracteres (RÍGIDO)
        O conteúdo será injetado automaticamente em um template gráfico fixo de Instagram Stories (1080x1920px). Respeite estritamente os limites abaixo para não quebrar o layout:
        1. `categoria_tag`: Rótulo curto para o topo do card (ex: "CUIDADOS COM CÍLIOS", "DICA DE RETENÇÃO", "GUIA DE MAKE", "DICA DE CABELO"). Máximo 25 caracteres.
        2. `titulo_card`: Manchete curta, direta e de alto impacto. Máximo 55 caracteres.
        3. `subtitulo_card`: Complemento ou gancho rápido abaixo do título. Máximo 75 caracteres.
        4. `topicos_card`: Array com exatamente 3 pontos objetivos que explicam o "porquê" ou o "como fazer". Cada item deve começar com um termo curto em destaque dentro de `<strong>...</strong>` seguido de dois pontos e a explicação direta (ex: "<strong>Evite dormir de bruços:</strong> o atrito direto no travesseiro entorta e arranca os fios."). Máximo 125 caracteres por item.
        5. `cta_box`: Chamada curta para o rodapé incentivando resposta no Direct (ex: "Qual lado você mais dorme? Me conta no Direct"). Máximo 55 caracteres.
        """;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<StoryContent> GerarConteudoStoryAsync(TemaSeed tema, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.GeminiApiKey))
        {
            Console.WriteLine("[Gemini] GEMINI_API_KEY não informada. Gerando conteúdo de demonstração local para o tema selecionado.");
            return GerarConteudoFallbackDeterministico(tema);
        }

        const int maxTentativas = 3;
        List<string> ultimosErros = [];

        for (int tentativa = 1; tentativa <= maxTentativas; tentativa++)
        {
            string userPrompt = MontarUserPrompt(tema, ultimosErros);
            var story = await ChamarGeminiStructuredOutputAsync(userPrompt, ct);

            if (story.ValidarLimites(out ultimosErros))
            {
                Console.WriteLine($"[Gemini] Conteúdo JSON gerado e validado com sucesso na tentativa {tentativa}.");
                return story;
            }

            Console.WriteLine($"[Gemini] Tentativa {tentativa} excedeu limites de caracteres ({string.Join("; ", ultimosErros)}). Solicitando ajuste...");
        }

        throw new InvalidOperationException($"O modelo Gemini não respeitou os limites de caracteres após {maxTentativas} tentativas: {string.Join(", ", ultimosErros)}");
    }

    private static string MontarUserPrompt(TemaSeed tema, List<string> errosAnteriores)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Gere o conteúdo para o Story do Instagram com base nos seguintes dados:");
        sb.AppendLine($"- Serviço: {tema.ObterNomeServicoLegivel()}");
        sb.AppendLine($"- Formato: {tema.ObterNomeFormatoLegivel()}");
        sb.AppendLine($"- Pauta: {tema.Tema}");

        if (errosAnteriores.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ATENÇÃO: Na tentativa anterior, os seguintes campos ficaram longos demais. Encurte-os estritamente:");
            foreach (string erro in errosAnteriores)
            {
                sb.AppendLine($"- {erro}");
            }
        }

        return sb.ToString();
    }

    private async Task<StoryContent> ChamarGeminiStructuredOutputAsync(string userPrompt, CancellationToken ct)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{options.GeminiModel}:generateContent?key={options.GeminiApiKey}";

        var requestBody = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = SystemPrompt } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = userPrompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.6,
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "OBJECT",
                    properties = new
                    {
                        categoria_tag = new { type = "STRING" },
                        titulo_card = new { type = "STRING" },
                        subtitulo_card = new { type = "STRING" },
                        topicos_card = new
                        {
                            type = "ARRAY",
                            items = new { type = "STRING" }
                        },
                        cta_box = new { type = "STRING" }
                    },
                    required = new[] { "categoria_tag", "titulo_card", "subtitulo_card", "topicos_card", "cta_box" }
                }
            }
        };

        string jsonPayload = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        using var response = await httpClient.PostAsync(url, content, ct);

        string responseText = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Erro na API do Gemini ({response.StatusCode}): {responseText}");
        }

        using var doc = JsonDocument.Parse(responseText);
        string? rawJson = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(rawJson))
            throw new InvalidOperationException("A resposta do Gemini veio vazia.");

        return JsonSerializer.Deserialize<StoryContent>(rawJson, JsonOpts)
               ?? throw new InvalidOperationException("Não foi possível desserializar o JSON retornado pelo Gemini.");
    }

    private static StoryContent GerarConteudoFallbackDeterministico(TemaSeed tema)
    {
        // Exemplo idêntico ao demonstrado nas páginas 4, 7 e 8 do PDF quando não há chave configurada
        if (tema.Id == 9 || tema.Servico.Equals("cilios", StringComparison.OrdinalIgnoreCase))
        {
            return new StoryContent
            {
                CategoriaTag = "DICA DE RETENÇÃO",
                TituloCard = "Acorda com um olho mais cheio que o outro?",
                SubtituloCard = "O jeito que você dorme pode estar amassando e soltando seus cílios.",
                TopicosCard =
                [
                    "<strong>Evite dormir de bruços:</strong> o atrito direto no travesseiro entorta e arranca os fios antes da hora.",
                    "<strong>Aposte na fronha de cetim:</strong> o tecido desliza sem puxar a extensão e ainda reduz o frizz do cabelo.",
                    "<strong>Escove logo ao acordar:</strong> com os fios secos, passe a escovinha suavemente do meio para as pontas."
                ],
                CtaBox = "Qual lado você mais dorme? Me conta no Direct"
            };
        }

        return new StoryContent
        {
            CategoriaTag = tema.ObterNomeServicoLegivel().ToUpperInvariant(),
            TituloCard = tema.Tema.Split(':')[0],
            SubtituloCard = "Cuidados essenciais para manter o resultado impecável por mais tempo.",
            TopicosCard =
            [
                "<strong>Preparação correta:</strong> seguir as orientações antes do horário garante melhor fixação e acabamento.",
                "<strong>Produtos adequados:</strong> evite fórmulas pesadas ou oleosas que comprometem a durabilidade.",
                "<strong>Manutenção em dia:</strong> respeitar o intervalo ideal preserva a saúde e a beleza do procedimento."
            ],
            CtaBox = "Ficou com dúvida? Me chama aqui no Direct"
        };
    }
}
