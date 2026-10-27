using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace SpokenFlow {
    public static class L {
        private static readonly Dictionary<string,Dictionary<string,string>> catalog=Load();
        private static volatile string language="en";
        private static event Action Changed;
        public static string Language { get { return language; } }
        private static Dictionary<string,Dictionary<string,string>> Load() {
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("SpokenFlow.Locales.json")) {
                if(stream==null) throw new InvalidOperationException("Missing bundled UI translations.");
                using(var reader=new StreamReader(stream)) return new JavaScriptSerializer().Deserialize<Dictionary<string,Dictionary<string,string>>>(reader.ReadToEnd());
            }
        }
        public static void SetLanguage(string code) {
            language=Array.IndexOf(LanguagePair.Codes,code)>=0?code:"en";
            if(Changed!=null) Changed();
        }
        public static string T(string source) { return In(language,source); }
        public static string In(string code,string source) {
            Dictionary<string,string> row; string value;
            if(catalog.TryGetValue(source,out row)) {
                if(row.TryGetValue(code,out value)) return value;
                if(row.TryGetValue("en",out value)) return value;
            }
            return source;
        }
        public static bool Complete {
            get { foreach(var row in catalog.Values) foreach(string code in LanguagePair.Codes) if(!row.ContainsKey(code) || String.IsNullOrWhiteSpace(row[code])) return false; return catalog.Count>200; }
        }
        // Translate only known UI labels, never editable user text or model output.
        private static string Known(string value) {
            if(String.IsNullOrEmpty(value)) return value;
            foreach(var row in catalog) {
                if(row.Key==value) return T(row.Key);
                foreach(string translation in row.Value.Values) if(translation==value) return T(row.Key);
            }
            return value;
        }
        public static void RefreshMenu(ToolStrip menu) {
            foreach(ToolStripItem item in menu.Items) {
                item.Text=Known(item.Text);
                var drop=item as ToolStripDropDownItem;
                if(drop!=null) RefreshMenu(drop.DropDown);
            }
        }
        private static void Refresh(Control control) {
            // Result and input content must not be relocalized.
            control.AccessibleName=Known(control.AccessibleName);
            if(control is TextBoxBase || control is ComboBox || control is TranslationView) return;
            control.SuspendLayout();
            control.Text=Known(control.Text);
            foreach(Control child in control.Controls) Refresh(child);
            control.ResumeLayout(true);
        }
        public static void Register(Form form) {
            Action refresh=delegate { if(!form.IsDisposed) Refresh(form); };
            Changed+=refresh;
            form.Disposed+=delegate { Changed-=refresh; };
        }
        public static void SetToolTip(ToolTip tips,Control control,string value) {
            Action update=delegate { if(!control.IsDisposed) tips.SetToolTip(control,Known(value)); };
            update(); Changed+=update; control.Disposed+=delegate { Changed-=update; };
        }
    }
}
