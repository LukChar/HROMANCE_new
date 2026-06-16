using System.ComponentModel.DataAnnotations;

namespace HRomance.Models
{
    public class Mitarbeiter
    {
        public int Id { get; set; }

        [Required]
        public string Personalnummer { get; set; } = string.Empty;

        [Required]
        public string Vorname { get; set; } = string.Empty;

        [Required]
        public string Nachname { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Telefon { get; set; } = string.Empty;

        public string Adresse { get; set; } = string.Empty;

        public string Stadt { get; set; } = string.Empty;

        public string Qualifikation { get; set; } = string.Empty;

        public bool Fuehrerschein { get; set; }

        public bool Verfuegbar { get; set; } = true;

        public ICollection<EinsatzMitarbeiter>? EinsatzMitarbeiter { get; set; }
    }
}