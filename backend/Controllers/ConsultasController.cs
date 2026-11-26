using ConsultasPlan.Models;
using ConsultasPlan.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConsultasPlan.Controllers;

[ApiController]
[Route("[controller]")]
public class ConsultasController : ControllerBase
{
    private readonly IConsultasService _service;

    public ConsultasController(IConsultasService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var consulta = await _service.GetByIdAsync(id, cancellationToken);
        return consulta == null ? NotFound() : Ok(consulta);
    }

    [HttpPost]
    public async Task<IActionResult> CrearConsulta([FromBody] CrearConsultaRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return result.Status switch
        {
            ServiceStatus.Success => CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data),
            ServiceStatus.NotFound => NotFound(new { mensaje = result.Message }),
            ServiceStatus.Conflict => Conflict(new { mensaje = result.Message }),
            _ => BadRequest(new { mensaje = result.Message })
        };
    }

    [HttpPost("cancelar/{id}")]
    public async Task<IActionResult> CancelarConsulta(int id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.CancelAsync(id, cancellationToken));

    [HttpPost("{id}/inasistencia")]
    public async Task<IActionResult> MarcarInasistencia(int id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.MarkAbsenceAsync(id, cancellationToken));

    [HttpPost("{id}/atencion")]
    public async Task<IActionResult> RegistrarAtencion(int id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.RegisterAttendanceAsync(id, cancellationToken));

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await _service.UpdateStateAsync(id, request.Estado, cancellationToken));

    private static IActionResult ToActionResult(ServiceResult<Consulta> result)
    {
        return result.Status switch
        {
            ServiceStatus.Success => new OkObjectResult(result.Data),
            ServiceStatus.NotFound => new NotFoundObjectResult(new { mensaje = result.Message }),
            _ => new BadRequestObjectResult(new { mensaje = result.Message })
        };
    }
}

public class ActualizarEstadoRequest
{
    public EstadoConsulta Estado { get; set; }
}
