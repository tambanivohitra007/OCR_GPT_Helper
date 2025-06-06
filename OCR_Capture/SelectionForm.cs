﻿// Application: OCR GPT Helper
// Version: 1.0.0
// Developer: Rindra Razafinjatovo
// Occupation: IT Administration/Instructor

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OCR_Capture
{

    /// <summary>
    /// A transparent fullscreen form used to allow the user to select a screen area.
    /// Captures the selected area as a screenshot.
    /// </summary>
    public partial class SelectionForm : Form
    {
        private Point startPoint; // Starting point of the selection rectangle
        private Point endPoint;   // Current or ending point of the selection rectangle
        private bool isSelecting = false; // Flag to indicate if the user is currently dragging
        private Rectangle selectedRectangle = Rectangle.Empty; // The final selected area

        /// <summary>
        /// Gets the rectangle representing the area selected by the user.
        /// </summary>
        public Rectangle SelectedRectangle => selectedRectangle;
        public string SelectedAction { get; private set; }

        public SelectionForm()
        {
            InitializeComponent();
            
            DoubleBuffered = true; // Enable double buffering to reduce flicker
            KeyPreview = true; // Allow the form to capture key events before they reach controls

            // --- Remove context menu creation and display from constructor ---
            // The action menu should only be shown after a valid selection.
        }

        // --- Remove the unused HandleAction method ---
        // void HandleAction(string action)
        // {
        //     MessageBox.Show($"You selected: {action}");
        // }

        /// <summary>
        /// Handles the MouseDown event to start the selection process.
        /// </summary>
        private void SelectionForm_MouseDown(object sender, MouseEventArgs e)
        {
            // Start selecting only on left mouse button press
            if (e.Button == MouseButtons.Left)
            {
                startPoint = e.Location; // Record the starting point
                isSelecting = true;      // Set the selecting flag
                selectedRectangle = Rectangle.Empty; // Clear any previous selection
                Opacity = 0.6; // Make the form slightly visible while dragging

                // *** ADD THIS LINE ***
                this.Capture = true; // Explicitly capture mouse input to this form
            }
        }

        /// <summary>
        /// Handles the MouseMove event to update the selection rectangle while dragging.
        /// </summary>
        private void SelectionForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                endPoint = e.Location; // Record the current mouse position
                                       // Calculate the rectangle based on start and end points.
                                       // Use Math.Min/Max to handle dragging in any direction.
                selectedRectangle = new Rectangle(
                    Math.Min(startPoint.X, endPoint.X),
                    Math.Min(startPoint.Y, endPoint.Y),
                    Math.Abs(startPoint.X - endPoint.X), // Width is absolute difference
                    Math.Abs(startPoint.Y - endPoint.Y)); // Height is absolute difference

                this.Invalidate(); // Request the form to redraw itself (triggers Paint event)
            }
        }

        /// <summary>
        /// Handles the MouseUp event to finalize the selection.
        /// </summary>
        private async void SelectionForm_MouseUp(object sender, MouseEventArgs e)
        {
            // Finalize selection only on left mouse button release and if selecting was active
            if (e.Button == MouseButtons.Left && isSelecting)
            {
                endPoint = e.Location; // Record the final mouse position
                isSelecting = false;   // Clear the selecting flag

                this.Capture = false; // Release mouse capture

                Opacity = 0.6;   // Return to near-full transparency

                // Calculate the final selected rectangle
                selectedRectangle = new Rectangle(
                   Math.Min(startPoint.X, endPoint.X),
                   Math.Min(startPoint.Y, endPoint.Y),
                   Math.Abs(startPoint.X - endPoint.X),
                   Math.Abs(startPoint.Y - endPoint.Y));

                // If a valid area (non-zero width and height) was selected
                if (selectedRectangle.Width > 0 && selectedRectangle.Height > 0)
                {
                    // Show the action menu and wait for the user's choice asynchronously
                    var action = await ShowActionMenu();

                    SelectedAction = action ?? string.Empty;

                    // Optional: Debugging aid
                    // MessageBox.Show($"SelectedAction: '{SelectedAction}'");

                    // Only proceed if the user picked an action (not cancelled)
                    if (!string.IsNullOrEmpty(SelectedAction))
                    {
                        DialogResult = DialogResult.OK;
                        this.Hide();
                    }
                    else
                    {
                        // If cancelled, reset selection and let the user try again
                        selectedRectangle = Rectangle.Empty;
                        this.Invalidate();
                    }
                }
            }
        }

        private Task<string?> ShowActionMenu()
        {
            var tcs = new TaskCompletionSource<string?>();
            var menu = new ContextMenuStrip();
            bool itemClicked = false;

            ToolStripItemClickedEventHandler? itemClickedHandler = null;
            itemClickedHandler = (s, e) =>
            {
                itemClicked = true;
                string? result = e.ClickedItem?.Text switch
                {
                    "Answer Question" => "answer",
                    "Explain" => "explain",
                    "Translate" => "translate",
                    "Enhance" => "enhance",
                    "Reply" => "reply",
                    _ => null
                };
                // Do NOT call menu.Close() here; clicking an item will close the menu automatically.
                tcs.TrySetResult(result);
            };

            menu.Items.Add("Answer Question");
            menu.Items.Add("Explain");
            menu.Items.Add("Translate");
            menu.Items.Add("Enhance");
            menu.Items.Add("Reply");
            menu.ItemClicked += itemClickedHandler;

            menu.Closed += (s, e) =>
            {
                // Only set null if no item was clicked (i.e., user dismissed the menu)
                if (!itemClicked)
                    tcs.TrySetResult(null);
                if (itemClickedHandler != null)
                    menu.ItemClicked -= itemClickedHandler;
                // Only dispose if not already disposed
                //if (!menu.IsDisposed)
                //    menu.Dispose();
            };

            menu.Show(Cursor.Position);
            return tcs.Task;
        }

        /// <summary>
        /// Handles KeyDown events to allow canceling the selection with ESC.
        /// </summary>
        private void SelectionForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                // If ESC is pressed, cancel the selection
                isSelecting = false;
                selectedRectangle = Rectangle.Empty; // Clear the selected area
                Opacity = 0.6; // Return to near-full transparency

                // *** ADD THIS LINE ***
                this.Capture = false; // Release mouse capture

                DialogResult = DialogResult.Cancel; // Set DialogResult to Cancel
                this.Hide(); // Hide the selection form
            }
        }

        /// <summary>
        /// Optimized Paint method for better resource usage and rendering
        /// </summary>
        private void SelectionForm_Paint(object sender, PaintEventArgs e)
        {
            if (isSelecting && !selectedRectangle.IsEmpty)
            {
                // Fill the selected area with a semi-transparent color
                using (var brush = new SolidBrush(Color.FromArgb(100, Color.Blue)))
                {
                    e.Graphics.FillRectangle(brush, selectedRectangle);
                }

                // Draw a dashed red border around the selected area
                using (var pen = new Pen(Color.Red, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                {
                    e.Graphics.DrawRectangle(pen, selectedRectangle);
                }
            }
        }

        /// <summary>
        /// Captures the specified area of the screen as a Bitmap.
        /// </summary>
        /// <param name="area">The screen area to capture.</param>
        /// <returns>A Bitmap containing the screenshot of the specified area, or null if the area is invalid.</returns>
        public Bitmap CaptureScreen(Rectangle area)
        {
            // Return null if the area has zero width or height
            if (area.Width <= 0 || area.Height <= 0)
            {
                return null;
            }

            // Create a new Bitmap object with the dimensions of the selected area
            Bitmap screenshot = new Bitmap(area.Width, area.Height);

            // Create a Graphics object from the Bitmap. This allows drawing onto the bitmap.
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                // Copy the pixels from the screen's specified area to the Bitmap.
                // The source point is the top-left corner of the 'area' rectangle.
                // The destination point on the bitmap is (0,0).
                // The size is the width and height of the 'area' rectangle.
                graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
            }

            return screenshot; // Return the captured bitmap
        }
    }
}
