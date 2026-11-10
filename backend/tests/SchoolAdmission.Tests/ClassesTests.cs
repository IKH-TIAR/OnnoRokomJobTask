using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests;

[Collection(ApiCollection.Name)]
public class ClassesTests(ApiFixture fixture)
{
    private readonly ApiClient _api = new(fixture.Api.CreateClient());

    [Fact]
    public async Task CreateClass_Returns201_WithContractFields()
    {
        var response = await _api.Http.PostAsJsonAsync("/classes", new { name = "Class 6 - Morning Shift", totalSeats = 120 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.False(string.IsNullOrEmpty(root.GetProperty("id").GetString()));
        Assert.Equal("Class 6 - Morning Shift", root.GetProperty("name").GetString());
        Assert.Equal(120, root.GetProperty("totalSeats").GetInt32());
    }

    [Fact]
    public async Task NewClass_HasAllSeatsAvailable_InListAndDetail()
    {
        var created = await _api.CreateClassAsync(totalSeats: 30);

        var detail = await _api.GetClassAsync(created.Id);
        Assert.Equal(new ClassSeatsResponse(created.Id, created.Name, 30, HeldSeats: 0, ConfirmedSeats: 0, AvailableSeats: 30), detail);

        var list = await _api.Http.GetFromJsonAsync<List<ClassSeatsResponse>>("/classes");
        Assert.Contains(detail, list!);
    }

    [Theory]
    [InlineData("""{ "name": "", "totalSeats": 10 }""")]
    [InlineData("""{ "name": "Class 7", "totalSeats": 0 }""")]
    [InlineData("""{ "name": "Class 7", "totalSeats": -5 }""")]
    [InlineData("""{ "name": "Class 7" }""")]
    [InlineData("""{ }""")]
    [InlineData("""{ "name": "Class 7", "totalSeats": "many" }""")]
    [InlineData("""{ "name": """)]
    public async Task CreateClass_InvalidInput_Returns400_WithErrorCode(string body)
    {
        var response = await _api.Http.PostAsync("/classes",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ApiClient.ReadProblemAsync(response);
        Assert.Contains(problem.Code, new[] { "VALIDATION_FAILED", "BAD_REQUEST" });
    }

    [Fact]
    public async Task GetClass_Unknown_Returns404()
    {
        var response = await _api.Http.GetAsync($"/classes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("CLASS_NOT_FOUND", (await ApiClient.ReadProblemAsync(response)).Code);
    }
}
