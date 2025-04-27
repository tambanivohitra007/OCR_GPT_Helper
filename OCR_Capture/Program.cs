namespace OCR_Capture
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            // Set the default text rendering compatibility mode
            Application.SetCompatibleTextRenderingDefault(false);
            // Set high DPI settings for better scaling on monitors with different resolutions/scaling factors
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.Run(new MainForm());
        }
    }
}