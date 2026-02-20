using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;

namespace GincanaPassagensBiblicas.Services
{
    public class BibleVerse
    {
        public string Reference { get; set; } = "";
        public string Text { get; set; } = "";
        public string CleanText { get; set; } = ""; 
    }

    public class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;
    }

    public class OllamaService : IGeminiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<OllamaService> _logger;
        private readonly string _bibleFilePath;
        private static List<BibleVerse>? _bibleCache;
        
        private const string PrimaryNode = "http://192.168.18.251:11434/api/generate";
        private const string SecondaryNode = "http://localhost:11434/api/generate";
        
        // Versão quantizada (leve) para o i5 e versão normal para o i7 com GPU
        private const string RemoteModel = "llama3.1:8b"; 
        private const string LocalModel = "llama3.1:8b"; 

        public OllamaService(HttpClient http, ILogger<OllamaService> logger)
        {
            _http = http;
            _logger = logger;
            _http.Timeout = TimeSpan.FromMinutes(5);
            _bibleFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Portugues-All-Bible-Corrigida-Fiel.txt");
            if (!File.Exists(_bibleFilePath))
                _bibleFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Portugues-All-Bible-Corrigida-Fiel.txt");
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
                if (string.IsNullOrWhiteSpace(text)) return null;
                if (_bibleCache == null) await LoadBibleToCache();

                var bestMatch = FindExactMatch(text);
                if (bestMatch != null) return (bestMatch.Reference + " - " + bestMatch.Text, "");

                var candidates = FindBestMatches(text, 3);
                if (!candidates.Any()) return null;

                var prompt = $@"Escolha a referência exata entre:
{string.Join("\n", candidates.Select(c => $"- {c.Reference}: {c.Text}"))}
Frase do usuário: ""{text}""
Responda APENAS JSON: {{ ""encontrou"": true, ""referencia"": ""Livro Capítulo:Versículo"" }}";

                HttpResponseMessage? response = null;

                // TENTA i5 (Modelo Leve)
                try {
                    _logger.LogInformation("Solicitando ao i5 (Llama 3.1 8B Light)...");
                    var payloadRemote = new { model = RemoteModel, prompt = prompt, stream = false, format = "json" };
                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(20)); 
                    response = await _http.PostAsync(PrimaryNode, new StringContent(JsonSerializer.Serialize(payloadRemote), Encoding.UTF8, "application/json"), cts.Token);
                } catch { }

                // TENTA i7 LOCAL (Modelo Full com GPU)
                if (response == null || !response.IsSuccessStatusCode) {
                    _logger.LogInformation("Usando i7 Local (Llama 3.1 8B Full + GPU)...");
                    var payloadLocal = new { 
                        model = LocalModel, prompt = prompt, stream = false, format = "json", 
                        options = new { temperature = 0.0, num_predict = 100, num_thread = 8, num_gpu = 35, low_vram = true } 
                    };
                    response = await _http.PostAsync(SecondaryNode, new StringContent(JsonSerializer.Serialize(payloadLocal), Encoding.UTF8, "application/json"));
                }
                
                if (!response.IsSuccessStatusCode) return (candidates.First().Reference + " - " + candidates.First().Text, "");

                var responseBody = await response.Content.ReadAsStringAsync();
                var ollamaResult = JsonSerializer.Deserialize<OllamaResponse>(responseBody);
                var jsonStr = ExtractJson(ollamaResult?.Response ?? "");
                if (string.IsNullOrEmpty(jsonStr)) return (candidates.First().Reference + " - " + candidates.First().Text, "");

                using var parsed = JsonDocument.Parse(jsonStr);
                var pRoot = parsed.RootElement;
                if (pRoot.TryGetProperty("encontrou", out var enc) && enc.GetBoolean())
                {
                    var refIA = pRoot.TryGetProperty("referencia", out var refEl) ? refEl.GetString() : "";
                    var matchFinal = candidates.FirstOrDefault(c => c.Reference.Equals(refIA, StringComparison.OrdinalIgnoreCase)) ?? candidates.First();
                    return (matchFinal.Reference + " - " + matchFinal.Text, "");
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Erro no RAG"); }
            return null;
        }

        private async Task LoadBibleToCache()
        {
            _logger.LogInformation("Carregando Bíblia...");
            _bibleCache = new List<BibleVerse>();
            if (!File.Exists(_bibleFilePath)) return;
            var lines = await File.ReadAllLinesAsync(_bibleFilePath);
            string currentBook = ""; string currentChapter = ""; BibleVerse? currentVerse = null;
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.Contains("Bíblia Almeida Corrigida Fiel") || trimmed.Contains("Sociedade Bíblica")) continue;
                var matchCap = Regex.Match(trimmed, @"^(.+)\s(\d+)$");
                if (matchCap.Success) {
                    var bookName = matchCap.Groups[1].Value.Trim();
                    if (new[] {"Gênesis", "Êxodo", "Levítico", "Números", "Deuteronômio", "Josué", "Juízes", "Rute", "1 Samuel", "2 Samuel", "1 Reis", "2 Reis", "1 Crônicas", "2 Crônicas", "Esdras", "Neemias", "Ester", "Jó", "Salmos", "Provérbios", "Eclesiastes", "Cânticos", "Isaías", "Jeremias", "Lamentações", "Ezequiel", "Daniel", "Oséias", "Joel", "Amós", "Obadias", "Jonas", "Miquéias", "Naum", "Habacuque", "Sofonias", "Ageu", "Zacarias", "Malaquias", "Mateus", "Marcos", "Lucas", "João", "Atos", "Romanos", "1 Coríntios", "2 Coríntios", "Gálatas", "Efésios", "Filipenses", "Colossenses", "1 Tessalonicenses", "2 Tessalonicenses", "1 Timóteo", "2 Timóteo", "Tito", "Filemom", "Hebreus", "Tiago", "1 Pedro", "2 Pedro", "1 João", "2 João", "3 João", "Judas", "Apocalipse"}.Any(l => l.Equals(bookName, StringComparison.OrdinalIgnoreCase))) {
                        currentBook = bookName; currentChapter = matchCap.Groups[2].Value; continue;
                    }
                }
                var firstSpace = trimmed.IndexOf(' ');
                if (firstSpace > 0 && int.TryParse(trimmed.Substring(0, firstSpace), out var vNum)) {
                    if (currentVerse != null) _bibleCache.Add(currentVerse);
                    var vText = trimmed.Substring(firstSpace).Trim();
                    currentVerse = new BibleVerse { Reference = $"{currentBook} {currentChapter}:{vNum}", Text = vText, CleanText = CleanString(vText) };
                }
                else if (currentVerse != null && !int.TryParse(trimmed, out _)) {
                    currentVerse.Text += " " + trimmed; currentVerse.CleanText = CleanString(currentVerse.Text);
                }
            }
            if (currentVerse != null) _bibleCache.Add(currentVerse);
        }

        private BibleVerse? FindExactMatch(string query) {
            var cleanQuery = CleanString(query);
            return _bibleCache?.FirstOrDefault(v => v.CleanText.Contains(cleanQuery) || cleanQuery.Contains(v.CleanText));
        }

        private List<BibleVerse> FindBestMatches(string query, int count) {
            var queryWords = CleanString(query).Split(' ').Where(w => w.Length > 3).ToList();
            return (_bibleCache ?? new List<BibleVerse>())
                .Select(v => new { Verse = v, Score = queryWords.Count(w => v.CleanText.Contains(w)) })
                .Where(x => x.Score > 0).OrderByDescending(x => x.Score).Take(count).Select(x => x.Verse).ToList();
        }

        private string CleanString(string s) {
            var normalized = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)))
                    sb.Append(char.ToLower(c));
            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        private static string? ExtractJson(string text) {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start) return text.Substring(start, end - start + 1);
            return null;
        }
    }
}
