# OCR GPT Answer Tool

A C# Windows Forms application that resides in the system tray, allowing users to capture a screen area, perform OCR on the captured image, and send the extracted text to OpenAI's GPT API for an answer, primarily aimed at solving multiple-choice questions.

---

**⚠️ Important Educational Notice ⚠️**

This project is developed strictly for **educational purposes** to demonstrate the integration of Windows system features (hotkeys, screen capture, Windows OCR) with external APIs (OpenAI GPT). It is intended as a learning resource for C# .NET development and API interaction.

**This tool should NOT be used for academic dishonesty, cheating on online quizzes, exams, or any other form of unauthorized assistance.** Using this tool in such a manner violates academic integrity policies and ethical guidelines. The developer is not responsible for any misuse of this software.

Please use this project responsibly and ethically.

---

## Features

* **System Tray Application:** Runs minimized in the background, accessible from the Windows system tray.

* **Global Hotkey:** Trigger screen capture by pressing the `F8` key.

* **Screen Area Selection:** A transparent overlay allows users to precisely select the area of the screen to capture, similar to the Windows Snipping Tool.

* **OCR (Optical Character Recognition):** Extracts text from the captured image using the built-in Windows.Media.Ocr capabilities.

* **OpenAI GPT Integration:** Sends the extracted text to the OpenAI Chat Completions API (e.g., GPT-4o) with a prompt designed to answer multiple-choice questions.

* **Modernized Response Display:** Displays the GPT response in a custom, more visually appealing popup window instead of a standard `MessageBox`.

* **Configuration:** Uses `appsettings.json` for configurable settings like the OpenAI API key and Tesseract data path (though Tesseract is replaced by Windows OCR, the setting structure remains).

* **Error Handling:** Includes basic error handling for capture failures, OCR issues, and API communication problems.

## Requirements

* **Operating System:** Windows 10 or newer (Windows.Media.Ocr requires a compatible Windows version).

* **.NET SDK:** .NET 6 or newer (project is configured for .NET 8).

* **OpenAI API Key:** An active OpenAI account and API key with access to chat completion models (e.g., `gpt-4o`, `gpt-3.5-turbo`).

* **Windows OCR Languages:** Ensure the necessary language packs with OCR capabilities are installed in your Windows Language settings.

## Setup and Installation

1. **Clone or Download:** Get the project source code.

2. **Open in Visual Studio:** Open the `.csproj` file in Visual Studio (2022 or newer recommended).

3. **Restore NuGet Packages:** Visual Studio should automatically restore the required NuGet packages (`OpenAI-DotNet`, `Microsoft.Extensions.Configuration.Json`, `System.Drawing.Common`, etc.). If not, right-click the project in Solution Explorer and select "Restore NuGet Packages".

4. **Configure Project for Windows SDK:**

   * Right-click your project file (`.csproj`) in Solution Explorer and select "Edit Project File".

   * Ensure the `<TargetFramework>` property is set to target a Windows SDK version, e.g., `<TargetFramework>net8.0-windows10.0.17763.0</TargetFramework>`.

   * Remove any explicit `<PackageReference Include="Microsoft.Windows.SDK.Contracts" ... />` lines, as these conflict with the modern .NET approach.

   * Save and close the `.csproj` file.

5. **OpenAI API Key:**

   * Locate the `appsettings.json` file in the project.

   * Replace `"YOUR_OPENAI_API_KEY_HERE"` with your actual OpenAI API key.

   * Ensure the "Copy to Output Directory" property for `appsettings.json` is set to "Copy always" or "Copy if newer".

6. **Application Icon (Optional but Recommended):**

   * Add an `.ico` file (e.g., `app.ico`) to your project.

   * Set its "Copy to Output Directory" property to "Copy if newer".

   * Update the icon file name in `MainForm.cs` if necessary.

7. **Build:** Build the project in Visual Studio (Build > Build Solution).

## How to Use

1. **Run the Application:** Navigate to the project's output directory (e.g., `bin\Debug\net8.0-windows10.0.17763.0\`) and run the executable (`OCR_Capture.exe` or similar).

2. **System Tray:** The application will start minimized and appear as an icon in your Windows system tray.

3. **Trigger Capture:** Press the `F8` key.

4. **Select Area:** The screen will become semi-transparent. Click and drag your mouse to draw a rectangle around the text you want to capture (e.g., a multiple-choice question in your browser).

5. **Release Mouse:** When you release the mouse button, the application will capture the selected area.

6. **Processing:** The application will perform OCR on the captured image and then send the extracted text to the OpenAI API.

7. **View Response:** A modernized popup window will appear displaying the response from the GPT model.

8. **Exit:** Right-click the system tray icon and select "Exit" to close the application.

## Project Structure

* `Program.cs`: The application's entry point.

* `MainForm.cs`: The main form (hidden), manages the system tray icon, hotkey registration, and orchestrates the capture, OCR, and GPT process. Contains the `HandleHotkeyTrigger` and `DisplayGptResponse` methods.

* `SelectionForm.cs`: A transparent fullscreen form used for drawing the screen selection rectangle and capturing the screenshot.

* `HotkeyManager.cs`: A helper class using P/Invoke to register and unregister global hotkeys.

* `OcrProcessor.cs`: Handles the OCR process using `Windows.Media.Ocr`.

* `appsettings.json`: Configuration file for API keys and settings.

## Error Handling

* **Configuration Errors:** Messages will be shown if the OpenAI API key or Tesseract path (though now using Windows OCR, the config structure remains) is missing or invalid.

* **Hotkey Registration:** An error message is displayed if the F8 hotkey cannot be registered.

* **Capture Errors:** If the screen capture fails, a message will be shown.

* **OCR Errors:** If no text is detected in the selected area or if there's an issue with the Windows OCR engine/language packs, an informative message is displayed.


## Potential Enhancements

* Add a settings dialog accessible from the tray icon to configure the API key and other options.

* Allow selecting different GPT models.

* Improve the UI of the response window (e.g., syntax highlighting for code in responses, copy button).

* Add options for different hotkeys.

* Implement logging for better debugging.

* Handle multi-monitor setups more explicitly if needed.

* Add support for saving captured images or extracted text.
