using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Text;

namespace GincanaPassagensBiblicas.Services
{
    public class GroqService : IGroqService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly ILogger<GroqService> _logger;
        private const string ModelName = "llama-3.3-70b-versatile"; 

        public GroqService(HttpClient http, ILogger<GroqService> logger)
        {
            _http = http;
            _logger = logger;
            _apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? string.Empty;
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

        public async Task<(string? Passagem, string? Contexto)?> AnalyzeFullAsync(string text)
        {
            if (!IsConfigured) return null;

            try
            {
                var systemPrompt = @"Você é um especialista bíblico com conhecimento preciso da Bíblia Sagrada na versão ARC (Almeida Revista e Corrigida).

TAREFA: Identificar se a frase do usuário é uma referência bíblica e retornar o versículo EXATO da versão ARC.

REGRAS CRÍTICAS:
1. NUNCA parafraseie ou reescreva o versículo - use o texto EXATO da versão ARC
2. Se não tiver certeza do texto exato, retorne 'encontrou': false
3. Retorne APENAS JSON válido, sem markdown, sem explicações
4. O texto da passagem deve ser IDÊNTICO ao da Bíblia ARC
5. Prefira 'encontrou': false a inventar ou aproximar um versículo

Formato de resposta:
{
    'encontrou': true ou false,
    'passagem': 'Livro Capitulo:Versiculo - Texto EXATO do versiculo na versao ARC',
    'confianca': 'alta' ou 'media' ou 'baixa'
}

Exemplos corretos:
Usuario: 'nao so de pao vivera o homem'
Resposta: {'encontrou': true, 'passagem': 'Mateus 4:4 - Mas ele respondeu e disse: Está escrito: Nem só de pão viverá o homem, mas de toda a palavra que sai da boca de Deus.', 'confianca': 'alta'}

Usuario: 'e sobrepujei em sabedoria a todos'
Resposta: {'encontrou': true, 'passagem': 'Eclesiastes 1:16 - Disse eu em meu coração: Eis que me engrandeci e tomei mais sabedoria do que todos os que têm sido antes de mim em Jerusalém; e o meu coração tem visto muita sabedoria e conhecimento.', 'confianca': 'alta'}

Usuario: 'hoje faz muito calor'
Resposta: {'encontrou': false, 'passagem': '', 'confianca': 'alta'}";

                var payload = new
                {
                    model = ModelName,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = text }
                    },
                    temperature = 0.0,
                    response_format = new { type = "json_object" }
                };

                var req = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions");
                req.Headers.Add("Authorization", $"Bearer {_apiKey}");
                req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                _logger.LogInformation("Solicitando ao Groq (Scholarly Prompt)...");
                var response = await _http.SendAsync(req);
                
                if (!response.IsSuccessStatusCode) return null;

                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);
                var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                if (string.IsNullOrEmpty(content)) return null;

                var jsonStr = ExtractJson(content);
                if (string.IsNullOrEmpty(jsonStr)) return null;

                using var parsed = JsonDocument.Parse(jsonStr);
                var pRoot = parsed.RootElement;

                if (pRoot.TryGetProperty("encontrou", out var enc) && enc.GetBoolean())
                {
                    var passagem = pRoot.TryGetProperty("passagem", out var passEl) ? passEl.GetString() : null;
                    // Mapeando 'confianca' para o campo de contexto temporariamente para não quebrar a UI
                    var confianca = pRoot.TryGetProperty("confianca", out var confEl) ? "Confiança: " + confEl.GetString() : "";
                    return (passagem, confianca);
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Erro no GroqService"); }
            return null;
        }

        private static string? ExtractJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start) return text.Substring(start, end - start + 1);
            return null;
        }
    }
}
