using Windows.Graphics.Imaging; // Requires targeting Windows SDK
using Windows.Media.Ocr;       // Requires targeting Windows SDK
using Windows.Storage.Streams; // Requires targeting Windows SDK
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime; // For Marshal

/// <summary>
/// Handles Optical Character Recognition (OCR) using the Windows.Media.Ocr engine.
/// This requires the application to be running on Windows 10 or newer and
/// the project to target a compatible Windows SDK version.
/// </summary>
public class OcrProcessor : IDisposable
{
    private OcrEngine _ocrEngine; // The Windows.Media.Ocr engine instance

    /// <summary>
    /// Initializes the Windows.Media.Ocr processor.
    /// </summary>
    public OcrProcessor()
    {
        // Windows.Media.Ocr is only available on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("Windows.Media.Ocr is only supported on Windows.");
        }

        // Attempt to create the OCR engine using the system's default language
        // You could add logic here to allow selecting a language if needed.
        _ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();

        if (_ocrEngine == null)
        {
            // This can happen if no OCR languages are installed or available
            throw new InvalidOperationException("Failed to create Windows.Media.Ocr engine. Ensure OCR languages are installed in Windows settings.");
        }
    }

    /// <summary>
    /// Performs OCR on a given Bitmap image asynchronously using Windows.Media.Ocr.
    /// </summary>
    /// <param name="image">The image to process.</param>
    /// <returns>The extracted text.</returns>
    public async Task<string> RecognizeAsync(Bitmap image)
    {
        if (_ocrEngine == null)
        {
            throw new InvalidOperationException("Windows.Media.Ocr engine is not initialized.");
        }

        if (image == null)
        {
            return string.Empty;
        }

        // Windows.Media.Ocr requires a SoftwareBitmap. We need to convert the System.Drawing.Bitmap.
        SoftwareBitmap softwareBitmap = null;
        try
        {
            // Convert System.Drawing.Bitmap to SoftwareBitmap
            softwareBitmap = await ConvertBitmapToSoftwareBitmapAsync(image);

            // Perform OCR asynchronously
            var ocrResult = await _ocrEngine.RecognizeAsync(softwareBitmap);

            // Return the extracted text, joining lines
            return ocrResult.Text.Trim();
        }
        catch (Exception ex)
        {
            // Handle exceptions during OCR processing
            throw new Exception($"An error occurred during Windows.Media.Ocr processing: {ex.Message}", ex);
        }
        finally
        {
            // Dispose the SoftwareBitmap
            softwareBitmap?.Dispose();
        }
    }

    /// <summary>
    /// Converts a System.Drawing.Bitmap to a Windows.Graphics.Imaging.SoftwareBitmap.
    /// This is necessary because Windows.Media.Ocr operates on SoftwareBitmap.
    /// </summary>
    private async Task<SoftwareBitmap> ConvertBitmapToSoftwareBitmapAsync(Bitmap bitmap)
    {
        // Save the System.Drawing.Bitmap to a stream in a format like PNG
        using (var ms = new MemoryStream())
        {
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0; // Reset stream position to the beginning

            // Create a RandomAccessStreamReference from the MemoryStream
            using (var ras = new InMemoryRandomAccessStream())
            {
                // Replace the problematic line with the following:
                await ras.WriteAsync(ms.ToArray().AsBuffer());
                ras.Seek(0); // Reset stream position for reading

                // Create a BitmapDecoder from the stream
                var decoder = await BitmapDecoder.CreateAsync(ras);

                // Get the first frame of the image
                var bitmapFrame = await decoder.GetFrameAsync(0);

                // Convert to a SoftwareBitmap in a suitable pixel format (e.g., Gray8 or Rgba16)
                // Gray8 is often sufficient for OCR and more memory efficient.
                // Rgba16 might be needed for color images or specific scenarios.
                // Use the GetSoftwareBitmapAsync method instead of the non-existent CreateCopyFromFrameAsync.
                try
                {
                    return await bitmapFrame.GetSoftwareBitmapAsync(BitmapPixelFormat.Gray8, BitmapAlphaMode.Ignore);
                }
                catch (Exception ex)
                {
                    // If Gray8 fails, try Rgba16
                    System.Diagnostics.Debug.WriteLine($"Failed to convert to Gray8: {ex.Message}. Trying Rgba16.");
                    return await bitmapFrame.GetSoftwareBitmapAsync(BitmapPixelFormat.Rgba16, BitmapAlphaMode.Premultiplied);
                }
            }
        }
    }


    /// <summary>
    /// Disposes the Windows.Media.Ocr engine and releases resources.
    /// Implements the IDisposable interface for proper cleanup.
    /// </summary>
    public void Dispose()
    {
        // The Windows.Media.Ocr.OcrEngine class does not implement IDisposable,
        // as WinRT objects are typically garbage collected by the system.
        // However, we keep the Dispose method structure for completeness
        // and consistency with the previous Tesseract implementation.
        // No explicit disposal of _ocrEngine is needed here.
    }
}
