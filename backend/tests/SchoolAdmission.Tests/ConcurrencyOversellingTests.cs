using System.Net;
using SchoolAdmission.Api.Contracts.Responses;
using SchoolAdmission.Tests.Fixtures;
using Xunit;

namespace SchoolAdmission.Tests;

public class ConcurrencyOversellingTests : IClassFixture<SchoolAdmissionApiFactory>
{
    private readonly HttpClient _client;

    public ConcurrencyOversellingTests(SchoolAdmissionApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Concurrency_NoOverselling_200ConcurrentRequests_For20Seats_Exactly20Succeed_180Conflict()
    {
        // 1. Arrange: Create a class with exactly 20 seats
        var uniqueClassName = $"Concurrency-20Seats-{Guid.NewGuid():N}";
        var createClassResponse = await _client.PostCreateClassAsync(uniqueClassName, 20);
        Assert.Equal(HttpStatusCode.Created, createClassResponse.StatusCode);

        var createdClass = await createClassResponse.ReadJsonAsync<ClassResponse>();
        Assert.NotNull(createdClass);
        var classId = createdClass.Id;

        // Verify initial state
        var initialClassGet = await _client.GetClassByIdAsync(classId);
        Assert.Equal(HttpStatusCode.OK, initialClassGet.StatusCode);
        var initialSummary = await initialClassGet.ReadJsonAsync<ClassSummaryResponse>();
        Assert.NotNull(initialSummary);
        Assert.Equal(20, initialSummary.TotalSeats);
        Assert.Equal(0, initialSummary.HeldSeats);
        Assert.Equal(0, initialSummary.ConfirmedSeats);
        Assert.Equal(20, initialSummary.AvailableSeats);

        // 2. Prepare 200 distinct 17-digit student IDs
        // Format: "100" + 14-digit sequential index = exactly 17 digits
        const int totalRequests = 200;
        var studentIds = Enumerable.Range(1, totalRequests)
            .Select(i => $"100{i:D14}")
            .ToList();

        // 3. Act: Send 200 concurrent POST /classes/{id}/holds requests
        // Use a barrier/Task.WhenAll to trigger concurrency simultaneously
        var tasks = studentIds.Select(async studentId =>
        {
            await Task.Yield();
            return await _client.PostApplyHoldAsync(classId, studentId);
        }).ToList();

        var responses = await Task.WhenAll(tasks);

        // 4. Assert: Exactly 20 return 201 Created, 180 return 409 Conflict
        var createdCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        var otherResponses = responses.Where(r => r.StatusCode != HttpStatusCode.Created && r.StatusCode != HttpStatusCode.Conflict).ToList();

        if (otherResponses.Count > 0)
        {
            var firstError = await otherResponses[0].Content.ReadAsStringAsync();
            var statusCounts = string.Join(", ", otherResponses.GroupBy(r => r.StatusCode).Select(g => $"{g.Key}: {g.Count()}"));
            Assert.Fail($"Received unexpected status codes ({statusCounts}). First error body: {firstError}");
        }

        Assert.Equal(20, createdCount);
        Assert.Equal(180, conflictCount);

        // 5. Query GET /classes/{id} to verify aggregate counts
        var finalClassGet = await _client.GetClassByIdAsync(classId);
        Assert.Equal(HttpStatusCode.OK, finalClassGet.StatusCode);
        var finalSummary = await finalClassGet.ReadJsonAsync<ClassSummaryResponse>();
        Assert.NotNull(finalSummary);

        Assert.Equal(20, finalSummary.TotalSeats);
        Assert.Equal(20, finalSummary.HeldSeats);
        Assert.Equal(0, finalSummary.ConfirmedSeats);
        Assert.Equal(0, finalSummary.AvailableSeats);
    }
}
