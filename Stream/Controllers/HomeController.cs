using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Stream.Models;
using System.Text;

namespace Stream.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost("/detect")]
        public async Task<IActionResult> Detect()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body)) return BadRequest();
            using var http = new HttpClient();
            try
            {
                var resp = await http.PostAsync("http://127.0.0.1:5001/detect", new StringContent(body, Encoding.UTF8, "application/json"));
                var txt = await resp.Content.ReadAsStringAsync();
                return Content(txt, "application/json");
            }
            catch
            {
                return Ok(new { detections = Array.Empty<object>(), gaugePct = (double?)null });
            }
        }
    }
}
