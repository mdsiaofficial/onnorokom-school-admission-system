using System.Net;
using System.Text.Json;
using SchoolAdmission.Api.Contracts.Responses;
using SchoolAdmission.Tests.Fixtures;
using Xunit;

namespace SchoolAdmission.Tests;

public class ApiContractTests : IClassFixture<SchoolAdmissionApiFactory>
{
    private readonly HttpClient _client;

    public ApiContractTests(SchoolAdmissionApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Contract_Endpoint1_PostClasses_CreatesClassAndValidatesErrorFormat()
    {
        // 1. Success path: POST /classes
        var className = $"Contract-Class-{Guid.NewGuid():N}";
        var response = await _client.PostCreateClassAsync(className, 25);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Verify exact field names
        Assert.True(root.TryGetProperty("id", out var idProp));
        Assert.True(root.TryGetProperty("name", out var nameProp));
        Assert.True(root.TryGetProperty("totalSeats", out var seatsProp));

        Assert.True(Guid.TryParse(idProp.GetString(), out _));
        Assert.Equal(className, nameProp.GetString());
        Assert.Equal(25, seatsProp.GetInt32());

        // 2. Error path: 400 Bad Request with RFC 9457 ProblemDetails
        var invalidResponse = await _client.PostCreateClassAsync("", 0);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.Equal("application/problem+json", invalidResponse.Content.Headers.ContentType?.MediaType);

        var error = await invalidResponse.ReadProblemDetailsAsync();
        Assert.NotNull(error);
        Assert.Equal(400, error.Status);
        Assert.False(string.IsNullOrWhiteSpace(error.Code));
        Assert.False(string.IsNullOrWhiteSpace(error.Title));
        Assert.False(string.IsNullOrWhiteSpace(error.Detail));
        Assert.False(string.IsNullOrWhiteSpace(error.Type));
    }

    [Fact]
    public async Task Contract_Endpoint2_GetClasses_ReturnsArrayWithExactFieldNames()
    {
        var response = await _client.GetClassesAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);

        if (doc.RootElement.GetArrayLength() > 0)
        {
            var first = doc.RootElement[0];
            Assert.True(first.TryGetProperty("id", out _));
            Assert.True(first.TryGetProperty("name", out _));
            Assert.True(first.TryGetProperty("totalSeats", out _));
            Assert.True(first.TryGetProperty("heldSeats", out _));
            Assert.True(first.TryGetProperty("confirmedSeats", out _));
            Assert.True(first.TryGetProperty("availableSeats", out _));
        }
    }

    [Fact]
    public async Task Contract_Endpoint3_GetClassById_ReturnsSingleClassOr404ProblemDetails()
    {
        // 1. Create a class
        var className = $"Contract-GetById-{Guid.NewGuid():N}";
        var createResponse = await _client.PostCreateClassAsync(className, 15);
        var created = await createResponse.ReadJsonAsync<ClassResponse>();
        Assert.NotNull(created);

        // 2. Success path: GET /classes/{id}
        var response = await _client.GetClassByIdAsync(created.Id);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.ReadJsonAsync<ClassSummaryResponse>();
        Assert.NotNull(summary);
        Assert.Equal(created.Id, summary.Id);
        Assert.Equal(className, summary.Name);
        Assert.Equal(15, summary.TotalSeats);
        Assert.Equal(0, summary.HeldSeats);
        Assert.Equal(0, summary.ConfirmedSeats);
        Assert.Equal(15, summary.AvailableSeats);

        // 3. Error path: Non-existent ID -> 404 ProblemDetails CLASS_NOT_FOUND
        var missingId = Guid.NewGuid().ToString();
        var missingResponse = await _client.GetClassByIdAsync(missingId);
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
        Assert.Equal("application/problem+json", missingResponse.Content.Headers.ContentType?.MediaType);

        var error = await missingResponse.ReadProblemDetailsAsync();
        Assert.NotNull(error);
        Assert.Equal(404, error.Status);
        Assert.Equal("CLASS_NOT_FOUND", error.Code);

        // 4. Error path: Non-guid ID -> 404 ProblemDetails
        var invalidIdResponse = await _client.GetClassByIdAsync("not-a-valid-guid");
        Assert.Equal(HttpStatusCode.NotFound, invalidIdResponse.StatusCode);
    }

