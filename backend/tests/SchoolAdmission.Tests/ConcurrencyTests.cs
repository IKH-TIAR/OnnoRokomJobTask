using System.Net;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests;

/// <summary>
/// The core rule of the system: no matter how many requests arrive at the same time,
/// held + confirmed seats never exceed totalSeats.
/// </summary>
[Collection(ApiCollection.Name)]
public class ConcurrencyTests(ApiFixture fixture)
{
    private readonly ApiClient _api = new(fixture.Api.CreateClient());

    [Fact]
    public async Task ManyStudentsApplyingAtOnce_NeverOverbookTheClass()
    {
        const int seats = 10;
        const int applicants = 300;
        var schoolClass = await _api.CreateClassAsync(seats);

        var statuses = await RunAllAtOnce(applicants,
            _ => _api.TryCreateHoldAsync(schoolClass.Id, StudentIds.New()));

        Assert.Equal(seats, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(applicants - seats, statuses.Count(s => s == HttpStatusCode.Conflict));

        var summary = await _api.GetClassAsync(schoolClass.Id);
        Assert.Equal((seats, 0, 0), (summary.HeldSeats, summary.ConfirmedSeats, summary.AvailableSeats));
        Assert.Equal(seats, await fixture.CountOccupiedSeatsInDbAsync(schoolClass.Id));
    }

    [Fact]
    public async Task TwoBackendInstancesSharingTheDatabase_NeverOverbookTheClass()
    {
        const int seats = 10;
        const int applicants = 300;
        var first = _api;
        var second = new ApiClient(fixture.SecondApi.CreateClient());
        var schoolClass = await first.CreateClassAsync(seats);

        // Alternate requests between the two independent app instances
        var statuses = await RunAllAtOnce(applicants, i =>
            (i % 2 == 0 ? first : second).TryCreateHoldAsync(schoolClass.Id, StudentIds.New()));

        Assert.Equal(seats, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(applicants - seats, statuses.Count(s => s == HttpStatusCode.Conflict));
        Assert.Equal(seats, await fixture.CountOccupiedSeatsInDbAsync(schoolClass.Id));
    }

    [Fact]
    public async Task SameStudentApplyingManyTimesAtOnce_GetsExactlyOneHold()
    {
        var schoolClass = await _api.CreateClassAsync(totalSeats: 10);
        var student = StudentIds.New();

        var statuses = await RunAllAtOnce(50, _ => _api.TryCreateHoldAsync(schoolClass.Id, student));

        Assert.Equal(1, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(49, statuses.Count(s => s == HttpStatusCode.Conflict));
        Assert.Single(await _api.GetMyHoldsAsync(student));
    }

    [Fact]
    public async Task ConfirmingTheSameHoldManyTimesAtOnce_CountsTheSeatOnce()
    {
        var schoolClass = await _api.CreateClassAsync(totalSeats: 5);
        var student = StudentIds.New();
        var hold = await _api.CreateHoldAsync(schoolClass.Id, student);

        var statuses = await RunAllAtOnce(50, _ => _api.ConfirmAsync(hold.HoldId, student));

        Assert.All(statuses, s => Assert.Equal(HttpStatusCode.OK, s));
        var summary = await _api.GetClassAsync(schoolClass.Id);
        Assert.Equal((0, 1, 4), (summary.HeldSeats, summary.ConfirmedSeats, summary.AvailableSeats));
    }

    [Fact]
    public async Task ApplyConfirmAndCancelAllAtOnce_KeepSeatCountsConsistent()
    {
        const int seats = 10;
        var schoolClass = await _api.CreateClassAsync(seats);

        // Fill the class, then simultaneously: half the holders pay, half cancel, and 200 new
        // students try to grab the seats being released.
        var holders = new List<(HoldResponse Hold, string Student)>();
        for (var i = 0; i < seats; i++)
        {
            var student = StudentIds.New();
            holders.Add((await _api.CreateHoldAsync(schoolClass.Id, student), student));
        }

        var payers = holders.Take(seats / 2).ToList();
        var cancellers = holders.Skip(seats / 2).ToList();

        var confirmTasks = payers.Select(h => _api.ConfirmAsync(h.Hold.HoldId, h.Student));
        var cancelTasks = cancellers.Select(h => _api.CancelAsync(h.Hold.HoldId, h.Student));
        var applyTasks = Enumerable.Range(0, 200).Select(_ => _api.TryCreateHoldAsync(schoolClass.Id, StudentIds.New()));

        var confirmAll = Task.WhenAll(confirmTasks);
        var cancelAll = Task.WhenAll(cancelTasks);
        var applyAll = Task.WhenAll(applyTasks);
        await Task.WhenAll(confirmAll, cancelAll, applyAll);
        var (confirmResults, cancelResults, applyResults) = (await confirmAll, await cancelAll, await applyAll);

        Assert.All(confirmResults, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.All(cancelResults, r => Assert.Equal(HttpStatusCode.NoContent, r.StatusCode));

        // Only the released seats can have been re-taken
        var newHolds = applyResults.Count(r => r.StatusCode == HttpStatusCode.Created);
        Assert.InRange(newHolds, 0, cancellers.Count);

        var summary = await _api.GetClassAsync(schoolClass.Id);
        Assert.Equal(payers.Count, summary.ConfirmedSeats);
        Assert.Equal(newHolds, summary.HeldSeats);
        Assert.True(summary.HeldSeats + summary.ConfirmedSeats <= seats);
        Assert.Equal(summary.HeldSeats + summary.ConfirmedSeats, await fixture.CountOccupiedSeatsInDbAsync(schoolClass.Id));
    }

    /// <summary>Starts all requests together (released by one gate) and returns their status codes.</summary>
    private static async Task<HttpStatusCode[]> RunAllAtOnce(int count, Func<int, Task<HttpResponseMessage>> request)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = Enumerable.Range(0, count).Select(async i =>
        {
            await gate.Task;
            using var response = await request(i);
            return response.StatusCode;
        }).ToList();

        gate.SetResult();
        return await Task.WhenAll(tasks);
    }
}
