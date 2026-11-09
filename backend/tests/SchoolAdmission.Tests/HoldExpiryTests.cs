using System.Net;
using SchoolAdmission.Api.Contracts.Responses;
using SchoolAdmission.Tests.Fixtures;
using Xunit;

namespace SchoolAdmission.Tests;

public class HoldExpiryTests : IClassFixture<ExpiryTestApiFactory>
{
    private readonly HttpClient _client;

    public HoldExpiryTests(ExpiryTestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Expiry_HoldWithShortDuration_ExpiredWithoutApiCalls_SeatReturnedToPool_ConfirmReturns409()
    {
        // 1. Arrange: Create a class with exactly 1 seat
        var uniqueClassName = $"ExpiryTest-{Guid.NewGuid():N}";
        var createClassResponse = await _client.PostCreateClassAsync(uniqueClassName, 1);
        Assert.Equal(HttpStatusCode.Created, createClassResponse.StatusCode);

        var createdClass = await createClassResponse.ReadJsonAsync<ClassResponse>();
        Assert.NotNull(createdClass);
        var classId = createdClass.Id;

        // Apply for hold (ExpiryTestApiFactory sets HOLD_DURATION_SECONDS = 2)
        var studentId = $"400000000{Random.Shared.Next(10000000, 99999999)}";
        var applyResponse = await _client.PostApplyHoldAsync(classId, studentId);
        Assert.Equal(HttpStatusCode.Created, applyResponse.StatusCode);

        var hold = await applyResponse.ReadJsonAsync<HoldResponse>();
        Assert.NotNull(hold);
        var holdId = hold.HoldId;

        // Verify hold is active immediately after creation
        var classCheckImmediately = await _client.GetClassByIdAsync(classId);
        Assert.Equal(HttpStatusCode.OK, classCheckImmediately.StatusCode);
        var summaryImmediately = await classCheckImmediately.ReadJsonAsync<ClassSummaryResponse>();
        Assert.NotNull(summaryImmediately);
        Assert.Equal(1, summaryImmediately.HeldSeats);
        Assert.Equal(0, summaryImmediately.AvailableSeats);

        // 2. Act: Wait 3 seconds with NO API calls
        await Task.Delay(TimeSpan.FromSeconds(3));

        // 3. Assert: Seat is returned to available pool
        var classCheckAfterExpiry = await _client.GetClassByIdAsync(classId);
        Assert.Equal(HttpStatusCode.OK, classCheckAfterExpiry.StatusCode);
        var summaryAfterExpiry = await classCheckAfterExpiry.ReadJsonAsync<ClassSummaryResponse>();
        Assert.NotNull(summaryAfterExpiry);
        Assert.Equal(0, summaryAfterExpiry.HeldSeats);
        Assert.Equal(0, summaryAfterExpiry.ConfirmedSeats);
        Assert.Equal(1, summaryAfterExpiry.AvailableSeats); // Seat freed!

        // Assert: GET /holds/{id} reports EXPIRED
        var holdCheckAfterExpiry = await _client.GetHoldByIdAsync(holdId);
        Assert.Equal(HttpStatusCode.OK, holdCheckAfterExpiry.StatusCode);
        var holdStatusAfterExpiry = await holdCheckAfterExpiry.ReadJsonAsync<HoldStatusResponse>();
        Assert.NotNull(holdStatusAfterExpiry);
        Assert.Equal("EXPIRED", holdStatusAfterExpiry.Status);

        // Assert: Confirm returns 409 Conflict with code HOLD_EXPIRED
        var confirmResponse = await _client.PostConfirmHoldAsync(holdId, studentId);
        Assert.Equal(HttpStatusCode.Conflict, confirmResponse.StatusCode);

        var problemDetails = await confirmResponse.ReadProblemDetailsAsync();
        Assert.NotNull(problemDetails);
        Assert.Equal("HOLD_EXPIRED", problemDetails.Code);
        Assert.Equal(409, problemDetails.Status);
    }
}
