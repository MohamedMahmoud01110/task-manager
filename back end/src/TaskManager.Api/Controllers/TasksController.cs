using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Tasks;

namespace TaskManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController(TaskService tasks, ICurrentUser current) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<TaskDto>> Create(CreateTaskRequest request, CancellationToken ct)
    {
        var dto = await tasks.CreateAsync(current.Id, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await tasks.GetAsync(current.Id, id, ct));

    [HttpGet]
    public async Task<ActionResult<PagedResult<TaskDto>>> List([FromQuery] TaskListQuery query, CancellationToken ct)
        => Ok(await tasks.ListAsync(current.Id, query, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskDto>> Update(Guid id, UpdateTaskRequest request, CancellationToken ct)
        => Ok(await tasks.UpdateAsync(current.Id, id, request, ct));

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<TaskDto>> UpdateStatus(Guid id, UpdateStatusRequest request, CancellationToken ct)
        => Ok(await tasks.ChangeStatusAsync(current.Id, id, request, ct));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await tasks.DeleteAsync(current.Id, id, ct);
        return NoContent();
    }
}