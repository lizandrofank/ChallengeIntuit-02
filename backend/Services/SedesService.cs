using ConsultasPlan.Data;
using ConsultasPlan.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsultasPlan.Services;

public interface ISedesService
{
    Task<List<Sede>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Sede?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<Sede>> CreateAsync(Sede sede, CancellationToken cancellationToken = default);
    Task<ServiceResult<Sede>> UpdateAsync(int id, Sede sede, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class SedesService : ISedesService
{
    private readonly AppDbContext _context;

    public SedesService(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Sede>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.Sedes.AsNoTracking().ToListAsync(cancellationToken);

    public Task<Sede?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Sedes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<ServiceResult<Sede>> CreateAsync(Sede sede, CancellationToken cancellationToken = default)
    {
        var validation = Validate(sede);
        if (validation != null)
            return ServiceResult<Sede>.Invalid(validation);

        sede.Id = 0;
        _context.Sedes.Add(sede);
        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult<Sede>.Ok(sede);
    }

    public async Task<ServiceResult<Sede>> UpdateAsync(int id, Sede sede, CancellationToken cancellationToken = default)
    {
        var validation = Validate(sede);
        if (validation != null)
            return ServiceResult<Sede>.Invalid(validation);

        var existing = await _context.Sedes.FindAsync(new object[] { id }, cancellationToken);
        if (existing == null)
            return ServiceResult<Sede>.NotFound("Sede no encontrada.");

        existing.Nombre = sede.Nombre;
        existing.Direccion = sede.Direccion;
        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult<Sede>.Ok(existing);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Sedes.FindAsync(new object[] { id }, cancellationToken);
        if (existing == null)
            return ServiceResult<bool>.NotFound("Sede no encontrada.");

        if (await _context.Profesionales.AnyAsync(p => p.SedeId == id, cancellationToken))
            return ServiceResult<bool>.Conflict("No se puede eliminar una sede que tiene profesionales asociados.");

        _context.Sedes.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }

    private static string? Validate(Sede sede)
    {
        if (string.IsNullOrWhiteSpace(sede.Nombre))
            return "El nombre de la sede es obligatorio.";
        if (string.IsNullOrWhiteSpace(sede.Direccion))
            return "La dirección de la sede es obligatoria.";
        return null;
    }
}
