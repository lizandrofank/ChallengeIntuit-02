using ConsultasPlan.Data;
using ConsultasPlan.Helpers;
using ConsultasPlan.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsultasPlan.Services;

public interface IConsultasService
{
    Task<List<Consulta>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Consulta?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<Consulta>> CreateAsync(CrearConsultaRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<Consulta>> CancelAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<Consulta>> MarkAbsenceAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<Consulta>> RegisterAttendanceAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<Consulta>> UpdateStateAsync(int id, EstadoConsulta estado, CancellationToken cancellationToken = default);
}

public class ConsultasService : IConsultasService
{
    private readonly AppDbContext _context;
    private readonly IPoliticaCupoService _politicaCupoService;

    public ConsultasService(AppDbContext context, IPoliticaCupoService politicaCupoService)
    {
        _context = context;
        _politicaCupoService = politicaCupoService;
    }

    public Task<List<Consulta>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.Consultas.Include(c => c.Afiliado).Include(c => c.Profesional).AsNoTracking().ToListAsync(cancellationToken);

    public Task<Consulta?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Consultas.Include(c => c.Afiliado).Include(c => c.Profesional).AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<ServiceResult<Consulta>> CreateAsync(CrearConsultaRequest request, CancellationToken cancellationToken = default)
    {
        var fechaHoraUtc = request.FechaHora.ToUtc();
        if (fechaHoraUtc <= DateTime.UtcNow)
            return ServiceResult<Consulta>.Invalid("No se puede agendar una consulta en una fecha pasada.");
        if (!request.AfiliadoId.HasValue)
            return ServiceResult<Consulta>.Invalid("El afiliado es obligatorio.");

        var afiliado = await _context.Afiliados.FindAsync(new object[] { request.AfiliadoId.Value }, cancellationToken);
        if (afiliado == null) return ServiceResult<Consulta>.NotFound("Afiliado no encontrado.");
        if (afiliado.Bloqueado) return ServiceResult<Consulta>.Invalid("El afiliado se encuentra bloqueado para agendar consultas.");
        if (!await _context.Profesionales.AnyAsync(p => p.Id == request.ProfesionalId, cancellationToken))
            return ServiceResult<Consulta>.NotFound("Profesional no encontrado.");

        var cupo = await _politicaCupoService.ValidarAsync(request.AfiliadoId.Value, request.ProfesionalId, fechaHoraUtc, request.Modalidad, cancellationToken);
        if (!cupo.Permitida) return ServiceResult<Consulta>.Invalid(cupo.Mensaje!);

        if (await _context.Consultas.AnyAsync(c => c.ProfesionalId == request.ProfesionalId && c.FechaHora == fechaHoraUtc && c.Estado != EstadoConsulta.Cancelado, cancellationToken))
            return ServiceResult<Consulta>.Conflict("El profesional ya tiene una consulta en ese horario.");

        var consulta = new Consulta
        {
            AfiliadoId = request.AfiliadoId,
            ProfesionalId = request.ProfesionalId,
            FechaHora = fechaHoraUtc,
            Modalidad = request.Modalidad,
            Motivo = request.Motivo,
            FechaCreacion = DateTime.UtcNow,
            Estado = EstadoConsulta.Pendiente
        };
        _context.Consultas.Add(consulta);
        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult<Consulta>.Ok(consulta);
    }

    public async Task<ServiceResult<Consulta>> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var consulta = await _context.Consultas.FindAsync(new object[] { id }, cancellationToken);
        if (consulta == null) return ServiceResult<Consulta>.NotFound("Consulta no encontrada.");
        if (consulta.FechaHora.ToUtc() - DateTime.UtcNow < TimeSpan.FromHours(24))
            return ServiceResult<Consulta>.Invalid("No se puede cancelar con menos de 24 horas de anticipación.");
        consulta.Estado = EstadoConsulta.Cancelado;
        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult<Consulta>.Ok(consulta);
    }

    public async Task<ServiceResult<Consulta>> MarkAbsenceAsync(int id, CancellationToken cancellationToken = default)
    {
        var consulta = await _context.Consultas.FindAsync(new object[] { id }, cancellationToken);
        if (consulta == null) return ServiceResult<Consulta>.NotFound("Consulta no encontrada.");
        if (consulta.FechaHora.ToUtc() > DateTime.UtcNow)
            return ServiceResult<Consulta>.Invalid("No se puede registrar una inasistencia sobre una consulta futura.");
        consulta.Estado = EstadoConsulta.Inasistencia;
        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult<Consulta>.Ok(consulta);
    }

    public async Task<ServiceResult<Consulta>> RegisterAttendanceAsync(int id, CancellationToken cancellationToken = default)
    {
        var consulta = await _context.Consultas.FindAsync(new object[] { id }, cancellationToken);
        if (consulta == null) return ServiceResult<Consulta>.NotFound("Consulta no encontrada.");
        if (!consulta.FechaHora.IsInCurrentYear())
            return ServiceResult<Consulta>.Invalid("Solo se pueden registrar atenciones de consultas del año en curso.");
        consulta.Estado = EstadoConsulta.Atendido;
        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult<Consulta>.Ok(consulta);
    }

    public async Task<ServiceResult<Consulta>> UpdateStateAsync(int id, EstadoConsulta estado, CancellationToken cancellationToken = default)
    {
        var consulta = await _context.Consultas.FindAsync(new object[] { id }, cancellationToken);
        if (consulta == null) return ServiceResult<Consulta>.NotFound("Consulta no encontrada.");
        consulta.Estado = estado;
        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult<Consulta>.Ok(consulta);
    }
}
