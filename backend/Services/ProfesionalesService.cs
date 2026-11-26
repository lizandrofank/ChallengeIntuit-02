using ConsultasPlan.Data;
using ConsultasPlan.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsultasPlan.Services;

public interface IProfesionalesService
{
    Task<List<Profesional>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Profesional?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<Profesional>> CreateAsync(Profesional profesional, CancellationToken cancellationToken = default);
    Task<ServiceResult<Profesional>> UpdateAsync(int id, Profesional profesional, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class ProfesionalesService : IProfesionalesService
{
    private readonly AppDbContext _context;

    public ProfesionalesService(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Profesional>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.Profesionales.Include(p => p.Sede).AsNoTracking().ToListAsync(cancellationToken);

    public Task<Profesional?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Profesionales.Include(p => p.Sede).AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<ServiceResult<Profesional>> CreateAsync(Profesional profesional, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(profesional, cancellationToken);
        if (validation != null)
            return ServiceResult<Profesional>.Invalid(validation);

        profesional.Id = 0;
        profesional.Sede = null;
        _context.Profesionales.Add(profesional);
        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult<Profesional>.Ok(profesional);
    }

    public async Task<ServiceResult<Profesional>> UpdateAsync(int id, Profesional profesional, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(profesional, cancellationToken);
        if (validation != null)
            return ServiceResult<Profesional>.Invalid(validation);

        var existing = await _context.Profesionales.FindAsync(new object[] { id }, cancellationToken);
        if (existing == null)
            return ServiceResult<Profesional>.NotFound("Profesional no encontrado.");

        existing.NombreCompleto = profesional.NombreCompleto;
        existing.Especialidad = profesional.Especialidad;
        existing.SedeId = profesional.SedeId;
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<Profesional>.Ok(await GetByIdAsync(id, cancellationToken) ?? existing);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Profesionales.FindAsync(new object[] { id }, cancellationToken);
        if (existing == null)
            return ServiceResult<bool>.NotFound("Profesional no encontrado.");

        if (await _context.Consultas.AnyAsync(c => c.ProfesionalId == id, cancellationToken))
            return ServiceResult<bool>.Conflict("No se puede eliminar un profesional que tiene consultas asociadas.");

        _context.Profesionales.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }

    private async Task<string?> ValidateAsync(Profesional profesional, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(profesional.NombreCompleto))
            return "El nombre del profesional es obligatorio.";
        if (string.IsNullOrWhiteSpace(profesional.Especialidad))
            return "La especialidad del profesional es obligatoria.";
        if (!await _context.Sedes.AnyAsync(s => s.Id == profesional.SedeId, cancellationToken))
            return "La sede indicada no existe.";
        return null;
    }
}
