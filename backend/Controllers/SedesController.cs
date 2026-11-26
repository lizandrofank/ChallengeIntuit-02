using ConsultasPlan.Models;
using ConsultasPlan.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConsultasPlan.Controllers;

[ApiController]
[Route("[controller]")]
public class SedesController : ControllerBase
{
    private readonly ISedesService _service;

    public SedesController(ISedesService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var sede = await _service.GetByIdAsync(id, cancellationToken);
        return sede == null ? NotFound() : Ok(sede);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Sede sede, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(sede, cancellationToken);
        if (result.Status != ServiceStatus.Success)
            return BadRequest(new { mensaje = result.Message });
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Sede sede, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, sede, cancellationToken);
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
