using System.Net;
using Xunit;

namespace RaceDay_API.Tests;

public class RoleAccessTests : ApiTestBase
{
    public RoleAccessTests(RaceDayFactory factory) : base(factory) { }

    // ---------- Not logged in ----------

    [Theory]
    [InlineData("/api/Users/profile")]
    [InlineData("/api/Events")]
    [InlineData("/api/Categories")]
    [InlineData("/api/Routes")]
    [InlineData("/api/Enrolments")]
    [InlineData("/api/Enrolments/my")]
    [InlineData("/api/Results")]
    [InlineData("/api/Results/my")]
    public async Task ProtectedEndpoints_WithoutLogin_ReturnUnauthorized(string url)
    {
        var response = await NewClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await NewClient().PostAsync("/api/Events", Form(
            ("name", "Anonymous Event"), ("description", "x"),
            ("eventDate", FutureDate(30)), ("location", "Soweto"), ("eventType", "Running")));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Participant tries Organiser functionality ----------

    [Fact]
    public async Task Participant_CannotCreateEvent()
    {
        var (participant, _, _) = await RegisterParticipantAsync();

        var response = await participant.PostAsync("/api/Events", Form(
            ("name", "Participant Event"), ("description", "x"),
            ("eventDate", FutureDate(30)), ("location", "Soweto"), ("eventType", "Running")));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUpdateOrDeleteEvent()
    {
        var organiser = await LoginOrganiserAsync();
        var eventId = await CreateEventAsync(organiser);
        var (participant, _, _) = await RegisterParticipantAsync();

        var update = await participant.PutAsync($"/api/Events/{eventId}", Form(
            ("name", "Hijacked"), ("description", "x"),
            ("eventDate", FutureDate(30)), ("location", "Soweto"), ("eventType", "Running")));
        var delete = await participant.DeleteAsync($"/api/Events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateCategory()
    {
        var organiser = await LoginOrganiserAsync();
        var eventId = await CreateEventAsync(organiser);
        var (participant, _, _) = await RegisterParticipantAsync();

        var response = await participant.PostAsync("/api/Categories", Form(
            ("eventID", eventId.ToString()), ("name", "Sneaky"), ("distanceKm", "5"),
            ("maxParticipants", "10"), ("entryFee", "0")));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewOrganiserEnrolmentList()
    {
        var (participant, _, _) = await RegisterParticipantAsync();

        var response = await participant.GetAsync("/api/Enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewOrganiserResultList()
    {
        var (participant, _, _) = await RegisterParticipantAsync();

        var response = await participant.GetAsync("/api/Results");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Organiser tries Participant functionality ----------

    [Fact]
    public async Task Organiser_CannotEnrolInACategory()
    {
        var organiser = await LoginOrganiserAsync();
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);

        var response = await EnrolAsync(organiser, categoryId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/Enrolments/my")]
    [InlineData("/api/Results/my")]
    public async Task Organiser_CannotViewParticipantOnlyEndpoints(string url)
    {
        var organiser = await LoginOrganiserAsync();

        var response = await organiser.GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Allowed ----------

    [Fact]
    public async Task Organiser_CanCreateCategory()
    {
        var organiser = await LoginOrganiserAsync();
        var eventId = await CreateEventAsync(organiser);

        var response = await organiser.PostAsync("/api/Categories", Form(
            ("eventID", eventId.ToString()), ("name", "10km Test"), ("distanceKm", "10"),
            ("maxParticipants", "50"), ("entryFee", "150")));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotAddCategoryToAnotherOrganisersEvent()
    {
        var owner = await LoginOtherOrganiserAsync();
        var eventId = await CreateEventAsync(owner);
        var intruder = await LoginOrganiserAsync();

        var response = await intruder.PostAsync("/api/Categories", Form(
            ("eventID", eventId.ToString()), ("name", "Intruder"), ("distanceKm", "5"),
            ("maxParticipants", "10"), ("entryFee", "0")));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}