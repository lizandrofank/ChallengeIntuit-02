using ConsultasPlan.Data;
using ConsultasPlan.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsultasPlan.Services;

public interface IAfiliadosService
{
    Task<List<Afiliado>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Afiliado?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<Afiliado>> CreateAsync(Afiliado afiliado, CancellationToken cancellationToken = default);
    Task<ServiceResult<Afiliado>> UpdateAsync(int id, Afiliado afiliado, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class AfiliadosService : IAfiliadosService
{
    private readonly AppDbContext _context;

    public AfiliadosService(AppDbContext context) => _context = context;

    public Task<List<Afiliado>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.Afiliados.AsNoTracking().ToListAsync(cancellationToken);

    public Task<Afiliado?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Afiliados.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<ServiceResult<Afiliado>> CreateAsync(Afiliado afiliado, CancellationToken cancellationToken = default)
    {
        var validation = Validate(afiliado);
        if (validation != null)
            return ServiceResult<Afiliado>.Invalid(validation);

        afiliado.Id = 0;
        afiliado.createdAt = DateTime.UtcNow;
        afiliado.isActive = true;
        _context.Afiliados.Add(afiliado);
        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult<Afiliado>.Ok(afiliado);
    }

    public async Task<ServiceResult<Afiliado>> UpdateAsync(int id, Afiliado afiliado, CancellationToken cancellationToken = default)
    {
        var validation = Validate(afiliado);
        if (validation != null)
            return ServiceResult<Afiliado>.Invalid(validation);

        var existing = await _context.Afiliados.FindAsync(new object[] { id }, cancellationToken);
        if (existing == null)
            return ServiceResult<Afiliado>.NotFound("Afiliado no encontrado.");

        existing.NombreCompleto = afiliado.NombreCompleto;
        existing.DNI = afiliado.DNI;
        existing.Email = afiliado.Email;
        existing.Telefono = afiliado.Telefono;
        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult<Afiliado>.Ok(existing);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var afiliado = await _context.Afiliados.FindAsync(new object[] { id }, cancellationToken);
        if (afiliado == null)
            return ServiceResult<bool>.NotFound("Afiliado no encontrado.");

        _context.Afiliados.Remove(afiliado);
        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }

    private static string? Validate(Afiliado afiliado)
    {
        if (string.IsNullOrWhiteSpace(afiliado.NombreCompleto)) return "El nombre del afiliado es obligatorio.";
        if (string.IsNullOrWhiteSpace(afiliado.DNI)) return "El DNI del afiliado es obligatorio.";
        return null;
    }
}
