using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace SpokenFlow {
    public sealed class AppearanceData {
        public int Width=400, Height=230, Opacity=94, Background=Color.FromArgb(23,34,52).ToArgb();
        public int X,Y;
        public bool Pinned, SelectionEnabled;
        public bool SelectionClipboardFallback=true;
    }
    public static class Appearance {
        public static AppearanceData Current=Load();
        public static event Action Changed;
        private static string FileName { get { return Path.Combine(Settings.Folder,"appearance.json"); } }
        private static AppearanceData Load() {
            try {
                if(File.Exists(FileName)) return Normalize(new JavaScriptSerializer().Deserialize<AppearanceData>(File.ReadAllText(FileName,Encoding.UTF8)));
                string old=Path.Combine(Settings.Folder,"bubble-position.json");
                var value=new AppearanceData();
                if(File.Exists(old)) {
                    var p=new JavaScriptSerializer().Deserialize<AppearanceData>(File.ReadAllText(old,Encoding.UTF8));
                    value.Pinned=p.Pinned; value.X=p.X; value.Y=p.Y;
                }
                return value;
            } catch { return new AppearanceData(); }
        }
        public static AppearanceData Normalize(AppearanceData d) {
            if(d==null) d=new AppearanceData();
            d.Width=Math.Max(320,Math.Min(1200,d.Width)); d.Height=Math.Max(180,Math.Min(900,d.Height));
            d.Opacity=Math.Max(55,Math.Min(100,d.Opacity));
            d.Background=Color.FromArgb(255,Color.FromArgb(d.Background)).ToArgb(); return d;
        }
        public static Color Background { get { return Color.FromArgb(Current.Background); } }
        private static double Linear(byte channel) { double v=channel/255.0; return v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4); }
        public static bool Light { get { Color c=Background; return .2126*Linear(c.R)+.7152*Linear(c.G)+.0722*Linear(c.B)>.179; } }
        public static Color Foreground { get { return Light?Color.FromArgb(30,43,53):Color.FromArgb(242,246,250); } }
        public static Color Muted { get { return Light?Color.FromArgb(92,109,119):Color.FromArgb(155,175,190); } }
        public static Color Accent { get { return Light?Color.FromArgb(5,110,99):Color.FromArgb(105,218,192); } }
        public static void Notify() { Current=Normalize(Current); if(Changed!=null) Changed(); }
        public static void Save() {
            string temp=null;
            try {
                Directory.CreateDirectory(Settings.Folder); temp=FileName+"."+Guid.NewGuid().ToString("N")+".tmp";
                File.WriteAllText(temp,new JavaScriptSerializer().Serialize(Current),Encoding.UTF8);
                if(File.Exists(FileName)) File.Replace(temp,FileName,null); else File.Move(temp,FileName);
            } catch { }
        }
    }
    public sealed class AppearanceDialog : Form {
        public AppearanceDialog() {
            Text=L.T("浮窗外观"); ClientSize=new Size(560,355); FormBorderStyle=FormBorderStyle.FixedDialog;
            StartPosition=FormStartPosition.CenterScreen; MaximizeBox=false; MinimizeBox=false; TopMost=true; Icon=AppIcon.Load();
            Font=new Font("Microsoft YaHei UI",10); BackColor=Color.FromArgb(246,248,250);
            var caption=new Label { Text=L.T("不透明度"),Location=new Point(22,22),Size=new Size(150,26) };
            var amount=new Label { Location=new Point(349,22),Size=new Size(80,26),Text=Appearance.Current.Opacity+"%" };
            var slider=new TrackBar { Minimum=55,Maximum=100,TickStyle=TickStyle.None,Value=Appearance.Current.Opacity,Location=new Point(18,58),Size=new Size(410,38),AccessibleName=L.T("浮窗不透明度") };
            slider.ValueChanged+=delegate { Appearance.Current.Opacity=slider.Value; amount.Text=slider.Value+"%"; Appearance.Notify(); };
            Controls.AddRange(new Control[]{caption,amount,slider,new Label { Text=L.T("背景颜色（文字颜色自动匹配）"),Location=new Point(22,109),Size=new Size(510,26) }});
            string[] names={L.T("深蓝"),L.T("墨绿"),L.T("浅色"),L.T("石墨")}; Color[] colors={Color.FromArgb(23,34,52),Color.FromArgb(20,61,56),Color.FromArgb(250,250,247),Color.FromArgb(37,37,43)};
            for(int i=0;i<4;i++) { int index=i; var b=new Button { Text=names[i],Location=new Point(22+i*130,145),Size=new Size(120,35),FlatStyle=FlatStyle.Flat }; b.Click+=delegate { Appearance.Current.Background=colors[index].ToArgb(); Appearance.Notify(); }; Controls.Add(b); }
            var custom=new Button { Text=L.T("自定义颜色…"),Location=new Point(22,203),Size=new Size(210,38) };
            custom.Click+=delegate { using(var picker=new ColorDialog { Color=Appearance.Background,FullOpen=true }) { if(picker.ShowDialog(this)==DialogResult.OK) { Appearance.Current.Background=picker.Color.ToArgb(); Appearance.Notify(); } } };
            var done=new Button { Text=L.T("保存并关闭"),Location=new Point(325,203),Size=new Size(210,38),DialogResult=DialogResult.OK };
            Controls.AddRange(new Control[]{custom,done}); AcceptButton=done;
            var selectionCopy=new CheckBox { Text=L.T("划词读取失败时，尝试复制选区并恢复剪贴板"),AutoSize=false,Size=new Size(510,65),Location=new Point(22,265),Checked=Appearance.Current.SelectionClipboardFallback };
            selectionCopy.CheckedChanged+=delegate { Appearance.Current.SelectionClipboardFallback=selectionCopy.Checked; };
            Controls.Add(selectionCopy);
            L.Register(this); FormClosed+=delegate { Appearance.Save(); };
        }
    }
}
