using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SchoolAdmission.Tests.Infrastructure;

// Test-side views of the API contract. Status is kept as a string so tests assert on
// the exact wire values ("HELD", "CONFIRMED", ...).
public record ClassResponse(Guid Id, string Name, int TotalSeats);
public record ClassSeatsResponse(Guid Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats);
public record HoldResponse(Guid HoldId, Guid ClassId, string? ClassName, string? StudentId, string Status, DateTime ExpiresAt);
public record ProblemResponse(int Status, string? Title, string? Detail, string? Code);

/// <summary>Thin typed wrapper around the HTTP API used by the tests.</summary>
public sealed class ApiClient(HttpClient http)
{
    public const string StudentHeader = "X-Student-Id";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public HttpClient Http => http;

    public async Task<ClassResponse> CreateClassAsync(int totalSeats, string? name = null)
    {
        var response = await http.PostAsJsonAsync("/classes",
            new { name = name ?? $"Class {Guid.NewGuid():N}", totalSeats });
        await EnsureStatus(response, HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ClassResponse>(Json))!;
    }

    public async Task<ClassSeatsResponse> GetClassAsync(Guid classId)
    {
        var response = await http.GetAsync($"/classes/{classId}");
        await EnsureStatus(response, HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ClassSeatsResponse>(Json))!;
    }

    public Task<HttpResponseMessage> TryCreateHoldAsync(Guid classId, string? studentId)
        => Send(HttpMethod.Post, $"/classes/{classId}/holds", studentId);

    public async Task<HoldResponse> CreateHoldAsync(Guid classId, string studentId)
    {
        var response = await TryCreateHoldAsync(classId, studentId);
        await EnsureStatus(response, HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<HoldResponse>(Json))!;
    }

    public async Task<HoldResponse> GetHoldAsync(Guid holdId)
    {
        var response = await http.GetAsync($"/holds/{holdId}");
        await EnsureStatus(response, HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<HoldResponse>(Json))!;
    }

    public Task<HttpResponseMessage> ConfirmAsync(Guid holdId, string? studentId)
        => Send(HttpMethod.Post, $"/holds/{holdId}/confirm", studentId);

    public Task<HttpResponseMessage> CancelAsync(Guid holdId, string? studentId)
        => Send(HttpMethod.Delete, $"/holds/{holdId}", studentId);

    public async Task<List<HoldResponse>> GetMyHoldsAsync(string studentId)
    {
        var response = await Send(HttpMethod.Get, "/me/holds", studentId);
        await EnsureStatus(response, HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<HoldResponse>>(Json))!;
    }

    public static async Task<ProblemResponse> ReadProblemAsync(HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<ProblemResponse>(Json))!;

    private Task<HttpResponseMessage> Send(HttpMethod method, string url, string? studentId)
    {
        var request = new HttpRequestMessage(method, url);
        if (studentId is not null)
            request.Headers.Add(StudentHeader, studentId);
        return http.SendAsync(request);
    }

    private static async Task EnsureStatus(HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException(
                $"Expected {(int)expected} but got {(int)response.StatusCode}: {body}");
        }
    }
}

public static class StudentIds
{
    // Random start per test run (so reruns against the same DB don't clash), then sequential (never repeats).
    private static long _next = Random.Shared.NextInt64(10_000_000_000_000L, 90_000_000_000_000L);

    /// <summary>A unique, valid 17-digit birth registration number.</summary>
    public static string New() => "20" + Interlocked.Increment(ref _next).ToString("D15");
}
