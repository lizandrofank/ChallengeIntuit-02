namespace ConsultasPlan.Models;

public class PoliticaCupoOptions
{
    public string Especialidad { get; set; } = "Nutrición";
    public int MaximoAnual { get; set; } = 6;
    public int MaximoMensual { get; set; } = 1;
    public ModalidadConsulta Modalidad { get; set; } = ModalidadConsulta.Presencial;
}
