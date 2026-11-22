using System;
using System.Collections.Generic;
using HomeSeer.Jui.Views;
using HomeSeer.PluginSdk.Events;

namespace HSPI_RoborockBridge.Actions {
    public class RoborockCommandAction : AbstractActionType {
        public interface IRoborockActionHandler : ActionTypeCollection.IActionTypeListener {
            bool ExecuteBridgeCommand(string action, string requestedDeviceId);
        }

        private const string DeviceIdInputIdSuffix = "_device_id";
        private const string CommandSelectIdSuffix = "_command";

        private static readonly List<string> CommandOptions = new List<string> {
            "start",
            "pause",
            "stop",
            "dock"
        };

        protected RoborockCommandAction(int id, int eventRef, byte[] dataIn, ActionTypeCollection.IActionTypeListener listener)
            : base(id, eventRef, dataIn, listener) {
        }

        protected RoborockCommandAction() {
        }

        protected override string GetName() => "Roborock command";

        protected override void OnNewAction() {
            var page = PageFactory.CreateEventActionPage(PageId, "Roborock command");
            page.WithDropDownSelectList(GetCommandSelectId(), "Action", CommandOptions, 0);
            page.WithInput(GetDeviceIdInputId(), "Device ID override (optional)");
            ConfigPage = page.Page;
        }

        public override bool IsFullyConfigured() {
            try {
                var select = ConfigPage.GetViewById<SelectListView>(GetCommandSelectId());
                return select.Selection >= 0;
            }
            catch {
                return false;
            }
        }

        protected override bool OnConfigItemUpdate(AbstractView configViewChange) {
            ConfigPage.UpdateViewById(configViewChange);
            return true;
        }

        public override string GetPrettyString() {
            string action = GetAction();
            string deviceId = GetDeviceId();
            if (string.IsNullOrWhiteSpace(deviceId)) {
                return $"Send Roborock action '{action}' to default configured device";
            }

            return $"Send Roborock action '{action}' to device '{deviceId}'";
        }

        public override bool OnRunAction() {
            var listener = ActionListener as IRoborockActionHandler;
            if (listener == null) {
                return false;
            }

            return listener.ExecuteBridgeCommand(GetAction(), GetDeviceId());
        }

        public override bool ReferencesDeviceOrFeature(int devOrFeatRef) {
            return false;
        }

        private string GetAction() {
            var select = ConfigPage.GetViewById<SelectListView>(GetCommandSelectId());
            return select.GetSelectedOption();
        }

        private string GetDeviceId() {
            var input = ConfigPage.GetViewById<InputView>(GetDeviceIdInputId());
            return input.Value ?? string.Empty;
        }

        private string GetDeviceIdInputId() => PageId + DeviceIdInputIdSuffix;
        private string GetCommandSelectId() => PageId + CommandSelectIdSuffix;
    }
}
