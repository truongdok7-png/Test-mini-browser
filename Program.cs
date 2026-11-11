using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace MiniBrowser
{
    public class MainForm : Form
    {
        private TabControl tabControl;
        private TextBox txtUrl;
        private string tempProfilePath;

        public MainForm()
        {
            this.Text = "Mini Incognito Browser (WebView2)";
            this.Width = 1024;
            this.Height = 768;

            // 1. Tạo đường dẫn tạm thời để chứa dữ liệu duyệt web (Cookie, Cache...)
            tempProfilePath = Path.Combine(Path.GetTempPath(), "MiniBrowser_" + Guid.NewGuid().ToString());

            // 2. Thiết kế Giao diện (UI) bằng code
            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 35 };
            
            txtUrl = new TextBox { Left = 10, Top = 5, Width = 800, Font = new System.Drawing.Font("Arial", 10) };
            txtUrl.KeyDown += TxtUrl_KeyDown;
            
            Button btnNewTab = new Button { Text = "+ Tab", Left = 820, Top = 4, Width = 60 };
            btnNewTab.Click += (s, e) => AddTab("https://www.google.com");

            Button btnCloseTab = new Button { Text = "X Đóng", Left = 890, Top = 4, Width = 60 };
            btnCloseTab.Click += (s, e) => { 
                if (tabControl.TabPages.Count > 1) tabControl.TabPages.Remove(tabControl.SelectedTab); 
            };

            topPanel.Controls.Add(txtUrl);
            topPanel.Controls.Add(btnNewTab);
            topPanel.Controls.Add(btnCloseTab);

            tabControl = new TabControl { Dock = DockStyle.Fill };
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            this.Controls.Add(tabControl);
            this.Controls.Add(topPanel);

            // 3. Sự kiện dọn dẹp khi tắt App (Xóa toàn bộ dấu vết)
            this.FormClosed += (s, e) => {
                try { Directory.Delete(tempProfilePath, true); } catch { }
            };

            // Mở tab đầu tiên
            AddTab("https://www.google.com");
        }

        private async void AddTab(string url)
        {
            TabPage page = new TabPage("Đang tải...");
            WebView2 webView = new WebView2 { Dock = DockStyle.Fill };
            page.Controls.Add(webView);
            tabControl.TabPages.Add(page);
            tabControl.SelectedTab = page;

            // Áp dụng Profile ẩn danh tạm thời cho WebView2
            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: tempProfilePath);
            await webView.EnsureCoreWebView2Async(env);

            // Cập nhật tiêu đề tab và thanh địa chỉ
            webView.CoreWebView2.DocumentTitleChanged += (s, e) => {
                string title = webView.CoreWebView2.DocumentTitle;
                page.Text = title.Length > 15 ? title.Substring(0, 15) + "..." : title;
            };
            
            webView.CoreWebView2.SourceChanged += (s, e) => { 
                if (tabControl.SelectedTab == page) txtUrl.Text = webView.CoreWebView2.Source; 
            };

            webView.CoreWebView2.Navigate(url);
        }

        private void TxtUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Chặn tiếng "Bíp" của Windows
                if (tabControl.SelectedTab?.Controls[0] is WebView2 webView && webView.CoreWebView2 != null)
                {
                    string input = txtUrl.Text.Trim();
                    // Tự động nhận diện URL hoặc Từ khóa tìm kiếm
                    if (!input.StartsWith("http"))
                        input = input.Contains(".") && !input.Contains(" ") ? "https://" + input : "https://www.google.com/search?q=" + Uri.EscapeDataString(input);
                    
                    webView.CoreWebView2.Navigate(input);
                }
            }
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab?.Controls[0] is WebView2 webView && webView.CoreWebView2 != null)
                txtUrl.Text = webView.CoreWebView2.Source;
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
