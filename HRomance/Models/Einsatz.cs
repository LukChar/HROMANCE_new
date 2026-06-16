using System.ComponentModel.DataAnnotations;

namespace HRomance.Models
{
    public class Einsatz
    {
        public int Id { get; set; }

        [Required]
        public string Titel { get; set; } = string.Empty;

        public string Beschreibung { get; set; } = string.Empty;

        public string Einsatzort { get; set; } = string.Empty;

        public string BenoetigteQualifikation { get; set; } = string.Empty;

        public DateTime Startdatum { get; set; }

        public DateTime Enddatum { get; set; }

        public int KundeId { get; set; }

        public Kunde? Kunde { get; set; }

        public ICollection<EinsatzMitarbeiter>? EinsatzMitarbeiter { get; set; }
    }
}