    [Fact]
    public async Task Contract_Endpoint4_PostClassHolds_AppliesHoldAndValidatesHeadersAndErrors()
    {
        var createResponse = await _client.PostCreateClassAsync($"Contract-Hold-{Guid.NewGuid():N}", 10);
        var createdClass = await createResponse.ReadJsonAsync<ClassResponse>();
        Assert.NotNull(createdClass);

        var studentId = $"500000000{Random.Shared.Next(10000000, 99999999)}";

        // 1. Success path: POST /classes/{id}/holds with valid X-Student-Id
        var response = await _client.PostApplyHoldAsync(createdClass.Id, studentId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("holdId", out var holdIdProp));
        Assert.True(root.TryGetProperty("classId", out var classIdProp));
        Assert.True(root.TryGetProperty("studentId", out var studentIdProp));
        Assert.True(root.TryGetProperty("status", out var statusProp));
        Assert.True(root.TryGetProperty("expiresAt", out var expiresAtProp));

        Assert.Equal(createdClass.Id, classIdProp.GetString());
        Assert.Equal(studentId, studentIdProp.GetString());
        Assert.Equal("HELD", statusProp.GetString());
        Assert.True(DateTimeOffset.TryParse(expiresAtProp.GetString(), out _));

        // 2. Error path: Missing or invalid X-Student-Id -> 400 ProblemDetails INVALID_STUDENT_ID
        var badStudentResponse = await _client.PostApplyHoldAsync(createdClass.Id, "12345"); // Not 17 digits
        Assert.Equal(HttpStatusCode.BadRequest, badStudentResponse.StatusCode);
        var badStudentError = await badStudentResponse.ReadProblemDetailsAsync();
        Assert.NotNull(badStudentError);
        Assert.Equal("INVALID_STUDENT_ID", badStudentError.Code);

        // 3. Error path: Class does not exist -> 404 ProblemDetails CLASS_NOT_FOUND
        var missingClassResponse = await _client.PostApplyHoldAsync(Guid.NewGuid().ToString(), $"501000000{Random.Shared.Next(10000000, 99999999)}");
        Assert.Equal(HttpStatusCode.NotFound, missingClassResponse.StatusCode);
        var missingClassError = await missingClassResponse.ReadProblemDetailsAsync();
        Assert.NotNull(missingClassError);
        Assert.Equal("CLASS_NOT_FOUND", missingClassError.Code);

        // 4. Error path: Duplicate active hold -> 409 ProblemDetails ALREADY_HELD
        var duplicateResponse = await _client.PostApplyHoldAsync(createdClass.Id, studentId);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        var duplicateError = await duplicateResponse.ReadProblemDetailsAsync();
        Assert.NotNull(duplicateError);
        Assert.Equal("ALREADY_HELD", duplicateError.Code);
    }

