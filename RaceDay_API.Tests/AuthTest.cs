using System.Net;
using System.Text.Json;
using Xunit;

namespace RaceDay_API.Tests;

public class AuthTests : ApiTestBase
{
    public AuthTests(RaceDayFactory factory) : base(factory) { }

    private static string NewEmail() => $"user_{Guid.NewGuid():N}@test.raceday.co.za";

    // ---------- Registration ----------

    [Fact]
    public async Task Register_WithValidDetails_ReturnsCreatedParticipant()
    {
        var email = NewEmail();

        var response = await NewClient().PostAsync("/api/Users/register",
            Form(("fullName", "Test Runner"), ("email", email), ("password", TestData.Password)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);

        using var doc = JsonDocument.Parse(text);
        Assert.Equal("Participant", doc.RootElement.Prop("role").GetString());
        Assert.Equal(email, doc.RootElement.Prop("email").GetString());
    }

    [Fact]
    public async Task Register_WithRoleField_StillCreatesParticipant()
    {
        var client = NewClient();
        var email = NewEmail();

        var register = await client.PostAsync("/api/Users/register", Form(
            ("fullName", "Sneaky User"), ("email", email),
            ("password", TestData.Password), ("role", "Organiser")));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsync("/api/Users/login",
            Form(("email", email), ("password", TestData.Password)));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal("Participant", (await login.ReadJsonAsync()).Prop("role").GetString());
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = NewClient();
        var email = NewEmail();
        var form = new[] { ("fullName", "First User"), ("email", email), ("password", TestData.Password) };

        var first = await client.PostAsync("/api/Users/register", Form(form));
        var second = await client.PostAsync("/api/Users/register", Form(form));
        var differentCase = await client.PostAsync("/api/Users/register", Form(
            ("fullName", "First User"), ("email", email.ToUpperInvariant()), ("password", TestData.Password)));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, differentCase.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_ReturnsBadRequest()
    {
        var response = await NewClient().PostAsync("/api/Users/register",
            Form(("fullName", "Test Runner"), ("email", "not-an-email"), ("password", TestData.Password)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        var response = await NewClient().PostAsync("/api/Users/register",
            Form(("fullName", "Test Runner"), ("email", NewEmail()), ("password", "short")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_MissingFields_ReturnsBadRequest()
    {
        var response = await NewClient().PostAsync("/api/Users/register", Form());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_SeededOrganiser_ReturnsOkWithOrganiserRole()
    {
        var response = await NewClient().PostAsync("/api/Users/login",
            Form(("email", TestData.OrganiserEmail), ("password", TestData.Password)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Organiser", (await response.ReadJsonAsync()).Prop("role").GetString());
    }

    [Fact]
    public async Task Login_RegisteredParticipant_ReturnsOkWithParticipantRole()
    {
        var (_, email, _) = await RegisterParticipantAsync();

        var response = await NewClient().PostAsync("/api/Users/login",
            Form(("email", email), ("password", TestData.Password)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Participant", (await response.ReadJsonAsync()).Prop("role").GetString());
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var response = await NewClient().PostAsync("/api/Users/login",
            Form(("email", TestData.OrganiserEmail), ("password", "WrongPassword!")));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var response = await NewClient().PostAsync("/api/Users/login",
            Form(("email", NewEmail()), ("password", TestData.Password)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Sessions ----------

    [Fact]
    public async Task Profile_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await NewClient().GetAsync("/api/Users/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_AfterLogin_ReturnsOwnDetails()
    {
        var (client, email, userId) = await RegisterParticipantAsync();

        var response = await client.GetAsync("/api/Users/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.ReadJsonAsync();
        Assert.Equal(email, json.Prop("email").GetString());
        Assert.Equal(userId, json.Prop("userID").GetInt32());
    }

    [Fact]
    public async Task UpdateProfile_ChangesName()
    {
        var (client, email, _) = await RegisterParticipantAsync();

        var update = await client.PutAsync("/api/Users/profile",
            Form(("fullName", "Renamed Runner"), ("email", email)));
        var profile = await client.GetAsync("/api/Users/profile");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Renamed Runner", (await profile.ReadJsonAsync()).Prop("fullName").GetString());
    }

    [Fact]
    public async Task UpdateProfile_EmailUsedByAnotherUser_ReturnsConflict()
    {
        var (_, firstEmail, _) = await RegisterParticipantAsync();
        var (second, _, _) = await RegisterParticipantAsync();

        var response = await second.PutAsync("/api/Users/profile",
            Form(("fullName", "Second User"), ("email", firstEmail)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var (client, _, _) = await RegisterParticipantAsync();

        var logout = await client.PostAsync("/api/Users/logout", null);
        var profile = await client.GetAsync("/api/Users/profile");

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await NewClient().PostAsync("/api/Users/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}