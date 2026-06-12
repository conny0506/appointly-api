using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Appointly.Api.Contracts;

namespace Appointly.Tests.Integration;

public class BookingFlowTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public BookingFlowTests(ApiTestFactory factory) => _factory = factory;

    private static DateTimeOffset NextMondayAt(int hour)
    {
        var date = DateTimeOffset.UtcNow.Date.AddDays(8);
        while (date.DayOfWeek != DayOfWeek.Monday)
            date = date.AddDays(1);
        return new DateTimeOffset(date.AddHours(hour), TimeSpan.Zero);
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Test Customer", "S3cret!Password"));
        register.EnsureSuccessStatusCode();

        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Health_endpoint_returns_ok()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_rejects_duplicate_email()
    {
        var client = _factory.CreateClient();
        var request = new RegisterRequest("dupe@test.local", "Dupe", "S3cret!Password");

        var first = await client.PostAsJsonAsync("/api/auth/register", request);
        var second = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("login@test.local", "Login Test", "S3cret!Password"));

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("login@test.local", "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Booking_requires_authentication()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/appointments",
            new BookAppointmentRequest(Guid.NewGuid(), Guid.NewGuid(), NextMondayAt(10), null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Full_booking_flow_succeeds_and_double_booking_is_rejected()
    {
        var client = await AuthenticatedClientAsync("flow@test.local");

        // Seeded provider and service come from DbSeeder.
        var providers = await client.GetFromJsonAsync<List<ProviderResponse>>("/api/providers");
        var provider = Assert.Single(providers!);
        var service = provider.Services.First(s => s.Name == "Haircut");

        var startsAt = NextMondayAt(10);
        var booked = await client.PostAsJsonAsync("/api/appointments",
            new BookAppointmentRequest(provider.Id, service.Id, startsAt, "First visit"));
        Assert.Equal(HttpStatusCode.Created, booked.StatusCode);

        // Same slot again -> conflict detected by BookingPolicy.
        var rival = await AuthenticatedClientAsync("rival@test.local");
        var conflict = await rival.PostAsJsonAsync("/api/appointments",
            new BookAppointmentRequest(provider.Id, service.Id, startsAt, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, conflict.StatusCode);

        // Booked slot no longer appears in availability.
        var date = startsAt.ToString("yyyy-MM-dd");
        var slots = await client.GetFromJsonAsync<List<SlotResponse>>(
            $"/api/providers/{provider.Id}/availability?serviceId={service.Id}&date={date}");
        Assert.DoesNotContain(slots!, s => s.StartsAt == startsAt);

        // Appointment shows up under /mine.
        var mine = await client.GetFromJsonAsync<List<AppointmentResponse>>("/api/appointments/mine");
        var appointment = Assert.Single(mine!);
        Assert.Equal("Pending", appointment.Status);

        // Cancelling far before start succeeds and frees the slot.
        var cancel = await client.PostAsJsonAsync(
            $"/api/appointments/{appointment.Id}/cancel",
            new CancelAppointmentRequest("Change of plans"));
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        var slotsAfterCancel = await client.GetFromJsonAsync<List<SlotResponse>>(
            $"/api/providers/{provider.Id}/availability?serviceId={service.Id}&date={date}");
        Assert.Contains(slotsAfterCancel!, s => s.StartsAt == startsAt);
    }

    [Fact]
    public async Task Booking_outside_working_hours_is_rejected()
    {
        var client = await AuthenticatedClientAsync("nighowl@test.local");

        var providers = await client.GetFromJsonAsync<List<ProviderResponse>>("/api/providers");
        var provider = providers!.First();
        var service = provider.Services.First();

        var response = await client.PostAsJsonAsync("/api/appointments",
            new BookAppointmentRequest(provider.Id, service.Id, NextMondayAt(22), null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Admin_endpoints_reject_customers()
    {
        var client = await AuthenticatedClientAsync("customer@test.local");

        var response = await client.PostAsJsonAsync("/api/services",
            new ServiceRequest("Sneaky Service", null, 30, 10m));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_service()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("admin@appointly.local", "ChangeMe!123"));
        login.EnsureSuccessStatusCode();

        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var response = await client.PostAsJsonAsync("/api/services",
            new ServiceRequest("Beard Trim", "Quick trim", 15, 12.5m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
