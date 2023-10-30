using Microsoft.AspNetCore.Mvc;
using report_service.Requests;
using report_service.Services.Interfaces;

namespace report_service.Controllers;

[ApiController]
[Route("[controller]")]
public class ReportGeneratorController : ControllerBase
{
    private readonly IReportGeneratorService _service;
    private static int _taskId = 1;
    private static readonly Dictionary<int, Task<string>> Tasks = new ();

    public ReportGeneratorController(IReportGeneratorService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(200)]
    [Route("/report_generator/add_task")]
    public IActionResult AddGenerateReportTask(GetReportRequest request)
    {
        var reportTask = _service.GenerateReport(request.ServiceName, request.LogsPath);
        Tasks.Add(_taskId++, reportTask);

        return Ok($"Created task with ID: {_taskId - 1}");
    }

    [HttpGet]
    [ProducesResponseType(200)]
    [Route("/report_generator/system_status")]
    public IActionResult GetSystemStatus()
    {
        var taskInProgressIds = Tasks
            .ToList()
            .Where(pair => !pair.Value.IsCompleted)
            .Select(pair => pair.Key)
            .ToArray();

        if (taskInProgressIds.Length == 0)
            return Ok("All tasks are completed");

        return Ok($"Tasks: {string.Join("; ", taskInProgressIds)} are in progress");
    }

    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(202)]
    [ProducesResponseType(400)]
    [Route("/report_generator/task_status")]
    public IActionResult GetGenerateReportTaskStatus(int taskId)
    {
        if (!Tasks.ContainsKey(taskId))
            return StatusCode(
                StatusCodes.Status400BadRequest,
                "Wrong task id provided");
        if (!Tasks[taskId].IsCompleted)
            return StatusCode(
                StatusCodes.Status202Accepted, 
                "Report generation in progress, please wait");
        if (Tasks[taskId].Exception != null && 
            Tasks[taskId].Exception!.InnerException is DirectoryNotFoundException)
            return StatusCode(
                StatusCodes.Status400BadRequest, 
                Tasks[taskId].Exception!.InnerException!.Message);

        return Ok(Tasks[taskId].Result);
    }
}