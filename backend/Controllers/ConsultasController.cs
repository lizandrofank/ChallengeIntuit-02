using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Helpers;
using ConsultasPlan.Models;
using ConsultasPlan.Services;

namespace ConsultasPlan.Controllers;

[ApiController]
[Route("[controller]")]
public class ConsultasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPoliticaCupoService _politicaCupoService;

    public ConsultasController(
        AppDbContext context,
        IPoliticaCupoService politicaCupoService)
    {
        _context = context;
        _politicaCupoService = politicaCupoService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var consultas = await _context.Consultas
            .Include(c => c.Afiliado)
            .Include(c => c.Profesional)
            .ToListAsync();
        return Ok(consultas);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var consulta = await _context.Consultas
            .Include(c => c.Afiliado)
            .Include(c => c.Profesional)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (consulta == null) return NotFound();
        return Ok(consulta);
    }

    [HttpPost]
    public async Task<IActionResult> CrearConsulta([FromBody] CrearConsultaRequest request)
    {
        var fechaHoraUtc = request.FechaHora.ToUtc();
        if (fechaHoraUtc <= DateTime.UtcNow)
            return BadRequest(new { mensaje = "No se puede agendar una consulta en una fecha pasada o en el momento actual." });

        if (!request.AfiliadoId.HasValue)
            return BadRequest(new { mensaje = "El afiliado es obligatorio." });

        var afiliado = await _context.Afiliados.FindAsync(request.AfiliadoId.Value);
        if (afiliado == null)
            return NotFound(new { mensaje = "Afiliado no encontrado." });

        if (afiliado.Bloqueado)
            return BadRequest(new { mensaje = "El afiliado se encuentra bloqueado para agendar consultas." });

        var profesionalExiste = await _context.Profesionales.AnyAsync(p => p.Id == request.ProfesionalId);
        if (!profesionalExiste)
            return NotFound(new { mensaje = "Profesional no encontrado." });

        var resultadoCupo = await _politicaCupoService.ValidarAsync(
            request.AfiliadoId.Value,
            request.ProfesionalId,
            fechaHoraUtc,
            request.Modalidad);
        if (!resultadoCupo.Permitida)
            return BadRequest(new { mensaje = resultadoCupo.Mensaje });

        var consultaConflicto = await _context.Consultas.AnyAsync(c =>
            c.ProfesionalId == request.ProfesionalId &&
            c.FechaHora == fechaHoraUtc &&
            c.Estado != EstadoConsulta.Cancelado);
        if (consultaConflicto)
            return BadRequest(new { mensaje = "El profesional ya tiene una consulta en ese horario." });

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
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = consulta.Id }, consulta);
    }

    [HttpPost("cancelar/{id}")]
    public async Task<IActionResult> CancelarConsulta(int id)
    {
        var consulta = await _context.Consultas.FindAsync(id);
        if (consulta == null) return NotFound();

        if (consulta.FechaHora.ToUtc() - DateTime.UtcNow < TimeSpan.FromHours(24))
            return BadRequest(new { mensaje = "No se puede cancelar con menos de 24 horas de anticipación." });

        consulta.Estado = EstadoConsulta.Cancelado;
        await _context.SaveChangesAsync();
        return Ok(consulta);
    }

    [HttpPost("{id}/inasistencia")]
    public async Task<IActionResult> MarcarInasistencia(int id)
    {
        var consulta = await _context.Consultas.FindAsync(id);
        if (consulta == null) return NotFound();

        if (consulta.FechaHora.ToUtc() > DateTime.UtcNow)
            return BadRequest(new { mensaje = "No se puede registrar una inasistencia sobre una consulta futura." });

        consulta.Estado = EstadoConsulta.Inasistencia;
        await _context.SaveChangesAsync();
        return Ok(consulta);
    }

    [HttpPost("{id}/atencion")]
    public async Task<IActionResult> RegistrarAtencion(int id)
    {
        var consulta = await _context.Consultas.FindAsync(id);
        if (consulta == null) return NotFound();

        if (!consulta.FechaHora.IsInCurrentYear())
            return BadRequest(new { mensaje = "Solo se pueden registrar atenciones de consultas del año en curso." });

        consulta.Estado = EstadoConsulta.Atendido;
        await _context.SaveChangesAsync();
        return Ok(consulta);
    }

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoRequest request)
    {
        var consulta = await _context.Consultas.FindAsync(id);
        if (consulta == null) return NotFound();

        consulta.Estado = request.Estado;
        await _context.SaveChangesAsync();
        return Ok(consulta);
    }
}

public class ActualizarEstadoRequest
{
    public EstadoConsulta Estado { get; set; }
}
