using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace RaceDay_API.Tests;

/// <summary>Tests for the API features added in Part 3: banners, profile pictures, filters, organiser events and finisher totals.</summary>
public class Part3ApiTests : ApiTestBase
{
    public Part3ApiTests(RaceDayFactory factory) : base(factory) { }

    private static MultipartFormDataContent EventForm(
        string name, string? distance = "10", string? fileName = null, string contentType = "image/png",
        string eventType = "Running")
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(name), "name" },
            { new StringContent("Test event"), "description" },
            { new StringContent(FutureDate(30)), "eventDate" },
            { new StringContent("Soweto"), "location" },
            { new StringContent(eventType), "eventType" }
        };

        if (distance != null)
        {
            content.Add(new StringContent(distance), "distanceKm");
        }

        if (fileName != null)
        {
            var file = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
            file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(file, "banner", fileName);
        }

        return content;
    }

    // ---------- Banner and distance ----------

    [Fact]
    public async Task Organiser_CanCreateEventWithBannerAndDistance()
    {
        var organiser = await LoginOrganiserAsync();

        var response = await organiser.PostAsync("/api/Events", EventForm("Banner Run", "10", "banner.png"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.ReadJsonAsync();
        Assert.StartsWith("https://fake-storage.test/banners/", json.Prop("bannerUrl").GetString());
        Assert.Equal(10, json.Prop("distanceKm").GetDecimal());
    }

    [Fact]
    public async Task Organiser_CannotUploadANonImageBanner()
    {
        var organiser = await LoginOrganiserAsync();

        var response = await organiser.PostAsync("/api/Events",
            EventForm("Bad Banner Run", "10", "notes.txt", "text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatingAnEventWithoutAFile_KeepsTheExistingBanner()
    {
        var organiser = await LoginOrganiserAsync();
        var create = await organiser.PostAsync("/api/Events", EventForm("Keep Banner Run", "10", "banner.png"));
        var created = await create.ReadJsonAsync();
        var eventId = created.Prop("eventID").GetInt32();
        var bannerUrl = created.Prop("bannerUrl").GetString();

        var update = await organiser.PutAsync($"/api/Events/{eventId}", EventForm("Renamed Banner Run", "12"));
        var get = await organiser.GetAsync($"/api/Events/{eventId}");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var json = await get.ReadJsonAsync();
        Assert.Equal("Renamed Banner Run", json.Prop("name").GetString());
        Assert.Equal(bannerUrl, json.Prop("bannerUrl").GetString());
    }

    [Fact]
    public async Task UpdatingAnEventWithANewFile_ReplacesTheBanner()
    {
        var organiser = await LoginOrganiserAsync();
        var create = await organiser.PostAsync("/api/Events", EventForm("Replace Banner Run", "10", "one.png"));
        var created = await create.ReadJsonAsync();
        var eventId = created.Prop("eventID").GetInt32();
        var oldUrl = created.Prop("bannerUrl").GetString();

        var update = await organiser.PutAsync($"/api/Events/{eventId}", EventForm("Replace Banner Run", "10", "two.png"));

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.NotEqual(oldUrl, (await update.ReadJsonAsync()).Prop("bannerUrl").GetString());
    }

    [Fact]
    public async Task Participant_CannotUploadAnEventBanner()
    {
        var (participant, _, _) = await RegisterParticipantAsync();

        var response = await participant.PostAsync("/api/Events", EventForm("Sneaky Run", "10", "banner.png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Filters ----------

    [Fact]
    public async Task Events_CanBeFilteredBySearchAndType()
    {
        var organiser = await LoginOrganiserAsync();
        var token = $"Filter{Guid.NewGuid():N}";
        await organiser.PostAsync("/api/Events", EventForm(token, eventType: "Running"));

        var found = await organiser.GetAsync($"/api/Events?search={token}");
        var wrongType = await organiser.GetAsync($"/api/Events?search={token}&eventType=Cycling");
        var rightType = await organiser.GetAsync($"/api/Events?search={token}&eventType=Running");

        var foundNames = (await found.ReadJsonAsync()).EnumerateArray().Select(e => e.Prop("name").GetString()).ToList();
        Assert.Equal(new[] { token }, foundNames);
        Assert.Empty((await wrongType.ReadJsonAsync()).EnumerateArray());
        Assert.Single((await rightType.ReadJsonAsync()).EnumerateArray());
    }

    [Fact]
    public async Task Events_CanBeFilteredByFromDate()
    {
        var organiser = await LoginOrganiserAsync();
        var token = $"Dated{Guid.NewGuid():N}";
        await organiser.PostAsync("/api/Events", EventForm(token));

        var soon = await organiser.GetAsync($"/api/Events?search={token}&from={FutureDate(1)}");
        var tooLate = await organiser.GetAsync($"/api/Events?search={token}&from={FutureDate(90)}");

        Assert.Single((await soon.ReadJsonAsync()).EnumerateArray());
        Assert.Empty((await tooLate.ReadJsonAsync()).EnumerateArray());
    }

    // ---------- Organiser events ----------

    [Fact]
    public async Task Organiser_CanListOwnEventsWithEnrolmentCounts()
    {
        var scenario = await CreateScenarioAsync();

        var response = await scenario.Organiser.GetAsync("/api/Events/mine");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mine = (await response.ReadJsonAsync()).EnumerateArray()
            .Single(e => e.Prop("eventID").GetInt32() == scenario.EventId);
        Assert.Equal(1, mine.Prop("enrolmentCount").GetInt32());
        Assert.Equal(1, mine.Prop("categoryCount").GetInt32());
    }

    [Fact]
    public async Task OtherOrganiser_DoesNotSeeSomeoneElsesEventsInMine()
    {
        var scenario = await CreateScenarioAsync();
        var other = await LoginOtherOrganiserAsync();

        var response = await other.GetAsync("/api/Events/mine");

        var ids = (await response.ReadJsonAsync()).EnumerateArray().Select(e => e.Prop("eventID").GetInt32());
        Assert.DoesNotContain(scenario.EventId, ids);
    }

    [Fact]
    public async Task Participant_CannotListOrganiserEvents()
    {
        var (participant, _, _) = await RegisterParticipantAsync();

        var response = await participant.GetAsync("/api/Events/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Results ----------

    [Fact]
    public async Task Results_IncludeTheTotalNumberOfFinishers()
    {
        var scenario = await CreateScenarioAsync();
        await scenario.Organiser.PostAsync("/api/Results", Form(
            ("enrolmentID", scenario.EnrolmentId.ToString()),
            ("finishTime", "00:31:42"),
            ("position", "1")));

        var response = await scenario.Participant.GetAsync("/api/Results/my");

        var result = (await response.ReadJsonAsync()).EnumerateArray()
            .Single(r => r.Prop("enrolmentID").GetInt32() == scenario.EnrolmentId);
        Assert.Equal(1, result.Prop("totalFinishers").GetInt32());
        Assert.Equal(1, result.Prop("position").GetInt32());
    }

    // ---------- Profile picture ----------

    [Fact]
    public async Task Participant_CanUploadAProfilePicture()
    {
        var (participant, email, _) = await RegisterParticipantAsync();

        var form = new MultipartFormDataContent
        {
            { new StringContent("Picture Runner"), "fullName" },
            { new StringContent(email), "email" }
        };
        var file = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(file, "profilePicture", "me.jpg");

        var update = await participant.PutAsync("/api/Users/profile", form);
        var profile = await participant.GetAsync("/api/Users/profile");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.StartsWith("https://fake-storage.test/profiles/",
            (await profile.ReadJsonAsync()).Prop("profilePictureUrl").GetString());
    }

    [Fact]
    public async Task Profile_RejectsANonImageFile()
    {
        var (participant, email, _) = await RegisterParticipantAsync();

        var form = new MultipartFormDataContent
        {
            { new StringContent("Picture Runner"), "fullName" },
            { new StringContent(email), "email" }
        };
        var file = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "profilePicture", "cv.pdf");

        var response = await participant.PutAsync("/api/Users/profile", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
