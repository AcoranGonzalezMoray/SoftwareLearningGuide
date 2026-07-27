using System.Text.Json;

namespace SoftwareLearningGuide.Api.FeatureToggles {
    public class FeatureManagementSourceConfiguration : IConfigurationSource {
        private readonly string _apiUrl;
        private readonly string _apiKey;
        private readonly int _reloadIntervalSeconds;

        public FeatureManagementSourceConfiguration(string apiUrl, string apiKey, int reloadIntervalSeconds) {
            _apiUrl = apiUrl;
            _apiKey = apiKey;
            _reloadIntervalSeconds = reloadIntervalSeconds;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder) {
            return new FeatureManagementConfigurationProvider(_apiUrl, _apiKey, _reloadIntervalSeconds);
        }
    }

    public class FeatureManagementConfigurationProvider : ConfigurationProvider, IDisposable {
        private readonly string _apiUrl;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient = new();
        private Timer? _timer;
        private readonly int _reloadIntervalSeconds;

        public FeatureManagementConfigurationProvider(string apiUrl, string apiKey, int reloadIntervalSeconds) {
            _apiUrl = apiUrl;
            _apiKey = apiKey;
            _reloadIntervalSeconds = reloadIntervalSeconds;
        }

        public override void Load() {
            FetchFlags();

            if (_reloadIntervalSeconds > 0) {
                _timer?.Dispose();
                _timer = new Timer(
                    callback: _ => { FetchFlags(); OnReload(); },
                    state: null,
                    dueTime: TimeSpan.FromSeconds(_reloadIntervalSeconds),
                    period: TimeSpan.FromSeconds(_reloadIntervalSeconds));
            }
        }

        private void FetchFlags() {
            try {
                using var request = new HttpRequestMessage(HttpMethod.Get, _apiUrl);
                request.Headers.Add("X-Environment-Key", _apiKey);
                var response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode) return;

                var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                using var doc = JsonDocument.Parse(json);

                var newData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

                foreach (var element in doc.RootElement.EnumerateArray()) {
                    var featureName = element.GetProperty("feature").GetProperty("name").GetString()?.ToUpperInvariant();
                    var enabled = element.GetProperty("enabled").GetBoolean().ToString();

                    newData[$"FeatureManagement:{featureName}"] = enabled;
                }

                Data.Clear();
                foreach (var kvp in newData) {
                    Data[kvp.Key] = kvp.Value;
                }
            }
            catch {
            }
        }

        public void Dispose() {
            _timer?.Dispose();
            _httpClient.Dispose();
        }
    }
}
