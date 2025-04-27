using Microsoft.Extensions.Configuration;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
namespace OCR_Capture
{

    /// <summary>
    /// The main application form, which runs minimized in the system tray.
    /// Manages the global hotkey, system tray icon, and orchestrates the
    /// screen capture, OCR, and OpenAI API call process.
    /// </summary>
    public partial class MainForm : Form
    {
        // A unique ID for our hotkey registration (can be any non-zero value)
        private const int HOTKEY_ID = 9001;
        private int registeredHotkeyId = 0; // Stores the actual ID returned by RegisterGlobalHotKey

        private NotifyIcon trayIcon; // The system tray icon
        private ContextMenuStrip trayMenu; // The context menu for the tray icon
            
        // Configuration settings
        private IConfiguration _configuration;
        private string _openAIApiKey;
        private string _tessdataPath; // Path to the directory *containing* the 'tessdata' folder

        public MainForm()
        {
            InitializeComponent(); // Standard WinForms designer initialization

            // Load configuration from appsettings.json
            LoadConfiguration();

            // Configure the form to run in the background/tray
            this.ShowInTaskbar = false;           // Don't show in the taskbar
            this.Visible = false;                 // Ensure it's not visible initially

            // Setup the System Tray Icon
            SetupTrayIcon();

            // Register the global hotkey (F8)
            RegisterHotkey();
        }

        /// <summary>
        /// Loads configuration settings from appsettings.json.
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                // Build configuration from appsettings.json.
                // Optional: true means the app won't crash if the file is missing.
                // reloadOnChange: true allows updating settings while the app is running (not used in this simple example).
                _configuration = new ConfigurationBuilder()
                   .SetBasePath(AppDomain.CurrentDomain.BaseDirectory) // Look for appsettings.json in the application directory
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                   .Build();

                // Read the OpenAI API key from configuration
                _openAIApiKey = _configuration["OpenAI:ApiKey"];
                // Read the Tesseract data path from configuration
                _tessdataPath = _configuration["Tesseract:TessdataPath"]; // Path to the directory *containing* tessdata

                // Provide feedback if essential configurations are missing
                if (string.IsNullOrWhiteSpace(_openAIApiKey))
                {
                    MessageBox.Show("OpenAI API Key is not configured in appsettings.json. Please add it to enable GPT functionality.", "Configuration Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // The application will still run, but GPT features will be disabled.
                }

                if (string.IsNullOrWhiteSpace(_tessdataPath))
                {
                    // If Tesseract path is not specified in config, default to a common location
                    // relative to the executable (assuming tessdata is copied there).
                    _tessdataPath = Path.Combine(Application.StartupPath, "tessdata");
                    // We will check if this path exists later during OCR processing.
                }
                else
                {
                    // If a path is specified, ensure it's treated as the directory *containing* tessdata.
                    // The TesseractEngine constructor expects the parent directory path.
                    // If the user provided the full path to tessdata, we need to get the parent directory.
                    // A simple check: if the path ends with "tessdata", use its parent.
                    if (_tessdataPath.EndsWith("tessdata", StringComparison.OrdinalIgnoreCase))
                    {
                        _tessdataPath = Path.GetDirectoryName(_tessdataPath);
                    }
                    // Note: This simple check might not cover all user input variations.
                    // More robust path handling might be needed in a production app.
                }

            }
            catch (Exception ex)
            {
                // Handle any errors during configuration loading
                MessageBox.Show($"Failed to load configuration: {ex.Message}", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Decide how to proceed: disable features, or exit the application.
                // For now, we'll let it continue but features relying on config might fail.
            }
        }

