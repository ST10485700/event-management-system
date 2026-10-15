using System.Net;
using Xunit;

namespace RaceDay_API.Tests;

public class EventTests : ApiTestBase
{
    public EventTests(RaceDayFactory factory) : base(factory) { }

    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        var organiser = await LoginOrganiserAsync();

        var response = await organiser.PostAsync("/api/Events", Form(
            ("name", "Soweto Test Run"), ("description", "A test event"),
            ("eventDate", FutureDate(30)), ("location", "Soweto"), ("eventType", "Running")));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.ReadJsonAsync();
        Assert.Equal("Soweto Test Run", json.Prop("name").GetString());
        Assert.Equal("Running", json.Prop("eventType").GetString());
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var organiser = await LoginOrganiserAsync();
        var eventId = await CreateEventAsync(organiser);

        var update = await organiser.PutAsync($"/api/Events/{eventId}", Form(
            ("name", "Renamed Event"), ("description", "Updated"),
            ("eventDate", FutureDate(45)), ("location", "Pretoria"), ("eventType", "Cycling")));
        var get = await organiser.GetAsync($"/api/Events/{eventId}");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var json = await get.ReadJsonAsync();
        Assert.Equal("Renamed Event", json.Prop("name").GetString());
        Assert.Equal("Cycling", json.Prop("eventType").GetString());
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        var organiser = await LoginOrganiserAsync();
        var eventId = await CreateEventAsync(organiser);

        var delete = await organiser.DeleteAsync($"/api/Events/{eventId}");
        var get = await organiser.GetAsync($"/api/Events/{eventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteEventThatHasCategories()
    {
        var organiser = await LoginOrganiserAsync();
        var eventId = await CreateEventAsync(organiser);
        await CreateCategoryAsync(organiser, eventId);

        var response = await organiser.DeleteAsync($"/api/Events/{eventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEditAnotherOrganisersEvent()
    {
        var owner = await LoginOtherOrganiserAsync();
        var eventId = await CreateEventAsync(owner);
        var intruder = await LoginOrganiserAsync();

        var response = await intruder.PutAsync($"/api/Events/{eventId}", Form(
            ("name", "Hijacked"), ("description", "x"),
            ("eventDate", FutureDate(30)), ("location", "Nowhere"), ("eventType", "Walking")));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent()
    {
        var owner = await LoginOtherOrganiserAsync();
        var eventId = await CreateEventAsync(owner);
        var intruder = await LoginOrganiserAsync();

        var response = await intruder.DeleteAsync($"/api/Events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCreateEventInThePast()
    {
        var organiser = await LoginOrganiserAsync();

        var response = await organiser.PostAsync("/api/Events", Form(
            ("name", "Old Event"), ("description", "x"),
            ("eventDate", FutureDate(-5)), ("location", "Soweto"), ("eventType", "Running")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCreateEventWithInvalidType()
    {
        var organiser = await LoginOrganiserAsync();

        var response = await organiser.PostAsync("/api/Events", Form(
            ("name", "Swim Event"), ("description", "x"),
            ("eventDate", FutureDate(30)), ("location", "Soweto"), ("eventType", "Swimming")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewEvents()
    {
        var organiser = await LoginOrganiserAsync();
        var eventId = await CreateEventAsync(organiser);
        var (participant, _, _) = await RegisterParticipantAsync();

        var list = await participant.GetAsync("/api/Events");
        var single = await participant.GetAsync($"/api/Events/{eventId}");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var ids = (await list.ReadJsonAsync()).EnumerateArray().Select(e => e.Prop("eventID").GetInt32());
        Assert.Contains(eventId, ids);
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
    }

    [Fact]
    public async Task GetEvent_UnknownId_ReturnsNotFound()
    {
        var organiser = await LoginOrganiserAsync();

        var response = await organiser.GetAsync("/api/Events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}