namespace HSPI_RoborockBridge {
    internal static class Program {
        private static HSPI _plugin;

        public static void Main(string[] args) {
            _plugin = new HSPI();
            _plugin.Connect(args);
        }
    }
}
