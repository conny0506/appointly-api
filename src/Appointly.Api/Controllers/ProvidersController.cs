using Appointly.Api.Contracts;
using Appointly.Domain.Booking;
using Appointly.Domain.Entities;
using Appointly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Appointly.Api.Controllers;

[ApiController]
[Route("api/providers")]
public class ProvidersController(AppDbContext db, TimeProvider clock) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProviderResponse>>> List()
    {
        var providers = await db.Providers
            .Where(p => p.Active)
            .Include(p => p.Services.Where(s => s.Active))
            .Include(p => p.WorkingHours)
            .OrderBy(p => p.FullName)
            .ToListAsync();

        return Ok(providers.Select(ToResponse).ToList());
    }

    /// <summary>Free slots for a provider+service on a given day (UTC).</summary>
    [HttpGet("{id:guid}/availability")]
    public async Task<ActionResult<IReadOnlyList<SlotResponse>>> Availability(
        Guid id, [FromQuery] Guid serviceId, [FromQuery] DateOnly date)
    {
        var provider = await db.Providers
            .Include(p => p.Services)
            .Include(p => p.WorkingHours)
            .FirstOrDefaultAsync(p => p.Id == id && p.Active);
        if (provider is null) return NotFound();

        var service = provider.Services.FirstOrDefault(s => s.Id == serviceId && s.Active);
        if (service is null)
            return BadRequest(new ProblemDetails { Title = "Provider does not offer this service." });

        var dayStart = new DateTimeOffset(date, TimeOnly.MinValue, TimeSpan.Zero);
        var appointments = await db.Appointments
            .Where(a => a.ProviderId == id
                && a.StartsAt >= dayStart && a.StartsAt < dayStart.AddDays(1))
            .ToListAsync();

        var slots = AvailabilityCalculator.GetAvailableSlots(
            provider, service, date, appointments, clock.GetUtcNow());

        return Ok(slots.Select(s => new SlotResponse(s.StartsAt, s.EndsAt)).ToList());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProviderResponse>> Create(ProviderRequest request)
    {
        var provider = new Provider
        {
            FullName = request.FullName.Trim(),
            Title = request.Title,
            Active = request.Active,
        };
        db.Providers.Add(provider);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(List), new { id = provider.Id }, ToResponse(provider));
    }

    [HttpPut("{id:guid}/working-hours")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProviderResponse>> SetWorkingHours(
        Guid id, List<WorkingHoursRequest> request)
    {
        if (request.Any(w => w.StartTime >= w.EndTime))
            return BadRequest(new ProblemDetails { Title = "StartTime must be before EndTime." });

        var provider = await db.Providers
            .Include(p => p.WorkingHours)
            .Include(p => p.Services)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (provider is null) return NotFound();

        provider.WorkingHours.Clear();
        provider.WorkingHours.AddRange(request.Select(w => new WorkingHours
        {
            ProviderId = id,
            DayOfWeek = w.DayOfWeek,
            StartTime = w.StartTime,
            EndTime = w.EndTime,
        }));

        await db.SaveChangesAsync();
        return Ok(ToResponse(provider));
    }

    [HttpPut("{id:guid}/services")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProviderResponse>> SetServices(Guid id, List<Guid> serviceIds)
    {
        var provider = await db.Providers
            .Include(p => p.Services)
            .Include(p => p.WorkingHours)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (provider is null) return NotFound();

        var services = await db.Services.Where(s => serviceIds.Contains(s.Id)).ToListAsync();
        provider.Services.Clear();
        provider.Services.AddRange(services);

        await db.SaveChangesAsync();
        return Ok(ToResponse(provider));
    }

    private static ProviderResponse ToResponse(Provider p) => new(
        p.Id,
        p.FullName,
        p.Title,
        p.Active,
        p.Services.Select(s => new ServiceResponse(
            s.Id, s.Name, s.Description, s.DurationMinutes, s.Price, s.Active)).ToList(),
        p.WorkingHours.OrderBy(w => w.DayOfWeek).ThenBy(w => w.StartTime)
            .Select(w => new WorkingHoursResponse(w.DayOfWeek, w.StartTime, w.EndTime)).ToList());
}