    [Fact]
    public async Task Contract_Endpoint5_GetHoldById_ReturnsHoldDetailsWithoutStudentId()
    {
        var createResponse = await _client.PostCreateClassAsync($"Contract-GetHold-{Guid.NewGuid():N}", 10);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdClass = await createResponse.ReadJsonAsync<ClassResponse>();
        Assert.NotNull(createdClass);

        var studentId = $"502000000{Random.Shared.Next(10000000, 99999999)}";
        var applyResponse = await _client.PostApplyHoldAsync(createdClass.Id, studentId);
        Assert.Equal(HttpStatusCode.Created, applyResponse.StatusCode);
        var hold = await applyResponse.ReadJsonAsync<HoldResponse>();
        Assert.NotNull(hold);

        // 1. Success path: GET /holds/{id}
        var response = await _client.GetHoldByIdAsync(hold.HoldId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("holdId", out _));
        Assert.True(root.TryGetProperty("classId", out _));
        Assert.True(root.TryGetProperty("status", out _));
        Assert.True(root.TryGetProperty("expiresAt", out _));
        // MUST NOT leak studentId per spec (§6.1)
        Assert.False(root.TryGetProperty("studentId", out _));

        // 2. Error path: Non-existent hold -> 404 ProblemDetails HOLD_NOT_FOUND
        var missingResponse = await _client.GetHoldByIdAsync(Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
        var missingError = await missingResponse.ReadProblemDetailsAsync();
        Assert.NotNull(missingError);
        Assert.Equal("HOLD_NOT_FOUND", missingError.Code);
    }

    [Fact]
    public async Task Contract_Endpoint6_PostHoldConfirm_ValidatesOwnershipAndIdempotency()
    {
        var createResponse = await _client.PostCreateClassAsync($"Contract-Confirm-{Guid.NewGuid():N}", 10);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdClass = await createResponse.ReadJsonAsync<ClassResponse>();
        Assert.NotNull(createdClass);

        var ownerStudentId = $"503000000{Random.Shared.Next(10000000, 99999999)}";
        var wrongStudentId = $"504000000{Random.Shared.Next(10000000, 99999999)}";

        var applyResponse = await _client.PostApplyHoldAsync(createdClass.Id, ownerStudentId);
        Assert.Equal(HttpStatusCode.Created, applyResponse.StatusCode);
        var hold = await applyResponse.ReadJsonAsync<HoldResponse>();
        Assert.NotNull(hold);

        // 1. Error path: Non-owner -> 403 Forbidden NOT_HOLD_OWNER
        var forbiddenResponse = await _client.PostConfirmHoldAsync(hold.HoldId, wrongStudentId);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
        var forbiddenError = await forbiddenResponse.ReadProblemDetailsAsync();
        Assert.NotNull(forbiddenError);
        Assert.Equal("NOT_HOLD_OWNER", forbiddenError.Code);

        // 2. Success path: Owner confirms -> 200 OK CONFIRMED
        var confirmResponse = await _client.PostConfirmHoldAsync(hold.HoldId, ownerStudentId);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var confirmResult = await confirmResponse.ReadJsonAsync<ConfirmHoldResponse>();
        Assert.NotNull(confirmResult);
        Assert.Equal(hold.HoldId, confirmResult.HoldId);
        Assert.Equal("CONFIRMED", confirmResult.Status);

        // 3. Success path: Repeat confirm (idempotent) -> 200 OK CONFIRMED
        var repeatConfirmResponse = await _client.PostConfirmHoldAsync(hold.HoldId, ownerStudentId);
        Assert.Equal(HttpStatusCode.OK, repeatConfirmResponse.StatusCode);

        // 4. Error path: Non-existent hold -> 404 ProblemDetails HOLD_NOT_FOUND
        var missingConfirmResponse = await _client.PostConfirmHoldAsync(Guid.NewGuid().ToString(), ownerStudentId);
        Assert.Equal(HttpStatusCode.NotFound, missingConfirmResponse.StatusCode);
        var missingError = await missingConfirmResponse.ReadProblemDetailsAsync();
        Assert.NotNull(missingError);
        Assert.Equal("HOLD_NOT_FOUND", missingError.Code);
    }

    [Fact]
    public async Task Contract_Endpoint7_DeleteHold_CancelsHoldAndVerifiesIdempotency()
    {
        var createResponse = await _client.PostCreateClassAsync($"Contract-Delete-{Guid.NewGuid():N}", 10);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdClass = await createResponse.ReadJsonAsync<ClassResponse>();
        Assert.NotNull(createdClass);

        var ownerStudentId = $"505000000{Random.Shared.Next(10000000, 99999999)}";
        var wrongStudentId = $"506000000{Random.Shared.Next(10000000, 99999999)}";

        var applyResponse = await _client.PostApplyHoldAsync(createdClass.Id, ownerStudentId);
        Assert.Equal(HttpStatusCode.Created, applyResponse.StatusCode);
        var hold = await applyResponse.ReadJsonAsync<HoldResponse>();
        Assert.NotNull(hold);

        // 1. Error path: Non-owner -> 403 Forbidden NOT_HOLD_OWNER
        var forbiddenResponse = await _client.DeleteHoldAsync(hold.HoldId, wrongStudentId);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
        var forbiddenError = await forbiddenResponse.ReadProblemDetailsAsync();
        Assert.NotNull(forbiddenError);
        Assert.Equal("NOT_HOLD_OWNER", forbiddenError.Code);

        // 2. Success path: Owner cancels -> 204 NoContent
        var deleteResponse = await _client.DeleteHoldAsync(hold.HoldId, ownerStudentId);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 3. Success path: Repeat delete (idempotent) -> 204 NoContent
        var repeatDeleteResponse = await _client.DeleteHoldAsync(hold.HoldId, ownerStudentId);
        Assert.Equal(HttpStatusCode.NoContent, repeatDeleteResponse.StatusCode);

        // 4. Verify hold status is CANCELLED
        var holdCheck = await _client.GetHoldByIdAsync(hold.HoldId);
        Assert.Equal(HttpStatusCode.OK, holdCheck.StatusCode);
        var holdStatus = await holdCheck.ReadJsonAsync<HoldStatusResponse>();
        Assert.NotNull(holdStatus);
        Assert.Equal("CANCELLED", holdStatus.Status);

        // 5. Error path: Attempting to cancel a CONFIRMED hold -> 409 HOLD_ALREADY_CONFIRMED
        var confirmClassResponse = await _client.PostCreateClassAsync($"Contract-DelConfirmed-{Guid.NewGuid():N}", 10);
        Assert.Equal(HttpStatusCode.Created, confirmClassResponse.StatusCode);
        var confirmClass = await confirmClassResponse.ReadJsonAsync<ClassResponse>();
        Assert.NotNull(confirmClass);
        var confirmedStudent = $"507000000{Random.Shared.Next(10000000, 99999999)}";

        var holdToConfirmResp = await _client.PostApplyHoldAsync(confirmClass.Id, confirmedStudent);
        Assert.Equal(HttpStatusCode.Created, holdToConfirmResp.StatusCode);
        var holdToConfirm = await holdToConfirmResp.ReadJsonAsync<HoldResponse>();
        Assert.NotNull(holdToConfirm);

        await _client.PostConfirmHoldAsync(holdToConfirm.HoldId, confirmedStudent);

        var cancelConfirmedResp = await _client.DeleteHoldAsync(holdToConfirm.HoldId, confirmedStudent);
        Assert.Equal(HttpStatusCode.Conflict, cancelConfirmedResp.StatusCode);
        var cancelConfirmedError = await cancelConfirmedResp.ReadProblemDetailsAsync();
        Assert.NotNull(cancelConfirmedError);
        Assert.Equal("HOLD_ALREADY_CONFIRMED", cancelConfirmedError.Code);
    }

    [Fact]
    public async Task Contract_Endpoint8_GetMyHolds_ReturnsStudentApplicationsList()
    {
        var className1 = $"Contract-MyHolds-1-{Guid.NewGuid():N}";
        var className2 = $"Contract-MyHolds-2-{Guid.NewGuid():N}";
        var class1Resp = await _client.PostCreateClassAsync(className1, 10);
        var class2Resp = await _client.PostCreateClassAsync(className2, 10);
        var class1 = await class1Resp.ReadJsonAsync<ClassResponse>();
        var class2 = await class2Resp.ReadJsonAsync<ClassResponse>();
        Assert.NotNull(class1);
        Assert.NotNull(class2);

        var studentId = $"508000000{Random.Shared.Next(10000000, 99999999)}";

        // Apply for class1
        await _client.PostApplyHoldAsync(class1.Id, studentId);

        // Apply for class2
        await _client.PostApplyHoldAsync(class2.Id, studentId);

        // 1. Success path: GET /me/holds with X-Student-Id
        var response = await _client.GetMyHoldsAsync(studentId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var myHolds = await response.ReadJsonAsync<List<StudentHoldResponse>>();
        Assert.NotNull(myHolds);
        Assert.True(myHolds.Count >= 2);

        var myHold1 = myHolds.FirstOrDefault(h => h.ClassId == class1.Id);
        Assert.NotNull(myHold1);
        Assert.Equal(className1, myHold1.ClassName);
        Assert.Equal("HELD", myHold1.Status);

        var myHold2 = myHolds.FirstOrDefault(h => h.ClassId == class2.Id);
        Assert.NotNull(myHold2);
        Assert.Equal(className2, myHold2.ClassName);
        Assert.Equal("HELD", myHold2.Status);

        // 2. Error path: Missing or bad X-Student-Id -> 400 ProblemDetails INVALID_STUDENT_ID
        var badResponse = await _client.GetMyHoldsAsync("short-id");
        Assert.Equal(HttpStatusCode.BadRequest, badResponse.StatusCode);
        var error = await badResponse.ReadProblemDetailsAsync();
        Assert.NotNull(error);
        Assert.Equal("INVALID_STUDENT_ID", error.Code);
    }
}
