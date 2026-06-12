using Appointly.Api.Contracts;
using Appointly.Domain.Entities;
using Appointly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Appointly.Api.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceResponse>>> List() =>
        Ok(await db.Services.Where(s => s.Active)
            .OrderBy(s => s.Name)
            .Select(s => ToResponse(s))
            .ToListAsync());

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ServiceResponse>> Create(ServiceRequest request)
    {
        var service = new Service
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            Active = request.Active,
        };
        db.Services.Add(service);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(List), new { id = service.Id }, ToResponse(service));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ServiceResponse>> Update(Guid id, ServiceRequest request)
    {
        var service = await db.Services.FindAsync(id);
        if (service is null) return NotFound();

        service.Name = request.Name.Trim();
        service.Description = request.Description;
        service.DurationMinutes = request.DurationMinutes;
        service.Price = request.Price;
        service.Active = request.Active;
        await db.SaveChangesAsync();
        return Ok(ToResponse(service));
    }

    private static ServiceResponse ToResponse(Service s) =>
        new(s.Id, s.Name, s.Description, s.DurationMinutes, s.Price, s.Active);
}
