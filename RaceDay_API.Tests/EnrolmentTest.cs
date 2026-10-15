using System.Net;
using Xunit;

namespace RaceDay_API.Tests;

public class EnrolmentTests : ApiTestBase
{
    public EnrolmentTests(RaceDayFactory factory) : base(factory) { }

    [Fact]
    public async Task Participant_CanEnrol_AndIsAssignedABibNumber()
    {
        var organiser = await LoginOrganiserAsync();
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);
        var (participant, _, userId) = await RegisterParticipantAsync();

        var response = await EnrolAsync(participant, categoryId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.ReadJsonAsync();
        Assert.Equal("Active", json.Prop("status").GetString());
        Assert.StartsWith("RD", json.Prop("bibNumber").GetString());
        Assert.Equal(categoryId, json.Prop("categoryID").GetInt32());
        Assert.Equal(userId, json.Prop("participantID").GetInt32());
        Assert.Equal(eventId, json.Prop("eventID").GetInt32());
    }

    [Fact]
    public async Task Enrolment_IsRecordedAndVisibleToTheParticipant()
    {
        var scenario = await CreateScenarioAsync();

        var response = await scenario.Participant.GetAsync("/api/Enrolments/my");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var enrolment = (await response.ReadJsonAsync()).EnumerateArray()
            .Single(e => e.Prop("enrolmentID").GetInt32() == scenario.EnrolmentId);
        Assert.Equal(scenario.CategoryId, enrolment.Prop("categoryID").GetInt32());
        Assert.Equal(scenario.ParticipantId, enrolment.Prop("participantID").GetInt32());
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnEvent()
    {
        var scenario = await CreateScenarioAsync();

        var response = await scenario.Organiser.GetAsync($"/api/Enrolments?eventId={scenario.EventId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var enrolment = (await response.ReadJsonAsync()).EnumerateArray()
            .Single(e => e.Prop("enrolmentID").GetInt32() == scenario.EnrolmentId);
        Assert.Equal(TestData.ParticipantName, enrolment.Prop("participantName").GetString());
    }

    [Fact]
    public async Task OtherOrganiser_CannotSeeEnrolmentsOfSomeoneElsesEvent()
    {
        var scenario = await CreateScenarioAsync();
        var otherOrganiser = await LoginOtherOrganiserAsync();

        var response = await otherOrganiser.GetAsync($"/api/Enrolments?eventId={scenario.EventId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.ReadJsonAsync()).EnumerateArray());
    }

    [Fact]
    public async Task Participant_CannotEnrolTwiceInTheSameCategory()
    {
        var scenario = await CreateScenarioAsync();

        var second = await EnrolAsync(scenario.Participant, scenario.CategoryId);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_UnknownCategory_ReturnsNotFound()
    {
        var (participant, _, _) = await RegisterParticipantAsync();

        var response = await EnrolAsync(participant, 999999);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_FullCategory_ReturnsConflict()
    {
        var scenario = await CreateScenarioAsync(maxParticipants: 1);
        var (secondParticipant, _, _) = await RegisterParticipantAsync();

        var response = await EnrolAsync(secondParticipant, scenario.CategoryId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_PastEvent_ReturnsBadRequest()
    {
        var (participant, _, _) = await RegisterParticipantAsync();

        // The seeded "Johannesburg City Run" took place on 10 October 2026.
        var events = await (await participant.GetAsync("/api/Events")).ReadJsonAsync();
        var pastEventId = events.EnumerateArray()
            .Single(e => e.Prop("name").GetString() == "Johannesburg City Run")
            .Prop("eventID").GetInt32();
        var categories = await (await participant.GetAsync($"/api/Categories?eventId={pastEventId}")).ReadJsonAsync();
        var categoryId = categories.EnumerateArray().First().Prop("categoryID").GetInt32();

        var response = await EnrolAsync(participant, categoryId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await EnrolAsync(NewClient(), 1);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment()
    {
        var scenario = await CreateScenarioAsync();

        var response = await scenario.Participant.PutAsync(
            $"/api/Enrolments/{scenario.EnrolmentId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Cancelled", (await response.ReadJsonAsync()).Prop("status").GetString());
    }

    [Fact]
    public async Task Participant_CannotCancelSomeoneElsesEnrolment()
    {
        var scenario = await CreateScenarioAsync();
        var (otherParticipant, _, _) = await RegisterParticipantAsync();

        var response = await otherParticipant.PutAsync(
            $"/api/Enrolments/{scenario.EnrolmentId}/cancel", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewSomeoneElsesEnrolment()
    {
        var scenario = await CreateScenarioAsync();
        var (otherParticipant, _, _) = await RegisterParticipantAsync();

        var response = await otherParticipant.GetAsync($"/api/Enrolments/{scenario.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}