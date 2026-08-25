namespace HRomance.Models;

public class ArbeitszeitMonat
{
    public int Jahr { get; set; }

    public int Monat { get; set; }

    public double Ist { get; set; }

    public double Soll { get; set; }

    public double Saldo { get; set; }

    public double LaufenderSaldo { get; set; }

    public int Abwesenheitstage { get; set; }
}
