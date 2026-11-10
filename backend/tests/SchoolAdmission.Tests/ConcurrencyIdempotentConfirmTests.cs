using System.Net;
using SchoolAdmission.Api.Contracts.Responses;
using SchoolAdmission.Tests.Fixtures;
using Xunit;

namespace SchoolAdmission.Tests;

public class ConcurrencyIdempotentConfirmTests : IClassFixture<SchoolAdmissionApiFactory>
{
    private readonly HttpClient _client;

    public ConcurrencyIdempotentConfirmTests(SchoolAdmissionApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Concurrency_IdempotentConfirm_20ConcurrentRequests_AllReturn200Confirmed_ConfirmedSeatsIncrementsBy1()
    {
        // 1. Arrange: Create a class with 10 seats
        var uniqueClassName = $"IdempotentConfirm-{Guid.NewGuid():N}";
        var createClassResponse = await _client.PostCreateClassAsync(uniqueClassName, 10);
        Assert.Equal(HttpStatusCode.Created, createClassResponse.StatusCode);

        var createdClass = await createClassResponse.ReadJsonAsync<ClassResponse>();
        Assert.NotNull(createdClass);
        var classId = createdClass.Id;

        // Apply for a hold
        var studentId = $"300000000{Random.Shared.Next(10000000, 99999999)}"; // 17 digits
        var applyResponse = await _client.PostApplyHoldAsync(classId, studentId);
        Assert.Equal(HttpStatusCode.Created, applyResponse.StatusCode);

        var hold = await applyResponse.ReadJsonAsync<HoldResponse>();
        Assert.NotNull(hold);
        var holdId = hold.HoldId;

        // Check hold status is HELD
        var holdCheck = await _client.GetHoldByIdAsync(holdId);
        Assert.Equal(HttpStatusCode.OK, holdCheck.StatusCode);
        var holdStatusBefore = await holdCheck.ReadJsonAsync<HoldStatusResponse>();
        Assert.NotNull(holdStatusBefore);
        Assert.Equal("HELD", holdStatusBefore.Status);

        // 2. Act: 20 concurrent POST /holds/{id}/confirm requests on the same hold
        const int concurrentConfirms = 20;
        var tasks = Enumerable.Range(1, concurrentConfirms).Select(async _ =>
        {
            await Task.Yield();
            return await _client.PostConfirmHoldAsync(holdId, studentId);
        }).ToList();

        var responses = await Task.WhenAll(tasks);

        // 3. Assert: All 20 return 200 OK with status "CONFIRMED"
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        foreach (var response in responses)
        {
            var confirmResult = await response.ReadJsonAsync<ConfirmHoldResponse>();
            Assert.NotNull(confirmResult);
            Assert.Equal(holdId, confirmResult.HoldId);
            Assert.Equal("CONFIRMED", confirmResult.Status);
        }

        // 4. Verify class seat summary: confirmedSeats is exactly 1, heldSeats is 0
        var classGet = await _client.GetClassByIdAsync(classId);
        Assert.Equal(HttpStatusCode.OK, classGet.StatusCode);
        var summary = await classGet.ReadJsonAsync<ClassSummaryResponse>();
        Assert.NotNull(summary);

        Assert.Equal(10, summary.TotalSeats);
        Assert.Equal(0, summary.HeldSeats);
        Assert.Equal(1, summary.ConfirmedSeats);
        Assert.Equal(9, summary.AvailableSeats);

        // Verify hold record status is CONFIRMED
        var finalHoldCheck = await _client.GetHoldByIdAsync(holdId);
        Assert.Equal(HttpStatusCode.OK, finalHoldCheck.StatusCode);
        var finalHoldStatus = await finalHoldCheck.ReadJsonAsync<HoldStatusResponse>();
        Assert.NotNull(finalHoldStatus);
        Assert.Equal("CONFIRMED", finalHoldStatus.Status);

        Console.WriteLine("\n================================================================================");
        Console.WriteLine(" [CONCURRENCY TEST RESULT: IDEMPOTENT PAYMENT CONFIRMATION]");
        Console.WriteLine($" Simultaneous Confirmations Sent : {concurrentConfirms} parallel requests");
        Console.WriteLine($" Successful Confirmations (200) : {responses.Length} requests");
        Console.WriteLine($" Total Confirmed Seats Added     : {summary.ConfirmedSeats} (Exactly 1, no duplicate counting)");
        Console.WriteLine(" Invariant Verified              : PASSED (Idempotency under race conditions)");
        Console.WriteLine("================================================================================\n");
    }
}
