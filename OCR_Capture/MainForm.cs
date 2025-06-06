// Application: OCR GPT Helper
// Version: 1.1.0
// Developer: Rindra Razafinjatovo
// Occupation: IT Administration/Instructor

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

        // Add this field to keep track of the open Answer form
        private Answer _answerForm;

        // Configuration settings
        private IConfiguration _configuration;
        private string _openAIApiKey;
        // Add a field for Gemini API key
        private string _geminiApiKey;

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
                // Add a field for Gemini API key
                _geminiApiKey = _configuration["Gemini:ApiKey"];

                // Provide feedback if essential configurations are missing
                if (string.IsNullOrWhiteSpace(_openAIApiKey))
                {
                    MessageBox.Show("OpenAI API Key is not configured in appsettings.json. Please add it to enable GPT functionality.", "Configuration Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // The application will still run, but GPT features will be disabled.
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
            trayMenu.Items.Add("Information", imageList.Images[1], OnInformationClick);
            trayMenu.Items.Add("Exit Answer AI", imageList.Images[2], OnExit);

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

        private void OnInformationClick(object? sender, EventArgs e)
        {
            string helpInfo = "How to Use OCR GPT Helper:\n\n" +
                              "1. Press F8 (the global hotkey) to start a screen capture.\n" +
                              "2. Select the area of the screen you want to capture.\n" +
                              "3. Choose one of the following actions:\n" +
                              "   - Answer: Get a direct answer to the captured text (e.g., a question).\n" +
                              "   - Explain: Get a detailed explanation of the captured text.\n" +
                              "   - Translate: Translate the captured text to another language.\n" +
                              "   - Enhance: Improve or rephrase the captured text.\n" +
                              "4. The result will appear in a popup window on your screen.\n\n" +
                              "You can access this help and other options by right-clicking the tray icon.\n" +
                              "Make sure your OpenAI API key is configured in appsettings.json for full functionality.";

            MessageBox.Show(helpInfo, "How to Use OCR GPT Helper", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            using (var selectionForm = new SelectionForm())
            {
                DialogResult result = selectionForm.ShowDialog();

                if (result == DialogResult.OK
                    && !selectionForm.SelectedRectangle.IsEmpty
                    && !string.IsNullOrEmpty(selectionForm.SelectedAction))
                {
                    using (Bitmap screenshot = selectionForm.CaptureScreen(selectionForm.SelectedRectangle))
                    {
                        if (screenshot == null)
                        {
                            MessageBox.Show("Failed to capture screenshot. The selected area might be invalid.", "Capture Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        string extractedText = null;
                        string gptResponse = null;

                        try
                        {
                            // --- Perform OCR ---
                            extractedText = await Task.Run(() =>
                            {
                                using (var ocrProcessor = new OcrProcessor())
                                {
                                    return ocrProcessor.RecognizeAsync(screenshot);
                                }
                            });

                            if (string.IsNullOrWhiteSpace(extractedText))
                            {
                                MessageBox.Show("No text was detected in the selected area.", "OCR Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }

                            // --- Perform GPT Query ---
                            if (!string.IsNullOrWhiteSpace(_openAIApiKey))
                            {
                                // Show loading indicator while waiting for OpenAI API
                                using (var loadingForm = new Form
                                {
                                    FormBorderStyle = FormBorderStyle.None,
                                    StartPosition = FormStartPosition.CenterScreen,
                                    Size = new Size(250, 80),
                                    TopMost = true,
                                    BackColor = Color.WhiteSmoke
                                })
                                {
                                    var label = new Label
                                    {
                                        Text = "Waiting for AI response...",
                                        Dock = DockStyle.Fill,
                                        TextAlign = ContentAlignment.MiddleCenter,
                                        Font = new Font("Segoe UI", 12F)
                                    };
                                    loadingForm.Controls.Add(label);
                                    loadingForm.Show();
                                    loadingForm.Refresh();

                                    var actionEnum = selectionForm.SelectedAction.ToLower() switch
                                    {
                                        "answer" => GeminiManager.GeminiAssistAction.Answer,
                                        "explain" => GeminiManager.GeminiAssistAction.Explain,
                                        "translate" => GeminiManager.GeminiAssistAction.Translate,
                                        "enhance" => GeminiManager.GeminiAssistAction.Enhance,
                                        "reply" => GeminiManager.GeminiAssistAction.Reply,
                                        _ => GeminiManager.GeminiAssistAction.Answer
                                    };

                                    gptResponse = await Task.Run(async () =>
                                    {
                                        using (var openAIManager = new GeminiManager(_openAIApiKey))
                                        {
                                            return await openAIManager.ProcessTextAsync(extractedText, actionEnum);
                                        }
                                    });

                                    loadingForm.Close();
                                }

                                DisplayGptResponse(gptResponse);
                            }
                            else
                            {
                                MessageBox.Show($"OCR Text:\n\n{extractedText}\n\nOpenAI API key is not configured, skipping GPT.", "OCR Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }

                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"An error occurred during processing:\n{ex.Message}", "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            if (!string.IsNullOrWhiteSpace(extractedText) && string.IsNullOrWhiteSpace(gptResponse))
                            {
                                MessageBox.Show($"Extracted Text (API Failed):\n\n{extractedText}", "OCR Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles the "Exit" menu item click from the system tray icon.
        /// Cleans up resources and exits the application.
        /// </summary>
        private void OnExit(object? sender, EventArgs e)
        {
            // Prevent ObjectDisposedException by detaching the context menu before disposing
            if (trayIcon != null)
                trayIcon.ContextMenuStrip = null;

            if (trayMenu != null)
            {
                trayMenu.Dispose();
                trayMenu = null;
            }

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
                trayIcon.ContextMenuStrip = null; // Detach menu to avoid accessing disposed object
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            if (trayMenu != null)
            {
                trayMenu.Dispose();
                trayMenu = null;
            }
            UnregisterHotkey();
        }

        /// <summary>
        /// Displays the GPT response in a modernized popup window and closes it after 3 seconds.
        /// </summary>
        /// <param name="gptResponse">The text response from GPT or an error message.</param>
        private void DisplayGptResponse(string gptResponse)
        {
            // If the answer form is already open and not disposed, update it
            if (_answerForm != null && !_answerForm.IsDisposed && _answerForm.Visible)
            {
                _answerForm.UpdateAnswerText(gptResponse);
                _answerForm.BringToFront();
                _answerForm.Activate();
            }
            else
            {
                // Dispose previous if needed
                if (_answerForm != null)
                {
                    try { _answerForm.Close(); } catch { }
                    _answerForm.Dispose();
                    _answerForm = null;
                }

                _answerForm = new Answer { AnswerText = gptResponse, TopMost = true };
                // Set the side here if you want left or right (default is right)
                _answerForm.Side = Answer.ScreenSide.Right; // or .Left
                _answerForm.FormClosed += (s, e) => { _answerForm = null; };
                _answerForm.Show();
            }
        }
    }

}
