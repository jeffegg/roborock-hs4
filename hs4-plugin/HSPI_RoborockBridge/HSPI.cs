using System;
using HomeSeer.Jui.Views;
using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Logging;
using HSPI_RoborockBridge.Actions;
using HSPI_RoborockBridge.Bridge;

namespace HSPI_RoborockBridge {
    // ReSharper disable once InconsistentNaming
    public class HSPI : AbstractPlugin, RoborockCommandAction.IRoborockActionHandler {
        private const string SettingsPageId = "rb_settings";
        private const string BridgeUrlViewId = "rb_bridge_url";
        private const string ApiKeyViewId = "rb_api_key";
        private const string DefaultDeviceIdViewId = "rb_default_device_id";

        public override string Id { get; } = "RoborockBridge";
        public override string Name { get; } = "Roborock Bridge";
        protected override string SettingsFileName { get; } = "RoborockBridge.ini";

        public HSPI() {
            LogDebug = true;
            InitializeSettingsPages();
            ActionTypes.AddActionType(typeof(RoborockCommandAction));
        }

        protected override void Initialize() {
            LoadSettingsFromIni();
            Status = PluginStatus.Ok();
            HomeSeerSystem.WriteLog(ELogType.Info, "Plugin initialized", Name);
        }

        protected override bool OnSettingChange(string pageId, AbstractView currentView, AbstractView changedView) {
            return true;
        }

        protected override void BeforeReturnStatus() {
            if (string.IsNullOrWhiteSpace(GetTextSetting(BridgeUrlViewId, string.Empty))) {
                Status = PluginStatus.Warn("Bridge URL is not configured.");
                return;
            }

            Status = PluginStatus.Ok();
        }

        public override string PostBackProc(string page, string data, string user, int userRights) {
            return string.Empty;
        }

        public bool ExecuteBridgeCommand(string action, string requestedDeviceId) {
            string bridgeUrl = GetTextSetting(BridgeUrlViewId, "http://127.0.0.1:8129").TrimEnd('/');
            string apiKey = GetTextSetting(ApiKeyViewId, string.Empty);
            string configuredDeviceId = GetTextSetting(DefaultDeviceIdViewId, string.Empty);
            string deviceId = string.IsNullOrWhiteSpace(requestedDeviceId) ? configuredDeviceId : requestedDeviceId;

            if (string.IsNullOrWhiteSpace(deviceId)) {
                HomeSeerSystem.WriteLog(ELogType.Error, "No device ID supplied for Roborock command.", Name);
                return false;
            }

            try {
                var client = new RoborockBridgeClient(bridgeUrl, apiKey);
                string response = client.SendCommandAsync(deviceId, action).GetAwaiter().GetResult();
                HomeSeerSystem.WriteLog(ELogType.Info, $"Sent '{action}' to device '{deviceId}'. Bridge response: {response}", Name);
                return true;
            }
            catch (Exception ex) {
                HomeSeerSystem.WriteLog(ELogType.Error, $"Failed to send Roborock command: {ex.Message}", Name);
                return false;
            }
        }

        private void InitializeSettingsPages() {
            var page = PageFactory.CreateSettingsPage(SettingsPageId, "Roborock Bridge");
            page.WithLabel("rb_settings_help", "Bridge API endpoint and defaults for event actions.");
            page.WithInput(BridgeUrlViewId, "Bridge URL", "http://127.0.0.1:8129");
            page.WithInput(ApiKeyViewId, "Bridge API key");
            page.WithInput(DefaultDeviceIdViewId, "Default Roborock device ID");
            Settings.Add(page.Page);
        }

        private string GetTextSetting(string viewId, string fallback) {
            try {
                var page = Settings[SettingsPageId];
                var view = page.GetViewById<InputView>(viewId);
                return string.IsNullOrWhiteSpace(view.Value) ? fallback : view.Value;
            }
            catch {
                return fallback;
            }
        }
    }
}
