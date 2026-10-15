using System.Net;
using Xunit;

namespace RaceDay_API.Tests;

public class ResultTests : ApiTestBase
{
    public ResultTests(RaceDayFactory factory) : base(factory) { }

    private static FormUrlEncodedContent ResultForm(int enrolmentId, int position = 1) =>
        Form(("enrolmentID", enrolmentId.ToString()),
             ("finishTime", "00:31:42"),
             ("position", position.ToString()));

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantCanSeeIt()
    {
        var scenario = await CreateScenarioAsync();

        var capture = await scenario.Organiser.PostAsync("/api/Results", ResultForm(scenario.EnrolmentId));
        var mine = await scenario.Participant.GetAsync("/api/Results/my");

        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        var result = (await mine.ReadJsonAsync()).EnumerateArray()
            .Single(r => r.Prop("enrolmentID").GetInt32() == scenario.EnrolmentId);
        Assert.Equal(1, result.Prop("position").GetInt32());
        Assert.Equal("00:31:42", result.Prop("finishTime").GetString());
    }

    [Fact]
    public async Task CapturingAResult_MarksTheEnrolmentCompleted()
    {
        var scenario = await CreateScenarioAsync();

        await scenario.Organiser.PostAsync("/api/Results", ResultForm(scenario.EnrolmentId));
        var enrolment = await scenario.Participant.GetAsync($"/api/Enrolments/{scenario.EnrolmentId}");

        Assert.Equal("Completed", (await enrolment.ReadJsonAsync()).Prop("status").GetString());
    }

    [Fact]
    public async Task Participant_CannotCaptureAResult()
    {
        var scenario = await CreateScenarioAsync();

        var response = await scenario.Participant.PostAsync("/api/Results", ResultForm(scenario.EnrolmentId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureTwoResultsForTheSameEnrolment()
    {
        var scenario = await CreateScenarioAsync();

        var first = await scenario.Organiser.PostAsync("/api/Results", ResultForm(scenario.EnrolmentId));
        var second = await scenario.Organiser.PostAsync("/api/Results", ResultForm(scenario.EnrolmentId, 2));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResultForAnotherOrganisersEvent()
    {
        var scenario = await CreateScenarioAsync();
        var otherOrganiser = await LoginOtherOrganiserAsync();

        var response = await otherOrganiser.PostAsync("/api/Results", ResultForm(scenario.EnrolmentId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_OnlySeesTheirOwnResults()
    {
        var scenario = await CreateScenarioAsync();
        await scenario.Organiser.PostAsync("/api/Results", ResultForm(scenario.EnrolmentId));
        var (otherParticipant, _, _) = await RegisterParticipantAsync();

        var response = await otherParticipant.GetAsync("/api/Results/my");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.ReadJsonAsync()).EnumerateArray());
    }
}