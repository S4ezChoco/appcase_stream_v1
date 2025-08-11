using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Stream.Models;
using Stream.Services;
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

        // Roboflow API Endpoints
        [HttpGet("/api/roboflow/project")]
        public async Task<IActionResult> GetProjectInfo()
        {
            try
            {
                var apiKey = GetApiKeyFromRequest();
                if (string.IsNullOrEmpty(apiKey))
                    return Unauthorized("API key required");

                var roboflowService = new RoboflowService(apiKey, "stream-cw7hj");
                var projectInfo = await roboflowService.GetProjectInfoAsync();
                return Ok(projectInfo);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("/api/roboflow/versions")]
        public async Task<IActionResult> GetProjectVersions()
        {
            try
            {
                var apiKey = GetApiKeyFromRequest();
                if (string.IsNullOrEmpty(apiKey))
                    return Unauthorized("API key required");

                var roboflowService = new RoboflowService(apiKey, "stream-cw7hj");
                var versions = await roboflowService.GetProjectVersionsAsync();
                return Ok(versions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("/api/roboflow/download/{version}")]
        public async Task<IActionResult> DownloadDataset(int version, [FromQuery] string format = "yolov7")
        {
            try
            {
                var apiKey = GetApiKeyFromRequest();
                if (string.IsNullOrEmpty(apiKey))
                    return Unauthorized("API key required");

                var roboflowService = new RoboflowService(apiKey, "stream-cw7hj");
                var downloadLink = await roboflowService.DownloadDatasetAsync(version, format);
                return Ok(new { downloadLink });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("/api/roboflow/inference/{version}")]
        public async Task<IActionResult> RunInference(int version, IFormFile image, [FromQuery] float confidence = 0.5f)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No image provided");

            try
            {
                var apiKey = GetApiKeyFromRequest();
                if (string.IsNullOrEmpty(apiKey))
                    return Unauthorized("API key required");

                // Save uploaded image temporarily
                var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                using (var stream = System.IO.File.Create(tempPath))
                {
                    await image.CopyToAsync(stream);
                }

                var roboflowService = new RoboflowService(apiKey, "stream-cw7hj");
                var result = await roboflowService.RunInferenceAsync(tempPath, version, confidence);
                
                // Clean up temp file
                System.IO.File.Delete(tempPath);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("/api/roboflow/upload")]
        public async Task<IActionResult> UploadImage(IFormFile image, [FromQuery] string name, [FromQuery] bool split = false)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No image provided");

            try
            {
                var apiKey = GetApiKeyFromRequest();
                if (string.IsNullOrEmpty(apiKey))
                    return Unauthorized("API key required");

                // Save uploaded image temporarily
                var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                using (var stream = System.IO.File.Create(tempPath))
                {
                    await image.CopyToAsync(stream);
                }

                var roboflowService = new RoboflowService(apiKey, "stream-cw7hj");
                var success = await roboflowService.UploadImageAsync(tempPath, name, split);
                
                // Clean up temp file
                System.IO.File.Delete(tempPath);
                
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private string GetApiKeyFromRequest()
        {
            // Try to get API key from Authorization header
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                return authHeader.Substring(7); // Remove "Bearer " prefix
            }
            
            // Fallback to query parameter
            return Request.Query["api_key"].FirstOrDefault() ?? "";
        }
    }
}
