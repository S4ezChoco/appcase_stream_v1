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

        // Serve river highlights GeoJSON
        [HttpGet("/api/river-highlights")]
        public async Task<IActionResult> GetRiverHighlights()
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "river_stream_highlights", "export.geojson");
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("River highlights data not found");
                }

                var geoJsonContent = await System.IO.File.ReadAllTextAsync(filePath);
                return Content(geoJsonContent, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Get river data from Overpass API for Naga City
        [HttpGet("/api/river-highlights/overpass")]
        public async Task<IActionResult> GetRiverHighlightsFromOverpass()
        {
            try
            {
                using var httpClient = new HttpClient();

                // Your specific Overpass API query for Naga City
                var overpassQuery = @"
[out:json][timeout:25];
area[name=""Naga City""]->.searchArea;
(
  way[""waterway""~""river|stream""](area.searchArea);
  relation[""waterway""~""river|stream""](area.searchArea);
);
out geom;";

                var content = new StringContent(overpassQuery, Encoding.UTF8, "text/plain");
                var response = await httpClient.PostAsync("https://overpass-api.de/api/interpreter", content);

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("Failed to fetch data from Overpass API");
                }

                var overpassData = await response.Content.ReadAsStringAsync();

                // Convert Overpass JSON to GeoJSON
                var geoJson = ConvertOverpassToGeoJson(overpassData);

                return Content(geoJson, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private static string ConvertOverpassToGeoJson(string overpassJson)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(overpassJson);
                var root = doc.RootElement;

                if (!root.TryGetProperty("elements", out var elements))
                {
                    return @"{""type"":""FeatureCollection"",""features"":[]}";
                }

                var features = new List<object>();

                foreach (var element in elements.EnumerateArray())
                {
                    if (element.TryGetProperty("type", out var typeProperty) &&
                        typeProperty.GetString() == "way" &&
                        element.TryGetProperty("geometry", out var geometry))
                    {
                        var coordinates = new List<List<double>>();

                        foreach (var coord in geometry.EnumerateArray())
                        {
                            if (coord.TryGetProperty("lat", out var lat) &&
                                coord.TryGetProperty("lon", out var lon))
                            {
                                coordinates.Add(new List<double> { lon.GetDouble(), lat.GetDouble() });
                            }
                        }

                        if (coordinates.Count > 0)
                        {
                            var properties = new Dictionary<string, object>();

                            // Add tags as properties
                            if (element.TryGetProperty("tags", out var tags))
                            {
                                foreach (var tag in tags.EnumerateObject())
                                {
                                    properties[tag.Name] = tag.Value.GetString() ?? "";
                                }
                            }

                            var feature = new
                            {
                                type = "Feature",
                                geometry = new
                                {
                                    type = "LineString",
                                    coordinates = coordinates
                                },
                                properties = properties
                            };

                            features.Add(feature);
                        }
                    }
                }

                var geoJson = new
                {
                    type = "FeatureCollection",
                    features = features
                };

                return System.Text.Json.JsonSerializer.Serialize(geoJson, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = false
                });
            }
            catch (Exception)
            {
                // Fallback to empty GeoJSON on error
                return @"{""type"":""FeatureCollection"",""features"":[]}";
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

        // Get Naga City geofence from static file
        [HttpGet]
        [Route("api/geofence")]
        public async Task<IActionResult> GetGeofence()
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "naga_geofence", "naga_geofence.geojson");
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("Geofence data not found");
                }

                var geoJsonContent = await System.IO.File.ReadAllTextAsync(filePath);
                return Content(geoJsonContent, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Get Naga City geofence from Overpass API
        [HttpGet]
        [Route("api/geofence/overpass")]
        public async Task<IActionResult> GetGeofenceFromOverpass()
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var overpassQuery = @"[out:json][timeout:25];
area[name=""Naga City""]->.searchArea;
relation[""boundary""=""administrative""][""name""=""Naga City""](area.searchArea);
out geom;";

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("data", overpassQuery)
                });

                var response = await httpClient.PostAsync("https://overpass-api.de/api/interpreter", content);
                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("Failed to fetch data from Overpass API");
                }

                var overpassJson = await response.Content.ReadAsStringAsync();
                var geoJson = ConvertOverpassToGeoJson(overpassJson);

                return Content(geoJson, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
