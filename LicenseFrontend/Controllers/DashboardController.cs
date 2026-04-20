using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class DashboardController : Controller
{
    private readonly HttpClient _httpClient;

    public DashboardController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [Authorize(Roles = "Applicant")]
    public async Task<IActionResult> Applicant()
    {
        var token = Request.Cookies["jwt"];
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        // Fetch licenses for the applicant
        var response = await _httpClient.GetAsync("http://localhost:5001/api/Licenses");
        var content = await response.Content.ReadAsStringAsync();
        
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var licenses = JsonSerializer.Deserialize<List<LicenseFrontend.Models.LicenseViewModel>>(content, options) ?? new();
        
        // Fetch Notifications
        ViewBag.Notifications = new List<LicenseFrontend.Models.NotificationViewModel>();
        try {
            var notifyResp = await _httpClient.GetAsync("http://localhost:5003/api/Notifications");
            if (notifyResp.IsSuccessStatusCode)
            {
                var notifyContent = await notifyResp.Content.ReadAsStringAsync();
                ViewBag.Notifications = JsonSerializer.Deserialize<List<LicenseFrontend.Models.NotificationViewModel>>(notifyContent, options) ?? new();
            }
        } catch { /* Handle service down */ }

        return View(licenses);
    }

    [Authorize(Roles = "Agency,Admin")]
    public async Task<IActionResult> Agency()
    {
        var token = Request.Cookies["jwt"];
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        // Fetch licenses for the agency/admin
        var response = await _httpClient.GetAsync("http://localhost:5001/api/Licenses");
        var content = await response.Content.ReadAsStringAsync();
        
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var licenses = JsonSerializer.Deserialize<List<LicenseFrontend.Models.LicenseViewModel>>(content, options) ?? new();

        // Fetch Notifications
        ViewBag.Notifications = new List<LicenseFrontend.Models.NotificationViewModel>();
        try {
            var notifyResp = await _httpClient.GetAsync("http://localhost:5003/api/Notifications");
            if (notifyResp.IsSuccessStatusCode)
            {
                var notifyContent = await notifyResp.Content.ReadAsStringAsync();
                ViewBag.Notifications = JsonSerializer.Deserialize<List<LicenseFrontend.Models.NotificationViewModel>>(notifyContent, options) ?? new();
            }
        } catch { /* Handle service down */ }
        
        return View(licenses);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Admin() => View();
}