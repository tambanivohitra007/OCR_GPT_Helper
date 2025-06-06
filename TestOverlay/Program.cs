using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text; // For StringBuilder in GlobalMouseHook for logging (optional)

namespace TestOverlayImproved
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            // The ApplicationContext will manage the global hook and context menu.
            Application.Run(new TextSelectionApplicationContext());
        }
    }

    public class TextSelectionApplicationContext : ApplicationContext
    {
        private ContextMenuForm _contextMenuForm;
        private GlobalMouseHook _globalMouseHook;
        private Point _mouseDownPosition = Point.Empty;
        private const int SELECTION_THRESHOLD = 5; // Min pixels moved to be considered a selection drag

        public TextSelectionApplicationContext()
        {
            _contextMenuForm = new ContextMenuForm();
            _globalMouseHook = new GlobalMouseHook();

            _globalMouseHook.LeftButtonDown += OnGlobalLeftButtonDown;
            _globalMouseHook.LeftButtonUp += OnGlobalLeftButtonUp;

            _globalMouseHook.Start();
        }

        private void OnGlobalLeftButtonDown(object sender, MouseEventArgs e)
        {
            _mouseDownPosition = e.Location;
            // If the context menu is visible, a click outside might mean we should hide it.
            // The menu's Deactivate event handles this, but good to be aware.
        }

        private void OnGlobalLeftButtonUp(object sender, MouseEventArgs e)
        {
            Point mouseUpPosition = e.Location;

            // Check if the mouse moved significantly, indicating a drag selection
            if (Math.Abs(mouseUpPosition.X - _mouseDownPosition.X) > SELECTION_THRESHOLD ||
                Math.Abs(mouseUpPosition.Y - _mouseDownPosition.Y) > SELECTION_THRESHOLD)
            {
                // Potential selection ended. Try to get selected text.
                string selectedText = GetSelectedTextViaClipboard();

                if (!string.IsNullOrEmpty(selectedText))
                {
                    // If our own context menu is somehow the source, ignore.
                    IntPtr fgWindow = NativeMethods.GetForegroundWindow();
                    if (_contextMenuForm.IsHandleCreated && fgWindow == _contextMenuForm.Handle)
                    {
                        return;
                    }
                    _contextMenuForm.ShowMenu(mouseUpPosition, selectedText);
                }
            }
            _mouseDownPosition = Point.Empty; // Reset
        }

        private string GetSelectedTextViaClipboard()
        {
            // 1. Store current clipboard data
            IDataObject oldClipboardData = null;
            string originalText = null;
            bool restoreClipboard = false;

            try
            {
                if (Clipboard.ContainsText())
                {
                    originalText = Clipboard.GetText();
                }
                // For more complex data, use GetDataObject and check types.
                // For simplicity here, we'll just focus on text.
                // If there's non-text data, we might want to be more careful.
                // For this example, if originalText is null, we won't try to restore text.
                // A full implementation might store the IDataObject.
                oldClipboardData = Clipboard.GetDataObject();
                restoreClipboard = true; // Mark that we should attempt to restore
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error accessing old clipboard data: {ex.Message}");
                restoreClipboard = false; // Cannot guarantee restore
            }

            // 2. Clear clipboard or set a unique placeholder to know if Ctrl+C worked
            try
            {
                Clipboard.SetText(" "); // Set to a non-empty known value
            }
            catch (Exception ex) { Debug.WriteLine($"Error clearing/setting clipboard: {ex.Message}"); /* Continue if this fails */ }


            // 3. Send Ctrl+C
            NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[4];
            inputs[0] = new NativeMethods.INPUT { type = NativeMethods.INPUT_KEYBOARD, u = { ki = new NativeMethods.KEYBDINPUT { wVk = NativeMethods.VK_CONTROL, dwFlags = 0 } } };
            inputs[1] = new NativeMethods.INPUT { type = NativeMethods.INPUT_KEYBOARD, u = { ki = new NativeMethods.KEYBDINPUT { wVk = (ushort)'C', dwFlags = 0 } } };
            inputs[2] = new NativeMethods.INPUT { type = NativeMethods.INPUT_KEYBOARD, u = { ki = new NativeMethods.KEYBDINPUT { wVk = (ushort)'C', dwFlags = NativeMethods.KEYEVENTF_KEYUP } } };
            inputs[3] = new NativeMethods.INPUT { type = NativeMethods.INPUT_KEYBOARD, u = { ki = new NativeMethods.KEYBDINPUT { wVk = NativeMethods.VK_CONTROL, dwFlags = NativeMethods.KEYEVENTF_KEYUP } } };
            NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));

            // 4. Wait for clipboard to update
            System.Threading.Thread.Sleep(150); // Adjust if needed

            // 5. Retrieve new clipboard text
            string selectedText = string.Empty;
            try
            {
                if (Clipboard.ContainsText())
                {
                    string clipboardContent = Clipboard.GetText();
                    if (clipboardContent != " ") // Check if it changed from our placeholder
                    {
                        selectedText = clipboardContent;
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"Error getting new clipboard text: {ex.Message}"); }

            // 6. Restore original clipboard data
            if (restoreClipboard)
            {
                try
                {
                    if (oldClipboardData != null)
                    {
                        Clipboard.SetDataObject(oldClipboardData, true, 5, 100); // Retries
                    }
                    else
                    {
                        // If old data was null (e.g. clipboard was empty or non-text)
                        // clearing it is a safe default.
                        Clipboard.Clear();
                    }
                }
                catch (Exception ex) { Debug.WriteLine($"Error restoring clipboard data: {ex.Message}"); }
            }
            return selectedText.Trim();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _globalMouseHook?.Stop();
                _globalMouseHook?.Dispose();
                _contextMenuForm?.Close(); // This will also dispose the form
            }
            base.Dispose(disposing);
        }
    }

    public class ContextMenuForm : Form
    {
        private System.Windows.Forms.Timer _fadeTimer;
        private FlowLayoutPanel _buttonPanel;
        private Label _selectedTextPreviewLabel; // Optional: to show a snippet

        public ContextMenuForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(240, 240, 240); // Light gray background
            StartPosition = FormStartPosition.Manual;
            Padding = new Padding(2); // Small padding around the panel

            // Timer for the fade-in effect
            _fadeTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _fadeTimer.Tick += (s, e) =>
            {
                if (Opacity >= 1.0) { _fadeTimer.Stop(); }
                else { Opacity += 0.15; }
            };

            _buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill, // Panel will fill the form
                Padding = new Padding(5)
            };
            Controls.Add(_buttonPanel);

            // Optional: Add a label to show a preview of the selected text
            _selectedTextPreviewLabel = new Label
            {
                Text = "Selected: ...",
                AutoSize = true,
                ForeColor = Color.DimGray,
                Font = new Font(Font.FontFamily, 8f),
                Margin = new Padding(3, 0, 3, 5) // Bottom margin
            };
            _buttonPanel.Controls.Add(_selectedTextPreviewLabel);


            AddMenuItem("Translate");
            AddMenuItem("Explain");
            AddMenuItem("Suggest Reply");
            // AddMenuItem("Answer"); // You can add more

            this.Deactivate += (s, e) => HideMenu(); // Hide when focus is lost
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        private void AddMenuItem(string text)
        {
            Button menuItem = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0, MouseDownBackColor = Color.LightBlue, MouseOverBackColor = Color.AliceBlue },
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 5, 8, 5),
                Margin = new Padding(1),
                AutoSize = true,
                MinimumSize = new Size(120, 0), // Ensure a minimum width
                Dock = DockStyle.Top // Will stack vertically
            };
            menuItem.Click += (s, e) =>
            {
                HandleAction(text, _selectedTextPreviewLabel.Tag as string ?? ""); // Pass full selected text
                HideMenu();
            };
            _buttonPanel.Controls.Add(menuItem);
        }

        private void HandleAction(string action, string text)
        {
            // In a real app, you'd call your translation/explanation logic here.
            MessageBox.Show($"Action: {action}\nText: {text.Substring(0, Math.Min(text.Length, 100))}...", "Action Triggered");
        }

        public void ShowMenu(Point screenPosition, string selectedText)
        {
            if (IsDisposed) return;

            // Update preview label
            _selectedTextPreviewLabel.Text = $"Selected: \"{selectedText.Substring(0, Math.Min(selectedText.Length, 30))}...\"";
            _selectedTextPreviewLabel.Tag = selectedText; // Store full text


            // Adjust position if it's too close to screen edges
            int menuWidth = this.Width; // Use current width (after autosize) or estimate
            int menuHeight = this.Height;

            if (screenPosition.X + menuWidth > Screen.PrimaryScreen.WorkingArea.Right)
            {
                screenPosition.X = Screen.PrimaryScreen.WorkingArea.Right - menuWidth - 5;
            }
            if (screenPosition.Y + menuHeight > Screen.PrimaryScreen.WorkingArea.Bottom)
            {
                screenPosition.Y = Screen.PrimaryScreen.WorkingArea.Bottom - menuHeight - 5;
            }


            Location = screenPosition;
            if (!Visible)
            {
                Opacity = 0;
                Show();
                _fadeTimer.Start();
            }
            else
            {
                Opacity = 1; // If already visible, ensure full opacity and bring to front
                BringToFront();
            }
        }

        public void HideMenu()
        {
            if (IsDisposed) return;
            _fadeTimer.Stop();
            if (Visible) Hide();
            Opacity = 1;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_LAYERED;
                return cp;
            }
        }
        protected override bool ShowWithoutActivation => true;

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Custom border drawing
            base.OnPaintBackground(e);
            using (Pen borderPen = new Pen(Color.Gray, 1))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            }
        }
    }

    public class GlobalMouseHook : IDisposable
    {
        private NativeMethods.HookProc _mouseHookDelegate;
        private IntPtr _mouseHookHandle = IntPtr.Zero;
        private const int WH_MOUSE_LL = 14; // Low-level mouse hook

        public event MouseEventHandler LeftButtonDown;
        public event MouseEventHandler LeftButtonUp;
        // Add RightButtonDown/Up, MouseMove etc. as needed

        public void Start()
        {
            if (_mouseHookHandle != IntPtr.Zero) return; // Already hooked

            _mouseHookDelegate = MouseHookCallback; // Keep a reference to prevent GC
            _mouseHookHandle = NativeMethods.SetWindowsHookEx(
                WH_MOUSE_LL,
                _mouseHookDelegate,
                NativeMethods.GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName),
                0); // 0 for desktop-global hook

            if (_mouseHookHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(errorCode, "Failed to set mouse hook.");
            }
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0) // Process the message
            {
                NativeMethods.MSLLHOOKSTRUCT mouseHookStruct = (NativeMethods.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.MSLLHOOKSTRUCT));
                MouseButtons button = MouseButtons.None;
                Point point = new Point(mouseHookStruct.pt.x, mouseHookStruct.pt.y);
                int clicks = 0; // Clicks not reliable from MSLLHOOKSTRUCT, usually 0 or 1
                int delta = 0;  // Wheel delta

                switch ((NativeMethods.MouseMessages)wParam)
                {
                    case NativeMethods.MouseMessages.WM_LBUTTONDOWN:
                        button = MouseButtons.Left;
                        LeftButtonDown?.Invoke(this, new MouseEventArgs(button, clicks, point.X, point.Y, delta));
                        break;
                    case NativeMethods.MouseMessages.WM_LBUTTONUP:
                        button = MouseButtons.Left;
                        LeftButtonUp?.Invoke(this, new MouseEventArgs(button, clicks, point.X, point.Y, delta));
                        break;
                        // Add other cases like WM_RBUTTONDOWN, WM_RBUTTONUP, WM_MOUSEMOVE if needed
                }
            }
            return NativeMethods.CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
        }

        public void Stop()
        {
            if (_mouseHookHandle != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_mouseHookHandle);
                _mouseHookHandle = IntPtr.Zero;
                _mouseHookDelegate = null; // Release the delegate
            }
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
        ~GlobalMouseHook() { Stop(); } // Finalizer as a fallback
    }

    public static class NativeMethods
    {
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_LAYERED = 0x00080000;

        // For Global Mouse Hook
        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        // For SendInput (simulating Ctrl+C)
        public const ushort VK_CONTROL = 0x11;
        public const uint KEYEVENTF_KEYUP = 0x0002;
        public const int INPUT_KEYBOARD = 1;

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }


        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);


        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData; // For WM_MOUSEWHEEL or WM_XBUTTONDOWN/UP, contains wheel delta or X button number
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
            // Add other messages as needed
        }
    }
}
