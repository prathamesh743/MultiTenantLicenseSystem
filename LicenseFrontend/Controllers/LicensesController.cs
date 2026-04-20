using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseFrontend.Controllers;

[Authorize]
public class LicensesController : Controller
{
    private readonly IHttpClientFactory _factory;

    public LicensesController(IHttpClientFactory httpClientFactory)
    {
        _factory = httpClientFactory;
    }

    // Creates an authenticated HttpClient using the JWT from cookies
    private HttpClient AuthClient()
    {
        var token = Request.Cookies["jwt"] ?? "";
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [HttpPost]
    public async Task<IActionResult> Apply(string applicantName, string agency, IFormFile document)
    {
        var token = Request.Cookies["jwt"];
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");

        var steps = new List<string>();

        // ── Step 1: Upload document ──────────────────────────────────────────
        int? documentId = null;
        if (document != null && document.Length > 0)
        {
            using var docClient = AuthClient();
            using var ms = new MemoryStream();
            await document.CopyToAsync(ms);
            var fileBytes = ms.ToArray();

            using var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(fileBytes), "file", document.FileName);

            var docResp = await docClient.PostAsync("http://localhost:5002/api/Documents/upload", form);
            var docBody = await docResp.Content.ReadAsStringAsync();
            steps.Add($"DOC={docResp.StatusCode}");

            if (docResp.IsSuccessStatusCode)
            {
                var docJson = JsonDocument.Parse(docBody);
                documentId = docJson.RootElement.GetProperty("documentId").GetInt32();
            }
            else
            {
                steps.Add($"DOC_ERR={docBody}");
            }
        }
        else
        {
            steps.Add("DOC=SKIPPED");
        }

        // ── Step 2: Apply for license ────────────────────────────────────────
        using var licClient = AuthClient();
        var docFileName = document?.FileName; // Capture original filename
        var applyBody = new { ApplicantName = applicantName, Agency = agency, DocumentId = documentId, DocumentFileName = docFileName };
        var applyJson = new StringContent(JsonSerializer.Serialize(applyBody), Encoding.UTF8, "application/json");
        var licResp = await licClient.PostAsync("http://localhost:5001/api/Licenses", applyJson);
        var licBody = await licResp.Content.ReadAsStringAsync();
        steps.Add($"LIC={licResp.StatusCode}");

        if (!licResp.IsSuccessStatusCode)
        {
            TempData["ErrorMessage"] = $"Application failed. Steps: {string.Join(" | ", steps)} | Body: {licBody}";
            return RedirectToAction("Applicant", "Dashboard");
        }

        var licJson = JsonDocument.Parse(licBody);
        var licenseId = licJson.RootElement.GetProperty("id").GetInt32();
        var licenseNum = licJson.RootElement.GetProperty("licenseNumber").GetString();

        // ── Step 3: Payment ──────────────────────────────────────────────────
        using var payClient = AuthClient();
        var payBody = new { LicenseId = licenseId, Amount = 150.00m };
        var payJson = new StringContent(JsonSerializer.Serialize(payBody), Encoding.UTF8, "application/json");
        var payResp = await payClient.PostAsync("http://localhost:5004/api/Payments/pay", payJson);
        steps.Add($"PAY={payResp.StatusCode}");

        // ── Step 4: Notification ─────────────────────────────────────────────
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0";
        using var notifyClient = AuthClient();
        var notifyBody = new { UserId = int.Parse(userIdStr), Message = $"Application {licenseNum} for {agency} submitted & paid." };
        var notifyJson = new StringContent(JsonSerializer.Serialize(notifyBody), Encoding.UTF8, "application/json");
        var notifyResp = await notifyClient.PostAsync("http://localhost:5003/api/Notifications", notifyJson);
        var notifyBody2 = await notifyResp.Content.ReadAsStringAsync();
        steps.Add($"NOTIFY={notifyResp.StatusCode}");

        // Provide consolidated feedback to the user about the multi-step transaction.
        TempData["SuccessMessage"] = $"Application {licenseNum} submitted! Steps: {string.Join(" | ", steps)}";
        return RedirectToAction("Applicant", "Dashboard");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        using var client = AuthClient();
        var content = new StringContent(JsonSerializer.Serialize(status), Encoding.UTF8, "application/json");
        var response = await client.PutAsync($"http://localhost:5001/api/Licenses/{id}/status", content);

        if (response.IsSuccessStatusCode)
            TempData["SuccessMessage"] = $"License {status} successfully.";
        else
        {
            var err = await response.Content.ReadAsStringAsync();
            TempData["ErrorMessage"] = $"Failed to update: {response.StatusCode} - {err}";
        }

        return RedirectToAction("Agency", "Dashboard");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        using var client = AuthClient();
        var response = await client.GetAsync($"http://localhost:5002/api/Documents/{id}");
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsByteArrayAsync();
            var fileName = response.Content.Headers.ContentDisposition?.FileName ?? "document.pdf";
            return File(content, "application/octet-stream", fileName);
        }
        TempData["ErrorMessage"] = $"Download failed: {response.StatusCode}";
        return RedirectToAction("Applicant", "Dashboard");
    }
}
