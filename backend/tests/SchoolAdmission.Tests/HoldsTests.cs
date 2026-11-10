using System.Net;
using System.Text.Json;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests;

[Collection(ApiCollection.Name)]
public class HoldsTests(ApiFixture fixture)
{
    private readonly ApiClient _api = new(fixture.Api.CreateClient());

    // ---------- Apply (POST /classes/{classId}/holds) ----------

    [Fact]
    public async Task Apply_Returns201_Held_WithContractFields_AndExpiryFromSettings()
    {
        var schoolClass = await _api.CreateClassAsync(totalSeats: 5);
        var student = StudentIds.New();
        var before = fixture.Clock.GetUtcNow();

        var response = await _api.TryCreateHoldAsync(schoolClass.Id, student);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.False(string.IsNullOrEmpty(root.GetProperty("holdId").GetString()));
        Assert.Equal(schoolClass.Id.ToString(), root.GetProperty("classId").GetString());
        Assert.Equal(student, root.GetProperty("studentId").GetString());
        Assert.Equal("HELD", root.GetProperty("status").GetString());

        var expiresAtText = root.GetProperty("expiresAt").GetString()!;
        Assert.EndsWith("Z", expiresAtText);
        var expiresAt = DateTimeOffset.Parse(expiresAtText);
        Assert.Equal(before.AddSeconds(TestConstants.HoldDurationSeconds), expiresAt, TimeSpan.FromSeconds(1));

        var seats = await _api.GetClassAsync(schoolClass.Id);
        Assert.Equal((1, 0, 4), (seats.HeldSeats, seats.ConfirmedSeats, seats.AvailableSeats));
    }

    [Fact]
    public async Task Apply_WhenClassIsFull_Returns409_ClassFull()
    {
        var schoolClass = await _api.CreateClassAsync(totalSeats: 2);
        await _api.CreateHoldAsync(schoolClass.Id, StudentIds.New());
        await _api.CreateHoldAsync(schoolClass.Id, StudentIds.New());

        var response = await _api.TryCreateHoldAsync(schoolClass.Id, StudentIds.New());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("CLASS_FULL", (await ApiClient.ReadProblemAsync(response)).Code);
    }

    [Fact]
    public async Task Apply_SameStudentTwiceForSameClass_Returns409_AlreadyApplied()
    {
        var schoolClass = await _api.CreateClassAsync(totalSeats: 5);
        var student = StudentIds.New();
        await _api.CreateHoldAsync(schoolClass.Id, student);

        var response = await _api.TryCreateHoldAsync(schoolClass.Id, student);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("ALREADY_APPLIED", (await ApiClient.ReadProblemAsync(response)).Code);
    }

    [Fact]
    public async Task Apply_SameStudentForDifferentClasses_IsAllowed()
    {
        var morning = await _api.CreateClassAsync(totalSeats: 5);
        var day = await _api.CreateClassAsync(totalSeats: 5);
        var student = StudentIds.New();

        await _api.CreateHoldAsync(morning.Id, student);
        await _api.CreateHoldAsync(day.Id, student);
    }

