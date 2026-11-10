using System.Net;
using SchoolAdmission.Api.Contracts.Responses;
using SchoolAdmission.Tests.Fixtures;
using Xunit;

namespace SchoolAdmission.Tests;

public class ConcurrencySingleActiveApplicationTests : IClassFixture<SchoolAdmissionApiFactory>
{
    private readonly HttpClient _client;

    public ConcurrencySingleActiveApplicationTests(SchoolAdmissionApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Concurrency_SingleActiveApplication_30ParallelRequests_SameStudentSameClass_Exactly1Created_29Conflict()
    {
        // 1. Arrange: Create a class with plenty of seats (e.g. 50 seats)
        var uniqueClassName = $"SingleActive-{Guid.NewGuid():N}";
        var createClassResponse = await _client.PostCreateClassAsync(uniqueClassName, 50);
        Assert.Equal(HttpStatusCode.Created, createClassResponse.StatusCode);

        var createdClass = await createClassResponse.ReadJsonAsync<ClassResponse>();
        Assert.NotNull(createdClass);
        var classId = createdClass.Id;

        // Exactly one unique 17-digit student ID
        var randomSuffix = Random.Shared.Next(1000000, 9999999);
        var singleStudentId = $"2000000000{randomSuffix:D7}"; // 17 digits

        // 2. Act: 30 parallel requests from the SAME student ID for the SAME class
        const int parallelRequests = 30;
        var tasks = Enumerable.Range(1, parallelRequests).Select(async _ =>
        {
            await Task.Yield();
            return await _client.PostApplyHoldAsync(classId, singleStudentId);
        }).ToList();

        var responses = await Task.WhenAll(tasks);

        // 3. Assert: Exactly 1 returns 201 Created, 29 return 409 Conflict
        var createdCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        var otherCount = responses.Count(r => r.StatusCode != HttpStatusCode.Created && r.StatusCode != HttpStatusCode.Conflict);

        Assert.Equal(1, createdCount);
        Assert.Equal(29, conflictCount);
        Assert.Equal(0, otherCount);

        Console.WriteLine("\n================================================================================");
        Console.WriteLine(" [CONCURRENCY TEST RESULT: SINGLE APPLICATION PER STUDENT]");
        Console.WriteLine($" Simultaneous Requests from Same ID : {parallelRequests} parallel requests");
        Console.WriteLine($" Granted Application (Hold)         : {createdCount} request (HTTP 201 Created)");
        Console.WriteLine($" Blocked Duplicate Attempts         : {conflictCount} requests (HTTP 409 Conflict)");
        Console.WriteLine(" Invariant Verified                 : PASSED (No duplicate holds for same student)");
        Console.WriteLine("================================================================================\n");

        // 4. Verify class seat breakdown
        var classGet = await _client.GetClassByIdAsync(classId);
        Assert.Equal(HttpStatusCode.OK, classGet.StatusCode);
        var summary = await classGet.ReadJsonAsync<ClassSummaryResponse>();
        Assert.NotNull(summary);

        Assert.Equal(50, summary.TotalSeats);
        Assert.Equal(1, summary.HeldSeats);
        Assert.Equal(0, summary.ConfirmedSeats);
        Assert.Equal(49, summary.AvailableSeats);
    }
}