        /// <summary>
        /// Sets up the system tray icon and its context menu.
        /// </summary>
        private void SetupTrayIcon()
        {
            // Create the context menu for the tray icon
            trayMenu = new ContextMenuStrip();

            // Add an "Exit" menu item
            trayMenu.Items.Add("Exit Answer AI", null, OnExit);

            // Create the NotifyIcon instance
            trayIcon = new NotifyIcon();

            // Set the icon. You need to add an icon file (e.g., app.ico) to your project
            // and set its "Copy to Output Directory" property to "Copy if newer".
            try
            {
                // Attempt to load the icon from a file named "app.ico" in the application directory
                trayIcon.Icon = new Icon(Path.Combine(Application.StartupPath, "app.ico"));
            }
            catch
            {
                // If the icon file is not found or loading fails, use a default system icon
                MessageBox.Show("Icon file 'app.ico' not found or could not be loaded. Using a default system icon.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                trayIcon.Icon = SystemIcons.Application; // Fallback to a standard system icon
            }

            // Set the tooltip text that appears when hovering over the icon
            trayIcon.Text = "OCR GPT Helper (F8 to Capture)";

            // 👉 Corrected this line
            trayIcon.ContextMenuStrip = trayMenu;

            // Make the tray icon visible
            trayIcon.Visible = true;
        }


        /// <summary>
        /// Registers the global F8 hotkey using the HotkeyManager.
        /// </summary>
        private void RegisterHotkey()
        {
            // Register F8 global hotkey. No modifier keys (Alt, Ctrl, Shift, Win) are used here.
            // Pass the handle of this form, as it will receive the WM_HOTKEY message.
            registeredHotkeyId = HotkeyManager.RegisterGlobalHotKey(this.Handle, 0, Keys.F8);

            // Check if hotkey registration failed
            if (registeredHotkeyId == 0)
            {
                MessageBox.Show("Failed to register F8 hotkey. Another application might be using it. The application will run, but the F8 hotkey will not work.", "Hotkey Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // The application will continue to run, but the primary trigger won't work.
                // You might consider exiting here in a real application if the hotkey is essential.
            }
        }

        /// <summary>
        /// Unregisters the previously registered global hotkey.
        /// </summary>
        private void UnregisterHotkey()
        {
            // Unregister the hotkey using the ID returned during registration
            HotkeyManager.UnregisterGlobalHotKey(this.Handle, registeredHotkeyId);
        }

        /// <summary>
        /// Overrides the form's message processing method to listen for Windows messages.
        /// This is where we detect the WM_HOTKEY message when the hotkey is pressed.
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            // Check if the received message is a hotkey message and if it matches our registered hotkey ID
            if (HotkeyManager.IsHotKeyMessage(ref m, out int hotkeyId))
            {
                if (hotkeyId == registeredHotkeyId)
                {
                    // If it's our hotkey, trigger the capture and processing logic
                    HandleHotkeyTrigger();
                    //testHandleHotKey();
                }
            }
            // Pass the message to the base class's WndProc for standard processing
            base.WndProc(ref m);
        }

        /// <summary>
        /// Handles the event when the registered hotkey is pressed.
        /// Shows the selection form, captures the screen, performs OCR, and calls the GPT API.
        /// Uses async/await to keep the UI responsive during processing.
        /// </summary>
        private async void HandleHotkeyTrigger()
        {
            // Prevent multiple selection forms from opening if the hotkey is pressed rapidly
            if (SelectionForm.ActiveForm != null && SelectionForm.ActiveForm is SelectionForm)
            {
                // A selection form is already active, ignore this hotkey press
                return;
            }

            // Create and show the SelectionForm. Use 'using' for automatic disposal.
            using (var selectionForm = new SelectionForm())
            {
                // Show the selection form modally and wait for the user to select an area or cancel.
                DialogResult result = selectionForm.ShowDialog();

                // Proceed only if the user selected an area (DialogResult.OK) and the rectangle is valid
                if (result == DialogResult.OK && !selectionForm.SelectedRectangle.IsEmpty)
                {
                    // Capture the selected area of the screen
                    using (Bitmap screenshot = selectionForm.CaptureScreen(selectionForm.SelectedRectangle))
                    {
                        // Check if the screenshot capture was successful
                        if (screenshot == null)
                        {
                            MessageBox.Show("Failed to capture screenshot. The selected area might be invalid.", "Capture Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return; // Stop processing if capture failed
                        }

                        string extractedText = null; // Variable to hold the text from OCR
                        string gptResponse = null;    // Variable to hold the response from GPT

                        try
                        {
                            // --- Perform OCR ---
                            // Use Task.Run to execute the CPU-bound OCR process on a background thread
                            // This prevents the UI from freezing.
                            extractedText = await Task.Run(() =>
                            {
                                // Create and use the OcrProcessor within the background task.
                                // Use 'using' for proper disposal of the Tesseract engine.
                                using (var ocrProcessor = new OcrProcessor())
                                {
                                    return ocrProcessor.RecognizeAsync(screenshot);
                                }
                            });

                            // Check if any text was extracted by OCR
                            if (string.IsNullOrWhiteSpace(extractedText))
                            {
                                MessageBox.Show("No text was detected in the selected area.", "OCR Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return; // Stop processing if no text was found
                            }

                            // --- Perform GPT Query ---
                            // Only proceed with the GPT query if an API key is configured
                            if (!string.IsNullOrWhiteSpace(_openAIApiKey))
                            {
                                // Use Task.Run for the I/O-bound API call as well, although HttpClient handles async.
                                // This ensures the entire block is off the UI thread.
                                gptResponse = await Task.Run(async () =>
                                {
                                    // Create and use the OpenAIManager within the background task.
                                    using (var openAIManager = new OpenAIManager(_openAIApiKey))
                                    {
                                        // Call the async method to ask the question
                                        return await openAIManager.AskQuestionAsync(extractedText);
                                    }
                                });

                                // Display the GPT response in a message box
                                //MessageBox.Show(gptResponse, "GPT Answer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                DisplayGptResponse(gptResponse);
                            }
                            else
                            {
                                // If no API key is configured, just show the extracted text
                                MessageBox.Show($"OCR Text:\n\n{extractedText}\n\nOpenAI API key is not configured, skipping GPT.", "OCR Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }

                        }
                        catch (Exception ex)
                        {
                            // Handle exceptions that occur during OCR or API call
                            MessageBox.Show($"An error occurred during processing:\n{ex.Message}", "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            // Optionally show the extracted text even if the API call failed
                            if (!string.IsNullOrWhiteSpace(extractedText) && string.IsNullOrWhiteSpace(gptResponse))
                            {
                                MessageBox.Show($"Extracted Text (API Failed):\n\n{extractedText}", "OCR Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                }
                // If DialogResult is Cancel, the user pressed ESC or didn't select a valid area.
                // The 'using' statement will ensure the selectionForm is disposed.
            }
        }

        /// <summary>
        /// Handles the "Exit" menu item click from the system tray icon.
        /// Cleans up resources and exits the application.
        /// </summary>
        private void OnExit(object sender, EventArgs e)
        {
            trayIcon?.Dispose();
            UnregisterHotkey();
            Application.Exit();
        }

        /// <summary>
        /// Handles the form's Load event. Used to immediately hide the form.
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            // Hide the form as soon as it's loaded
            this.Hide();
        }

        /// <summary>
        /// Handles the form's FormClosing event. Used for cleanup before the application exits.
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Ensure resources are cleaned up when the form is closing (e.g., if Application.Exit() is called elsewhere)
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            UnregisterHotkey();
        }

        /// <summary>
        /// Displays the GPT response in a modernized popup window and closes it after 3 seconds.
        /// </summary>
        /// <param name="gptResponse">The text response from GPT or an error message.</param>
        private void DisplayGptResponse(string gptResponse)
        {
            using (var responseForm = new Answer { AnswerText = gptResponse, TopMost = true })
            {
                var closeTimer = new System.Windows.Forms.Timer { Interval = 3000 };
                closeTimer.Tick += (s, e) =>
                {
                    closeTimer.Stop();
                    responseForm.Close();
                };
                closeTimer.Start();

                responseForm.ShowDialog();
            }
        }
    }

}
