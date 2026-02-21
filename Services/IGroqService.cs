using System.Threading.Tasks;

namespace GincanaPassagensBiblicas.Services
{
    public interface IGroqService
    {
        Task<(string? Passagem, string? Contexto)?> AnalyzeFullAsync(string text);
        bool IsConfigured { get; }
    }
}
