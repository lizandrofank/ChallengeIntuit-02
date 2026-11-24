using ConsultasPlan.Data;
using ConsultasPlan.Helpers;
using ConsultasPlan.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ConsultasPlan.Services;

public interface IPoliticaCupoService
{
    Task<ResultadoPoliticaCupo> ValidarAsync(
        int afiliadoId,
        int profesionalId,
        DateTime fechaHora,
        ModalidadConsulta modalidad,
        CancellationToken cancellationToken = default);
}

public sealed record ResultadoPoliticaCupo(bool Permitida, string? Mensaje = null);

public class PoliticaCupoService : IPoliticaCupoService
{
    private static readonly EstadoConsulta[] EstadosQueReservanCupo =
    {
        EstadoConsulta.Pendiente,
        EstadoConsulta.Confirmado,
        EstadoConsulta.Atendido
    };

    private readonly AppDbContext _context;
    private readonly PoliticaCupoOptions _options;

    public PoliticaCupoService(
        AppDbContext context,
        IOptions<PoliticaCupoOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public async Task<ResultadoPoliticaCupo> ValidarAsync(
        int afiliadoId,
        int profesionalId,
        DateTime fechaHora,
        ModalidadConsulta modalidad,
        CancellationToken cancellationToken = default)
    {
        if (modalidad != _options.Modalidad)
            return new ResultadoPoliticaCupo(true);

        var esEspecialidadAplicable = await _context.Profesionales
            .AsNoTracking()
            .AnyAsync(
                p => p.Id == profesionalId && p.Especialidad == _options.Especialidad,
                cancellationToken);

        if (!esEspecialidadAplicable)
            return new ResultadoPoliticaCupo(true);

        fechaHora = fechaHora.ToUtc();
        var inicioAnio = new DateTime(fechaHora.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var finAnio = inicioAnio.AddYears(1);
        var inicioMes = new DateTime(fechaHora.Year, fechaHora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var finMes = inicioMes.AddMonths(1);

        var consultasAplicables = _context.Consultas
            .AsNoTracking()
            .Where(c =>
                c.AfiliadoId == afiliadoId &&
                c.Profesional != null &&
                c.Profesional.Especialidad == _options.Especialidad &&
                c.Modalidad == _options.Modalidad &&
                EstadosQueReservanCupo.Contains(c.Estado));

        var consultasAnuales = await consultasAplicables
            .CountAsync(c => c.FechaHora >= inicioAnio && c.FechaHora < finAnio, cancellationToken);

        if (consultasAnuales >= _options.MaximoAnual)
        {
            return new ResultadoPoliticaCupo(
                false,
                $"El afiliado alcanzó el límite de {_options.MaximoAnual} consultas de {_options.Especialidad} para el año indicado.");
        }

        var consultasMensuales = await consultasAplicables
            .CountAsync(c => c.FechaHora >= inicioMes && c.FechaHora < finMes, cancellationToken);

        if (consultasMensuales >= _options.MaximoMensual)
        {
            return new ResultadoPoliticaCupo(
                false,
                $"El afiliado ya alcanzó el límite de {_options.MaximoMensual} consulta de {_options.Especialidad} para el mes indicado.");
        }

        return new ResultadoPoliticaCupo(true);
    }
}
