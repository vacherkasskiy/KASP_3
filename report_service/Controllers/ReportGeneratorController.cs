using Microsoft.AspNetCore.Mvc;
using report_service.Requests;
using report_service.Services;

namespace report_service.Controllers;

[ApiController]
[Route("[controller]")]
public class ReportGeneratorController : ControllerBase
{
    private readonly ReportGeneratorService _service;

    public ReportGeneratorController(ReportGeneratorService service)
    {
        _service = service;
    }
    
    [HttpGet]
    [Route("/report_generator/generate")]
    public async Task<IActionResult> GenerateReport([FromQuery]GetReportRequest request)
    {
        try
        {
            var response = await _service.GenerateReport(request.ServiceName, request.LogsPath);
            return Ok(response);
        }
        catch
        {
            return BadRequest();
        }
    }
}