using System.Net.Http.Json;
using System.Text.Json;
using SchoolAdmission.Api.Contracts.Requests;
using SchoolAdmission.Api.Contracts.Responses;

namespace SchoolAdmission.Tests.Fixtures;

public record ProblemDetailsResponse(
    string? Type,
    string? Title,
    int? Status,
    string? Code,
    string? Detail);

public static class TestClientHelper
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<HttpResponseMessage> PostCreateClassAsync(this HttpClient client, string name, int totalSeats)
    {
        return await client.PostAsJsonAsync("/classes", new CreateClassRequest(name, totalSeats), JsonOptions);
    }

    public static async Task<HttpResponseMessage> PutUpdateClassAsync(this HttpClient client, string classId, string name, int totalSeats)
    {
        return await client.PutAsJsonAsync($"/classes/{classId}", new UpdateClassRequest(name, totalSeats), JsonOptions);
    }

    public static async Task<HttpResponseMessage> DeleteClassAsync(this HttpClient client, string classId)
    {
        return await client.DeleteAsync($"/classes/{classId}");
    }

    public static async Task<HttpResponseMessage> GetClassesAsync(this HttpClient client)
    {
        return await client.GetAsync("/classes");
    }

    public static async Task<HttpResponseMessage> GetClassByIdAsync(this HttpClient client, string classId)
    {
        return await client.GetAsync($"/classes/{classId}");
    }

    public static async Task<HttpResponseMessage> PostApplyHoldAsync(this HttpClient client, string classId, string studentId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
        if (!string.IsNullOrEmpty(studentId))
        {
            request.Headers.Add("X-Student-Id", studentId);
        }
        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> GetHoldByIdAsync(this HttpClient client, string holdId)
    {
        return await client.GetAsync($"/holds/{holdId}");
    }

    public static async Task<HttpResponseMessage> PostConfirmHoldAsync(this HttpClient client, string holdId, string studentId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdId}/confirm");
        if (!string.IsNullOrEmpty(studentId))
        {
            request.Headers.Add("X-Student-Id", studentId);
        }
        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> DeleteHoldAsync(this HttpClient client, string holdId, string studentId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/holds/{holdId}");
        if (!string.IsNullOrEmpty(studentId))
        {
            request.Headers.Add("X-Student-Id", studentId);
        }
        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> GetMyHoldsAsync(this HttpClient client, string studentId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/me/holds");
        if (!string.IsNullOrEmpty(studentId))
        {
            request.Headers.Add("X-Student-Id", studentId);
        }
        return await client.SendAsync(request);
    }

    public static async Task<T?> ReadJsonAsync<T>(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    public static async Task<ProblemDetailsResponse?> ReadProblemDetailsAsync(this HttpResponseMessage response)
    {
        return await response.ReadJsonAsync<ProblemDetailsResponse>();
    }
}