    [Theory]
    [InlineData(null, "STUDENT_ID_REQUIRED")]
    [InlineData("", "STUDENT_ID_REQUIRED")]
    [InlineData("1234", "STUDENT_ID_INVALID")]
    [InlineData("2015269123456789A", "STUDENT_ID_INVALID")]
    [InlineData("201526912345678901", "STUDENT_ID_INVALID")]
    public async Task Apply_MissingOrInvalidStudentId_Returns400(string? studentId, string expectedCode)
    {
        var schoolClass = await _api.CreateClassAsync(totalSeats: 5);

        var response = await _api.TryCreateHoldAsync(schoolClass.Id, studentId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(expectedCode, (await ApiClient.ReadProblemAsync(response)).Code);
    }

    [Fact]
    public async Task Apply_UnknownClass_Returns404()
    {
        var response = await _api.TryCreateHoldAsync(Guid.NewGuid(), StudentIds.New());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- Expiry ----------

    [Fact]
    public async Task Hold_ExpiresByItself_SeatReturns_AndStudentCanApplyAgain()
    {
        var schoolClass = await _api.CreateClassAsync(totalSeats: 1);
        var student = StudentIds.New();
        var hold = await _api.CreateHoldAsync(schoolClass.Id, student);

        // Full while the hold is active
        var other = await _api.TryCreateHoldAsync(schoolClass.Id, StudentIds.New());
        Assert.Equal(HttpStatusCode.Conflict, other.StatusCode);

        // Time passes; nobody calls any API
        fixture.Clock.Advance(TimeSpan.FromSeconds(TestConstants.HoldDurationSeconds + 1));

        Assert.Equal("EXPIRED", (await _api.GetHoldAsync(hold.HoldId)).Status);
        var seats = await _api.GetClassAsync(schoolClass.Id);
        Assert.Equal((0, 0, 1), (seats.HeldSeats, seats.ConfirmedSeats, seats.AvailableSeats));

        // Expired hold can no longer be paid for
        var confirm = await _api.ConfirmAsync(hold.HoldId, student);
        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);
        Assert.Equal("HOLD_EXPIRED", (await ApiClient.ReadProblemAsync(confirm)).Code);

        // The same student may apply again
        var again = await _api.CreateHoldAsync(schoolClass.Id, student);
        Assert.Equal("HELD", again.Status);
    }

    [Fact]
    public async Task Hold_JustBeforeExpiry_CanStillBeConfirmed()
    {
        var schoolClass = await _api.CreateClassAsync(totalSeats: 1);
        var student = StudentIds.New();
        var hold = await _api.CreateHoldAsync(schoolClass.Id, student);

        fixture.Clock.Advance(TimeSpan.FromSeconds(TestConstants.HoldDurationSeconds - 1));

        Assert.Equal(HttpStatusCode.OK, (await _api.ConfirmAsync(hold.HoldId, student)).StatusCode);
    }

    // ---------- Confirm (POST /holds/{holdId}/confirm) ----------

    [Fact]
    public async Task Confirm_Returns200_Confirmed_AndIsIdempotent()
    {
        var schoolClass = await _api.CreateClassAsync(totalSeats: 3);
        var student = StudentIds.New();
        var hold = await _api.CreateHoldAsync(schoolClass.Id, student);

        for (var i = 0; i < 3; i++)
        {
            var response = await _api.ConfirmAsync(hold.HoldId, student);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(hold.HoldId.ToString(), json.RootElement.GetProperty("holdId").GetString());
            Assert.Equal("CONFIRMED", json.RootElement.GetProperty("status").GetString());
        }

        // Counted exactly once, and confirmation does not expire
        fixture.Clock.Advance(TimeSpan.FromSeconds(TestConstants.HoldDurationSeconds * 2));
        var seats = await _api.GetClassAsync(schoolClass.Id);
        Assert.Equal((0, 1, 2), (seats.HeldSeats, seats.ConfirmedSeats, seats.AvailableSeats));
        Assert.Equal("CONFIRMED", (await _api.GetHoldAsync(hold.HoldId)).Status);
    }

    [Fact]
    public async Task Confirm_SomeoneElsesHold_Returns403_AndChangesNothing()
    {
        var schoolClass = await _api.CreateClassAsync(totalSeats: 3);
        var hold = await _api.CreateHoldAsync(schoolClass.Id, StudentIds.New());

        var response = await _api.ConfirmAsync(hold.HoldId, StudentIds.New());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("HOLD_NOT_OWNED", (await ApiClient.ReadProblemAsync(response)).Code);
        Assert.Equal("HELD", (await _api.GetHoldAsync(hold.HoldId)).Status);
    }

    [Fact]
    public async Task Confirm_UnknownHold_Returns404_AndMissingHeader_Returns400()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _api.ConfirmAsync(Guid.NewGuid(), StudentIds.New())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _api.ConfirmAsync(Guid.NewGuid(), null)).StatusCode);
    }

    // ---------- Cancel (DELETE /holds/{holdId}) ----------

    [Fact]
    public async Task Cancel_Returns204_ReleasesSeat_AndCannotBeConfirmedAfterwards()
    {
        var schoolClass = await _api.CreateClassAsync(totalSeats: 1);
        var student = StudentIds.New();
        var hold = await _api.CreateHoldAsync(schoolClass.Id, student);

        var response = await _api.CancelAsync(hold.HoldId, student);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("CANCELLED", (await _api.GetHoldAsync(hold.HoldId)).Status);
        Assert.Equal(1, (await _api.GetClassAsync(schoolClass.Id)).AvailableSeats);

        // Repeating the cancel is harmless
        Assert.Equal(HttpStatusCode.NoContent, (await _api.CancelAsync(hold.HoldId, student)).StatusCode);

        var confirm = await _api.ConfirmAsync(hold.HoldId, student);
        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);
        Assert.Equal("HOLD_CANCELLED", (await ApiClient.ReadProblemAsync(confirm)).Code);

        // The freed seat can be taken by someone else
        await _api.CreateHoldAsync(schoolClass.Id, StudentIds.New());
    }

    [Fact]
    public async Task Cancel_ConfirmedAdmission_Returns409()
    {
        var schoolClass = await _api.CreateClassAsync(totalSeats: 1);
        var student = StudentIds.New();
        var hold = await _api.CreateHoldAsync(schoolClass.Id, student);
        await _api.ConfirmAsync(hold.HoldId, student);

        var response = await _api.CancelAsync(hold.HoldId, student);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("HOLD_ALREADY_CONFIRMED", (await ApiClient.ReadProblemAsync(response)).Code);
    }

    [Fact]
    public async Task Cancel_SomeoneElsesHold_Returns403()
    {
        var schoolClass = await _api.CreateClassAsync(totalSeats: 1);
        var hold = await _api.CreateHoldAsync(schoolClass.Id, StudentIds.New());

        var response = await _api.CancelAsync(hold.HoldId, StudentIds.New());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("HELD", (await _api.GetHoldAsync(hold.HoldId)).Status);
    }

    // ---------- My applications (GET /me/holds) ----------

    [Fact]
    public async Task MyHolds_ReturnsOnlyThisStudentsHolds_WithCurrentStatus_NewestFirst()
    {
        var classA = await _api.CreateClassAsync(totalSeats: 5, name: "Class 6 - Morning Shift");
        var classB = await _api.CreateClassAsync(totalSeats: 5, name: "Class 6 - Day Shift");
        var student = StudentIds.New();

        var first = await _api.CreateHoldAsync(classA.Id, student);
        fixture.Clock.Advance(TimeSpan.FromSeconds(1));
        var second = await _api.CreateHoldAsync(classB.Id, student);
        await _api.ConfirmAsync(second.HoldId, student);
        await _api.CreateHoldAsync(classA.Id, StudentIds.New()); // another student

        var mine = await _api.GetMyHoldsAsync(student);

        Assert.Equal(2, mine.Count);
        Assert.Equal(second.HoldId, mine[0].HoldId);
        Assert.Equal("CONFIRMED", mine[0].Status);
        Assert.Equal("Class 6 - Day Shift", mine[0].ClassName);
        Assert.Equal(first.HoldId, mine[1].HoldId);
        Assert.Equal("HELD", mine[1].Status);
        Assert.Equal("Class 6 - Morning Shift", mine[1].ClassName);
    }

    [Fact]
    public async Task MyHolds_UnknownStudent_ReturnsEmptyList()
    {
        Assert.Empty(await _api.GetMyHoldsAsync(StudentIds.New()));
    }
}
