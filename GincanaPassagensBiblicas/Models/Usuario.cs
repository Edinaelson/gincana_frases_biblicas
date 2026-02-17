using System.Collections.Generic;

namespace GincanaPassagensBiblicas.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        // Path para imagem de perfil
        public string ImagePath { get; set; } = string.Empty;

        // Navigation properties
        public List<Frase> Frases { get; set; } = new();
        public List<Pontuacao> Pontuacoes { get; set; } = new();
    }
}
