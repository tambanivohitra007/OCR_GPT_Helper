using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OCR_Capture
{
    public static class HotkeyManager
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_NONE = 0x0000; // No modifier  
        private const uint MOD_ALT = 0x0001; // ALT key  
        private const uint MOD_CONTROL = 0x0002; // CTRL key  
        private const uint MOD_SHIFT = 0x0004; // SHIFT key  
        private const uint MOD_WIN = 0x0008; // Windows key  

        private const int WM_HOTKEY = 0x0312;

        public static int RegisterGlobalHotKey(IntPtr handle, uint modifiers, Keys key)
        {
            int id = Guid.NewGuid().GetHashCode(); // Generate a unique ID for the hotkey  
            if (!RegisterHotKey(handle, id, modifiers, (uint)key))
            {
                return 0; // Registration failed  
            }
            return id;
        }

        public static void UnregisterGlobalHotKey(IntPtr handle, int id)
        {
            UnregisterHotKey(handle, id);
        }

        public static bool IsHotKeyMessage(ref Message m, out int hotkeyId)
        {
            if (m.Msg == WM_HOTKEY)
            {
                hotkeyId = m.WParam.ToInt32();
                return true;
            }
            hotkeyId = 0;
            return false;
        }
    }
}
