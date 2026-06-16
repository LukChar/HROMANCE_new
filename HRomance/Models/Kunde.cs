using System.ComponentModel.DataAnnotations;

namespace HRomance.Models
{
    public class Kunde
    {
        public int Id { get; set; }

        [Required]
        public string Firmenname { get; set; } = string.Empty;

        public string Ansprechpartner { get; set; } = string.Empty;

        public string Telefon { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Adresse { get; set; } = string.Empty;

        public string Stadt { get; set; } = string.Empty;

        public ICollection<Einsatz>? Einsaetze { get; set; }
    }
}