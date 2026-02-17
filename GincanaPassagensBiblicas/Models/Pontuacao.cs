namespace GincanaPassagensBiblicas.Models
{
    public class Pontuacao
    {
        public int Id { get; set; }
        public int Valor { get; set; }

        // Foreign key
        public int UsuarioId { get; set; }

        // Navigation property
        public Usuario? Usuario { get; set; }
        // Referência à frase que originou a pontuação (opcional)
        public int? FraseId { get; set; }
        public Frase? Frase { get; set; }
    }
}
