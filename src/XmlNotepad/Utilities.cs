using System;
using System.Xml;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.IO;

namespace XmlNotepad
{
    public sealed class Utilities
    {
        private Utilities() { }

        public static void InitializeWriterSettings(XmlWriterSettings settings, IServiceProvider sp) {
            settings.CheckCharacters = false;
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.NewLineChars = "\r\n";
            settings.NewLineHandling = NewLineHandling.Replace;

            if (sp != null) {
                Settings s = (Settings)sp.GetService(typeof(Settings));
                if (s != null)
                {
                    settings.Indent = (bool)s["AutoFormatOnSave"];
                    IndentChar indentChar = (IndentChar)s["IndentChar"];
                    int indentLevel = (int)s["IndentLevel"];
                    char ch = (indentChar == IndentChar.Space) ? ' ' : '\t';
                    settings.IndentChars = new string(ch, indentLevel);
                    settings.NewLineChars = UserSettings.Unescape((string)s["NewLineChars"]);
                }
            }
        }

        public static string DefaultHelp
        {
            get
            {
                if (OnlineHelpAvailable)
                {
                    return "http://microsoft.github.io/XmlNotepad/help/overview";
                }
                else 
                { 
                    return Application.StartupPath + "\\Help\\Help.htm";
                }
            }
        }

        public static string OptionsHelp
        {
            get
            {
                if (OnlineHelpAvailable)
                {
                    return "http://microsoft.github.io/XmlNotepad/help/options";
                }
                else
                {
                    return Application.StartupPath + "\\Help\\Options.htm";
                }
            }
        }

        public static string SchemaHelp
        {
            get
            {
                if (OnlineHelpAvailable)
                {
                    return "http://microsoft.github.io/XmlNotepad/help/schemas";
                }
                else
                {
                    return Application.StartupPath + "\\Help\\Schemas.htm";
                }
            }
        }
        

        public static string FindHelp
        {
            get
            {
                if (OnlineHelpAvailable)
                {
                    return Application.StartupPath + "\\Help\\FindReplace.htm";
                }
                else
                {
                    return "http://microsoft.github.io/XmlNotepad/help/find";
                }
            }
        }

        public static bool OnlineHelpAvailable { get; set; }

        public static bool DynamicHelpEnabled { get; set; }


        public static void WriteFileWithoutBOM(MemoryStream ms, string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = new byte[16000];
                int len = ms.Read(bytes, 0, bytes.Length);

                int start = 0;
                Encoding sniff = SniffByteOrderMark(bytes, len);
                if (sniff != null)
                {
                    if (sniff == Encoding.UTF8)
                    {
                        start = 3;
                    }
                    else if (sniff == Encoding.GetEncoding(12001) || sniff == Encoding.UTF32)  // UTF-32.
                    {
                        start = 4;
                    }
                    else if (sniff == Encoding.Unicode || sniff == Encoding.BigEndianUnicode)  // UTF-16.
                    {
                        start = 2;
                    }
                }

                while (len > 0)
                {
                    fs.Write(bytes, start, len - start);
                    len = ms.Read(bytes, 0, bytes.Length);
                    start = 0;
                }
            }
        }

        internal static Encoding SniffByteOrderMark(byte[] bytes, int len)
        {
            if (len >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf)
            {
                return Encoding.UTF8;
            }
            else if (len >= 4 && ((bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xfe && bytes[3] == 0xff) || (bytes[0] == 0xfe && bytes[1] == 0xff && bytes[2] == 0xfe && bytes[3] == 0xff)))
            {
                return Encoding.GetEncoding(12001); // big endian UTF-32.
            }
            else if (len >= 4 && ((bytes[0] == 0xff && bytes[1] == 0xfe && bytes[2] == 0x00 && bytes[3] == 0x00) || (bytes[0] == 0xff && bytes[1] == 0xfe && bytes[2] == 0xff && bytes[3] == 0xfe)))
            {
                return Encoding.UTF32; // skip UTF-32 little endian BOM
            }
            else if (len >= 2 && bytes[0] == 0xff && bytes[1] == 0xfe)
            {
                return Encoding.Unicode; // skip UTF-16 little endian BOM
            }
            else if (len >= 2 && bytes[0] == 0xf2 && bytes[1] == 0xff)
            {
                return Encoding.BigEndianUnicode; // skip UTF-16 big endian BOM
            }
            return null;
        }

        // Lighten up the given baseColor so it is easy to read on the system Highlight color background.
        public static Brush HighlightTextBrush(Color baseColor) {
            SolidBrush ht = SystemBrushes.Highlight as SolidBrush;
            Color selectedColor = ht != null ? ht.Color : Color.FromArgb(49, 106, 197);
            HLSColor cls = new HLSColor(baseColor);
            HLSColor hls = new HLSColor(selectedColor);
            int luminosity = (hls.Luminosity > 120) ? 20 : 220;
            return new SolidBrush(HLSColor.ColorFromHLS(cls.Hue, luminosity, cls.Saturation));
        }


        public static void OpenUrl(IntPtr hwnd, string url) {
            const int SW_SHOWNORMAL = 1;
            ShellExecute(hwnd, "open", url, null, Application.StartupPath, SW_SHOWNORMAL);
        }

        [DllImport("Shell32.dll", EntryPoint = "ShellExecuteA",
             SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true,
             CallingConvention = CallingConvention.StdCall)]
        static extern int ShellExecute(IntPtr handle, string verb, string file,
            string args, string dir, int show);
    }

    public static class CurrentEvent {
        public static EventArgs Event;
    }


    [ClassInterface(ClassInterfaceType.None)]
    [ComImport]
    [Guid("1968106d-f3b5-44cf-890e-116fcb9ecef1")]
    [TypeLibType(TypeLibTypeFlags.FCanCreate)]
    internal sealed class ApplicationAssociationRegistrationUI : IApplicationAssociationRegistrationUI
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern void LaunchAdvancedAssociationUI(string appRegistryName);
    }

    [CoClass(typeof(ApplicationAssociationRegistrationUI))]
    [ComImport]
    [Guid("1f76a169-f994-40ac-8fc8-0959e8874710")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [TypeLibImportClass(typeof(ApplicationAssociationRegistrationUI))]
    internal interface IApplicationAssociationRegistrationUI
    {
        void LaunchAdvancedAssociationUI([MarshalAs(UnmanagedType.LPWStr)] string appRegistryName);
    }

}
