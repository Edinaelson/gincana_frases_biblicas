using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Text;

namespace GincanaPassagensBiblicas.Services
{
    public class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;
    }

    public class OllamaService : IGeminiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<OllamaService> _logger;

        // Voltando para o 3.1 8b que é mais estável para o formato JSON no seu hardware
        private const string ModelName = "llama3.1:8b";

        public OllamaService(HttpClient http, ILogger<OllamaService> logger)
        {
            _http = http;
            _logger = logger;
            _http.Timeout = TimeSpan.FromMinutes(5); 
        }

        public bool IsConfigured => true;

        public async Task<bool> AnalyzeAsync(string text)
        {
            var full = await AnalyzeFullAsync(text);
            return full.HasValue && !string.IsNullOrEmpty(full.Value.Passagem);
        }

        public async Task<(string? Passagem, string? Contexto)?> AnalyzeFullAsync(string text)
        {
            try
            {
                var safeText = System.Web.HttpUtility.JavaScriptStringEncode(text ?? string.Empty);

                var prompt = $@"Você é um conferente de gincana bíblica rigoroso.
Sua única missão é encontrar o versículo EXATO na versão ALMEIDA REVISTA E CORRIGIDA (ARC).

Regras Absolutas:
1. Retorne o texto INTEGRAL e LITERAL da versão ARC.
2. Exemplos de como o texto DEVE ser (ARC):
   - João 3:16: ""Porque Deus amou o mundo de tal maneira que deu o seu Filho unigênito, para que todo aquele que nele crê não pereça, mas tenha a vida eterna.""
   - Romanos 7:19: ""Porque não faço o bem que quero, mas o mal que não quero, esse faço.""

JSON de resposta:
{{
  ""encontrou"": true,
  ""passagem"": ""Livro Cap:Ver - [Texto integral da ARC aqui]"",
  ""contexto"": ""Resumo teológico breve.""
}}

Frase do usuário: ""{safeText}""";

                var payload = new
                {
                    model = ModelName,
                    prompt = prompt,
                    stream = false,
                    format = "json",
                    options = new {
                        temperature = 0.0,
                        num_predict = 400,
                        num_thread = 8, // Otimiza para seu processador
                        num_gpu = 20,   // Tenta carregar o máximo de camadas na sua RX 550
                        low_vram = true // Essencial para placas de 4GB
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation("Aguardando Llama processar Bíblia... (Texto: {Len} chars)", text?.Length ?? 0);
                
                var response = await _http.PostAsync("http://localhost:11434/api/generate", content);
                
                _logger.LogInformation("Ollama respondeu com status: {Status}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Erro no Ollama: {Error}", errorBody);
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Resposta bruta do Ollama: {Body}", responseBody);

                var ollamaResult = JsonSerializer.Deserialize<OllamaResponse>(responseBody);
                
                if (ollamaResult == null || string.IsNullOrEmpty(ollamaResult.Response))
                {
                    _logger.LogWarning("Ollama retornou uma resposta vazia.");
                    return null;
                }

                _logger.LogInformation("Texto da resposta do modelo: {Text}", ollamaResult.Response);

                // Tentar extrair o JSON se o modelo retornar texto extra
                var jsonStr = ExtractJson(ollamaResult.Response);
                if (string.IsNullOrEmpty(jsonStr))
                {
                    _logger.LogWarning("Não foi possível encontrar um JSON válido na resposta do modelo.");
                    return null;
                }

                using var parsed = JsonDocument.Parse(jsonStr);
                var pRoot = parsed.RootElement;

                if (pRoot.TryGetProperty("encontrou", out var encontrou))
                {
                    var encontrouVal = encontrou.GetBoolean();
                    var passagem = pRoot.TryGetProperty("passagem", out var passEl) ? passEl.GetString() : null;
                    var contexto = pRoot.TryGetProperty("contexto", out var ctxEl) ? ctxEl.GetString() : null;
                    
                    _logger.LogInformation("Resultado da análise: Encontrou={E}, Passagem={P}", encontrouVal, passagem);
                    
                    if (!encontrouVal)
                        return (null, null);

                    return (passagem, contexto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha na análise do Ollama");
            }

            return null;
        }
        private static string? ExtractJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var trimmed = text.Trim();
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end > start)
                return trimmed.Substring(start, end - start + 1);

            return null;
        }
    }
}
