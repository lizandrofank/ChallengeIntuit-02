using ConsultasPlan.Models;
using ConsultasPlan.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConsultasPlan.Controllers;

[ApiController]
[Route("[controller]")]
public class ProfesionalesController : ControllerBase
{
    private readonly IProfesionalesService _service;

    public ProfesionalesController(IProfesionalesService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var profesional = await _service.GetByIdAsync(id, cancellationToken);
        return profesional == null ? NotFound() : Ok(profesional);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Profesional profesional, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(profesional, cancellationToken);
        if (result.Status == ServiceStatus.Invalid)
            return BadRequest(new { mensaje = result.Message });
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Profesional profesional, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, profesional, cancellationToken);
        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Data),
            ServiceStatus.NotFound => NotFound(new { mensaje = result.Message }),
            _ => BadRequest(new { mensaje = result.Message })
        };
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, cancellationToken);
        return result.Status switch
        {
            ServiceStatus.Success => NoContent(),
            ServiceStatus.NotFound => NotFound(new { mensaje = result.Message }),
            ServiceStatus.Conflict => Conflict(new { mensaje = result.Message }),
            _ => BadRequest(new { mensaje = result.Message })
        };
    }
}
