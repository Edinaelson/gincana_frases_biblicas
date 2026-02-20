using System.Threading.Tasks;

namespace GincanaPassagensBiblicas.Services
{
    public interface IGeminiService
    {
        // Analyze text and return true if phrase is valid / makes sense
        Task<bool> AnalyzeAsync(string text);
        // Return detailed analysis including passagem and contexto (or null if not found)
        Task<(string? Passagem, string? Contexto)?> AnalyzeFullAsync(string text);
        // Whether the service is configured (API key present)
        bool IsConfigured { get; }
    }
}
