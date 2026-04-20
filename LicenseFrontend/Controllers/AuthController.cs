using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace LicenseFrontend.Controllers;

public class AuthController : Controller
{
    private readonly HttpClient _httpClient;

    public AuthController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, string tenantId)
    {
        var requestBody = new { Username = username, Password = password, TenantId = tenantId };
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("http://localhost:5000/license/api/Auth/login", content);

        if (response.IsSuccessStatusCode)
        {
            var responseData = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(responseData);
            var token = json.RootElement.GetProperty("token").GetString();

            if (!string.IsNullOrEmpty(token))
            {
                // Store JWT in a cookie for subsequent authenticated requests.
                Response.Cookies.Append("jwt", token, new CookieOptions
                {
                    HttpOnly = true, // Enhanced security: prevents client-side script access to the token.
                    Secure = false,  // Set to true in production with HTTPS.
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddHours(2)
                });

                if (json.RootElement.GetProperty("role").GetString() == "Applicant")
                    return RedirectToAction("Applicant", "Dashboard");
                else
                    return RedirectToAction("Agency", "Dashboard");
            }
        }

        ViewBag.Error = "Invalid credentials or backend error.";
        return View();
    }
    
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string username, string password, string role, string tenantId)
    {
        var requestBody = new { Username = username, Password = password, Role = role, TenantId = tenantId };
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("http://localhost:5000/license/api/Auth/register", content);

        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] = "Registration successful. You can now login.";
            return RedirectToAction("Login");
        }

        var errorMessage = await response.Content.ReadAsStringAsync();
        ViewBag.Error = $"Registration failed: {errorMessage}";
        return View();
    }

    [HttpGet]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("jwt");
        return RedirectToAction("Index", "Home");
    }
}
