namespace GincanaPassagensBiblicas.Models
{
    public class Frase
    {
        public int Id { get; set; }
        public string Texto { get; set; } = string.Empty;

        // Foreign key
        public int UsuarioId { get; set; }

        // Navigation property
        public Usuario? Usuario { get; set; }
        // Navigation to the score created from this phrase (optional)
        public Pontuacao? Pontuacao { get; set; }
        // Resultado da análise pela IA: true = válida, false = inválida
        public bool IsValid { get; set; }
        // Fields returned by the AI analysis
        public string Passagem { get; set; } = string.Empty;
        public string Contexto { get; set; } = string.Empty;
        // Creation timestamp
        public DateTime CreatedAt { get; set; }
    }
}
