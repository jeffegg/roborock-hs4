using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HSPI_RoborockBridge.Bridge {
    internal sealed class RoborockBridgeClient {
        private readonly string _bridgeUrl;
        private readonly string _apiKey;

        public RoborockBridgeClient(string bridgeUrl, string apiKey) {
            _bridgeUrl = bridgeUrl ?? throw new ArgumentNullException(nameof(bridgeUrl));
            _apiKey = apiKey ?? string.Empty;
        }

        public async Task<string> SendCommandAsync(string deviceId, string action) {
            if (string.IsNullOrWhiteSpace(deviceId)) {
                throw new ArgumentException("Device ID is required", nameof(deviceId));
            }

            if (string.IsNullOrWhiteSpace(action)) {
                throw new ArgumentException("Action is required", nameof(action));
            }

            using (var client = new HttpClient()) {
                var endpoint = $"{_bridgeUrl}/command/{Uri.EscapeDataString(deviceId)}";
                var payload = JsonConvert.SerializeObject(new { action = action });
                using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint)) {
                    request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                    if (!string.IsNullOrWhiteSpace(_apiKey)) {
                        request.Headers.Add("x-api-key", _apiKey);
                    }

                    using (HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false)) {
                        string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!response.IsSuccessStatusCode) {
                            throw new InvalidOperationException($"Bridge HTTP {(int)response.StatusCode}: {responseBody}");
                        }

                        return responseBody;
                    }
                }
            }
        }
    }
}
