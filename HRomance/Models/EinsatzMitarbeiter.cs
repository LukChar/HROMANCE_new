namespace HRomance.Models
{
    public class EinsatzMitarbeiter
    {
        public int MitarbeiterId { get; set; }

        public Mitarbeiter? Mitarbeiter { get; set; }

        public int EinsatzId { get; set; }

        public Einsatz? Einsatz { get; set; }
    }
}