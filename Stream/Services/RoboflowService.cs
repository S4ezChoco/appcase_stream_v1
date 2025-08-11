using RestSharp;
using Newtonsoft.Json;
using System.Text;

namespace Stream.Services
{
    public class RoboflowService
    {
        private readonly RestClient _client;
        private readonly string _apiKey;
        private readonly string _projectId;

        public RoboflowService(string apiKey, string projectId = "stream-cw7hj")
        {
            _client = new RestClient("https://api.roboflow.com/");
            _apiKey = apiKey;
            _projectId = projectId;
        }

        public async Task<RoboflowProject> GetProjectInfoAsync()
        {
            var request = new RestRequest($"{_projectId}", Method.Get);
            request.AddParameter("api_key", _apiKey);

            var response = await _client.ExecuteAsync(request);
            if (response.IsSuccessful && response.Content != null)
            {
                return JsonConvert.DeserializeObject<RoboflowProject>(response.Content) ?? new RoboflowProject();
            }
            
            throw new Exception($"Failed to get project info: {response.ErrorMessage}");
        }

        public async Task<List<RoboflowVersion>> GetProjectVersionsAsync()
        {
            var request = new RestRequest($"{_projectId}", Method.Get);
            request.AddParameter("api_key", _apiKey);

            var response = await _client.ExecuteAsync(request);
            if (response.IsSuccessful && response.Content != null)
            {
                var project = JsonConvert.DeserializeObject<RoboflowProject>(response.Content);
                return project?.Versions ?? new List<RoboflowVersion>();
            }
            
            return new List<RoboflowVersion>();
        }

        public async Task<string> DownloadDatasetAsync(int version, string format = "yolov7")
        {
            var request = new RestRequest($"{_projectId}/{version}/download", Method.Get);
            request.AddParameter("api_key", _apiKey);
            request.AddParameter("format", format);

            var response = await _client.ExecuteAsync(request);
            if (response.IsSuccessful && response.Content != null)
            {
                var downloadInfo = JsonConvert.DeserializeObject<RoboflowDownload>(response.Content);
                return downloadInfo?.Export?.DownloadLink ?? "";
            }
            
            throw new Exception($"Failed to get download link: {response.ErrorMessage}");
        }

        public async Task<RoboflowInferenceResult> RunInferenceAsync(string imagePath, int version, float confidence = 0.5f)
        {
            try
            {
                // Convert image to base64
                var imageBytes = await File.ReadAllBytesAsync(imagePath);
                var base64Image = Convert.ToBase64String(imageBytes);

                var request = new RestRequest($"{_projectId}/{version}", Method.Post);
                request.AddParameter("api_key", _apiKey);
                request.AddParameter("confidence", confidence);
                request.AddParameter("overlap", 0.5);
                
                // Add the image as base64 in the body
                request.AddParameter("application/x-www-form-urlencoded", base64Image, ParameterType.RequestBody);

                var response = await _client.ExecuteAsync(request);
                if (response.IsSuccessful && response.Content != null)
                {
                    return JsonConvert.DeserializeObject<RoboflowInferenceResult>(response.Content) ?? new RoboflowInferenceResult();
                }
                
                throw new Exception($"Inference failed: {response.ErrorMessage}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error running inference: {ex.Message}");
            }
        }

        public async Task<bool> UploadImageAsync(string imagePath, string imageName, bool split = false)
        {
            try
            {
                var request = new RestRequest($"{_projectId}/upload", Method.Post);
                request.AddParameter("api_key", _apiKey);
                request.AddParameter("name", imageName);
                request.AddParameter("split", split ? "train" : "valid");

                request.AddFile("file", imagePath);

                var response = await _client.ExecuteAsync(request);
                return response.IsSuccessful;
            }
            catch
            {
                return false;
            }
        }
    }

    // Data models for Roboflow API responses
    public class RoboflowProject
    {
        [JsonProperty("project")]
        public ProjectInfo? Project { get; set; }
        
        [JsonProperty("versions")]
        public List<RoboflowVersion> Versions { get; set; } = new();
    }

    public class ProjectInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";
        
        [JsonProperty("name")]
        public string Name { get; set; } = "";
        
        [JsonProperty("created")]
        public DateTime Created { get; set; }
        
        [JsonProperty("updated")]
        public DateTime Updated { get; set; }
        
        [JsonProperty("images")]
        public int Images { get; set; }
        
        [JsonProperty("classes")]
        public List<string> Classes { get; set; } = new();
    }

    public class RoboflowVersion
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";
        
        [JsonProperty("name")]
        public string Name { get; set; } = "";
        
        [JsonProperty("version")]
        public int Version { get; set; }
        
        [JsonProperty("images")]
        public int Images { get; set; }
        
        [JsonProperty("splits")]
        public VersionSplits? Splits { get; set; }
    }

    public class VersionSplits
    {
        [JsonProperty("train")]
        public int Train { get; set; }
        
        [JsonProperty("valid")]
        public int Valid { get; set; }
        
        [JsonProperty("test")]
        public int Test { get; set; }
    }

    public class RoboflowDownload
    {
        [JsonProperty("export")]
        public ExportInfo? Export { get; set; }
    }

    public class ExportInfo
    {
        [JsonProperty("link")]
        public string DownloadLink { get; set; } = "";
    }

    public class RoboflowInferenceResult
    {
        [JsonProperty("predictions")]
        public List<RoboflowPrediction> Predictions { get; set; } = new();
        
        [JsonProperty("image")]
        public ImageInfo? Image { get; set; }
    }

    public class RoboflowPrediction
    {
        [JsonProperty("class")]
        public string Class { get; set; } = "";
        
        [JsonProperty("confidence")]
        public float Confidence { get; set; }
        
        [JsonProperty("x")]
        public float X { get; set; }
        
        [JsonProperty("y")]
        public float Y { get; set; }
        
        [JsonProperty("width")]
        public float Width { get; set; }
        
        [JsonProperty("height")]
        public float Height { get; set; }
    }

    public class ImageInfo
    {
        [JsonProperty("width")]
        public int Width { get; set; }
        
        [JsonProperty("height")]
        public int Height { get; set; }
    }
}
