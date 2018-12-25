using Microsoft.Win32;
using mshtml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IRC_Client
{
    [ComVisible(true)]
    public partial class frmUI : Form
    {
        private IRC irc;
        private string nickname, activeWindow;
        private readonly char PREFIX = '/';
        private List<string> windows = new List<string>() { "main" };
        private Dictionary<string, string> topics = new Dictionary<string, string>() { { "main", "Server Window" } };
        public delegate void InvokeDelegate1(string arg);
        public delegate void InvokeDelegate(string arg, string arg2);

        public frmUI()
        {
            UseLatestIEVersion();
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            Size = Properties.Settings.Default.FormSize;
            if (!File.Exists("styles.css"))
                File.WriteAllText("styles.css", ReadEmbeddedFile("default_styles.css"));
            ChangeNick(Properties.Settings.Default.nick);
            canvas.DocumentText = "0";
            canvas.Document.OpenNew(true);
            canvas.ObjectForScripting = this;
            canvas.Document.Write(ReadEmbeddedFile("ui.html"));
            // Load internal layout CSS file
            HtmlElement css = canvas.Document.CreateElement("style");
            css.InnerHtml = ReadEmbeddedFile("layout.css");
            canvas.Document.GetElementsByTagName("head")[0].AppendChild(css);
            // Load external user CSS file
            css = canvas.Document.CreateElement("link");
            css.SetAttribute("rel", "stylesheet");
            css.SetAttribute("type", "text/css");
            css.SetAttribute("href", $"file://{Directory.GetCurrentDirectory()}/styles.css");
            canvas.Document.GetElementsByTagName("head")[0].AppendChild(css);
            // Load internal JS file
            HtmlElement js = canvas.Document.CreateElement("script");
            js.InnerHtml = ReadEmbeddedFile("scripts.js");
            canvas.Document.GetElementsByTagName("head")[0].AppendChild(js);
            SwitchTo("main");
            //webBrowser1.Document.Window.ScrollTo(0, webBrowser1.Document.Body.ScrollRectangle.Height);
        }

        public void Send(string input)
        {
            if (input == null) return;
            if (input[0] == PREFIX)
            {
                string[] tokens = input.Remove(0, 1).Split(' ');
                switch (tokens[0])
                {
                    case "server":
                        canvas.BeginInvoke(new InvokeDelegate(WriteLine), "main", "<span class=\"info\">* Connecting...</span>");
                        irc = new IRC();
                        AttachEvents();
                        irc.Connect(tokens[1], 6667, nickname);
                        break;
                    default:
                        if (irc.isConnected)
                        {
                            irc.Write(input.Remove(0, 1));
                            Console.WriteLine("-> " + input);
                        }
                        break;
                }
            }
            else
            {
                if (irc != null && irc.isConnected && !activeWindow.Equals("main"))
                {
                    irc.Write($"PRIVMSG #{activeWindow} :{input}");
                    WriteLine(activeWindow, $"<span class=\"chat_nick\">&lt;{nickname}&gt;</span> {input}");
                    Console.WriteLine($"-> PRIVMSG #{activeWindow} :{input}");
                }
            }
        }

        // Events
        private void OnNotice(object sender, TokenEventArgs e)
        {
            WriteLineA("main", $"-<span class=\"notice_nick\">{e.tokens[0].Substring(1)}</span>- {FormatString(e.tokens)}");
        }

        private void OnNick(object sender, TokenEventArgs e)
        {
            if (NoColon(e.tokens[0]).Substring(0, nickname.Length).Equals(nickname))
            {
                ChangeNick(e.tokens[2]);
                WriteLineA("main", $"* Your nickname is now {nickname}");
            }
        }

        private void OnJoin(object sender, TokenEventArgs e)
        {
            string channelName = NoColon(e.tokens[2]).Remove(0, 1); // rem #
            if (NoColon(e.tokens[0]).Substring(0, nickname.Length).Equals(nickname))
            {
                windows.Add(channelName);
                canvas.BeginInvoke(new InvokeDelegate1(MakeWindow), channelName);
                WriteLineA(channelName, $"<span class=\"info\">* You have joined #{channelName}</span>");
            }
            else
                if (windows.Contains(channelName))
                WriteLineA(channelName, $"<span class=\"info\">* {NoColon(e.tokens[0])} has joined #{channelName}</span>");
        }

        private void OnQuit(object sender, TokenEventArgs e)
        {
            string nick = NoColon(e.tokens[0]).Split('!')[0];
            canvas.BeginInvoke(new InvokeDelegate1(RemoveFromUserList), nick);
        }

        private void RemoveFromUserList(string nick)
        {
            foreach (string w in windows)
                if (canvas.Document.GetElementById($"{w}_{nick}") != null)
                {
                    RemoveById($"{w}_{nick}");
                    WriteLineA(w, $"<span class=\"info\">* {nick} has quit</span>");
                }
        }

        private void OnPrivMsg(object sender, TokenEventArgs e)
        {
            string channelName = NoColon(e.tokens[2]).Remove(0, 1); // rem #
            channelName = e.tokens[2].Remove(0, 1);
            if (windows.Contains(channelName))
            {
                WriteLineA(channelName, $"<span class=\"chat_nick\">&lt;{NoColon(e.tokens[0]).Split('!')[0]}&gt;</span> {NoColon(string.Join(" ", e.tokens.Skip(3).ToArray()))}");
                canvas.BeginInvoke(new InvokeDelegate1(ChannelListNotify), channelName);
            }
        }

        private void ChannelListNotify(string channelName)
        {
            string currentClass = canvas.Document.GetElementById($"{channelName}_link").GetAttribute("className");
            if (!activeWindow.Equals(channelName))
                if (!currentClass.Contains("channels_unread"))
                    canvas.Document.GetElementById($"{channelName}_link").SetAttribute("className", currentClass + " channels_unread");
        }

        private void OnTopic(object sender, TokenEventArgs e)
        {
            topics.Add(e.tokens[3].Remove(0, 1), NoColon(string.Join(" ", e.tokens.Skip(4).ToArray())));
        }

        private void OnUser(object sender, TokenEventArgs e)
        {
            string channelName = e.tokens[4].Remove(0, 1);
            if (windows.Contains(channelName))
            {
                e.tokens[5] = NoColon(e.tokens[5]);
                foreach (string u in e.tokens.Skip(5).Where(x => !string.IsNullOrEmpty(x)).ToArray())
                    canvas.BeginInvoke(new InvokeDelegate(AddUserToList), channelName, u);
            }
        }

        private void AddUserToList(string channelName, string u)
        {
            HtmlElement ul = canvas.Document.CreateElement("li");
            ul.SetAttribute("id", $"{channelName}_{u}");
            ul.InnerHtml = $"{u}";
            canvas.Document.GetElementById($"{channelName}_users_list").AppendChild(ul);
        }

        private void OnDefault(object sender, TokenEventArgs e)
        {
            if (e.tokens.Length > 2)
                WriteLineA("main", $"-<span class=\"notice_nick\">Server</span>- {FormatString(e.tokens)}");
        }

        private void OnDisconnect(object sender, EventArgs e)
        {
            canvas.BeginInvoke(new InvokeDelegate(WriteLine), "main", "<span class=\"info\">* DISCONNECTED</span>");
        }

        private void ChangeNick(string nick)
        {
            nickname = NoColon(nick);
            Text = $"IRC Client - {nickname}";
        }

        private void WriteLine(string window, string text)
        {
            HtmlElement tmp = canvas.Document.CreateElement("p");
            tmp.InnerHtml = text + "\n";
            canvas.Document.GetElementById(window).AppendChild(tmp);
            //webBrowser1.Document.Window.ScrollTo(0, webBrowser1.Document.Body.ScrollRectangle.Height);
            canvas.Document.InvokeScript("scroll");
            canvas.Update();
        }

        private void WriteLineA(string window, string text)
        {
            canvas.BeginInvoke(new InvokeDelegate(WriteLine), window, text);
        }

        private string FormatString(string[] input)
        {
            string tmp = "";
            for (int i = 3; i < input.Length; i++)
                tmp += " " + input[i];
            tmp = tmp.TrimStart();
            if (tmp[0] == ':') tmp = tmp.Remove(0, 1);  //remove ':'
            return tmp;
        }

        private void MakeWindow(string windowName)
        {
            HtmlElement channelDiv = canvas.Document.CreateElement("div");
            channelDiv.SetAttribute("id", windowName);
            channelDiv.SetAttribute("className", "window");
            channelDiv.Style = "display:none";
            canvas.Document.GetElementsByTagName("body")[0].AppendChild(channelDiv);

            HtmlElement channelLink = canvas.Document.CreateElement("a");
            channelLink.SetAttribute("id", $"{windowName}_link");
            channelLink.SetAttribute("className", "channel_link");
            channelLink.InnerHtml = $"#{windowName}";
            canvas.Document.GetElementById("channel_list").AppendChild(channelLink);
            foreach (HtmlElement el in canvas.Document.GetElementsByTagName("a"))
            {
                if (el.Id != null && el.Id.Equals($"{windowName}_link"))
                    el.AttachEventHandler("onclick", (sender1, e1) => ClickEventHandler(el, EventArgs.Empty));
            }

            /* HtmlElement channelX = ui.Document.CreateElement("span");
            channelX.SetAttribute("className", "close");
            channelX.InnerHtml = "X";
            ui.Document.GetElementById($"{windowName}_link").AppendChild(channelX); */

            HtmlElement br = canvas.Document.CreateElement("br");
            canvas.Document.GetElementById("channel_list").AppendChild(br);

            HtmlElement userDiv = canvas.Document.CreateElement("div");
            userDiv.SetAttribute("id", $"{windowName}_users");
            userDiv.SetAttribute("className", "sider");
            userDiv.Style = "display:none";
            canvas.Document.GetElementsByTagName("body")[0].AppendChild(userDiv);
            HtmlElement ul = canvas.Document.CreateElement("ul");
            ul.SetAttribute("id", $"{windowName}_users_list");
            canvas.Document.GetElementById($"{windowName}_users").AppendChild(ul);

            windows.Add(windowName);
            SwitchTo(windowName);
        }

        public void ClickEventHandler(object sender, EventArgs e)
        {
            SwitchTo(((HtmlElement)sender).InnerHtml.Remove(0, 1));
        }

        public void SwitchTo(string window)
        {
            if (windows.Contains(window))
            {
                foreach (var w in windows)
                {
                    canvas.Document.GetElementById(w).Style = "display:none";
                    canvas.Document.GetElementById($"{w}_users").Style = "display:none";
                }
                canvas.Document.GetElementById(window).Style = "display:block";
                canvas.Document.GetElementById($"{window}_users").Style = "display:block";
                // todo: add scroll
                activeWindow = window;

                // unread channel message notify
                if (!window.Equals("main"))
                {
                    string currentClass = canvas.Document.GetElementById($"{window}_link").GetAttribute("className");
                    if (currentClass.Contains("channels_unread"))
                        canvas.Document.GetElementById($"{window}_link").SetAttribute("className", "");
                }

                // update topic
                if (topics.ContainsKey(window))
                {
                    canvas.Document.GetElementById("topic").InnerHtml = (activeWindow.Equals("main")) ? topics[window] : $"#{window} | {topics[window]}";
                }
            }
        }

        private string ReadEmbeddedFile(string file)
        {
            string result;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"IRC_Client.resources.{file}"))
            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
                reader.Close();
            }
            return result;
        }

        private void AttachEvents()
        {
            irc.NoticeEvent += OnNotice;
            irc.NickEvent += OnNick;
            irc.JoinEvent += OnJoin;
            irc.QuitEvent += OnQuit;
            irc.PrivMsgEvent += OnPrivMsg;
            irc.TopicEvent += OnTopic;
            irc.UserEvent += OnUser;
            irc.DefaultEvent += OnDefault;
            irc.Disconnect += OnDisconnect;
        }

        private string NoColon(string input) => (input[0] == ':') ? input.Remove(0, 1) : input;

        /// <summary>
        /// Forces WebBrowser control to use the latest version of Internet Explorer that is installed on current machine
        /// Source: https://stackoverflow.com/questions/17922308/use-latest-version-of-internet-explorer-in-the-webbrowser-control/34267121#34267121
        /// </summary>
        private static void UseLatestIEVersion()
        {
            int BrowserVer, RegVal;

            // get the installed IE version
            using (WebBrowser Wb = new WebBrowser())
                BrowserVer = Wb.Version.Major;

            // set the appropriate IE version
            if (BrowserVer >= 11)
                RegVal = 11001;
            else if (BrowserVer == 10)
                RegVal = 10001;
            else if (BrowserVer == 9)
                RegVal = 9999;
            else if (BrowserVer == 8)
                RegVal = 8888;
            else
                RegVal = 7000;

            // set the actual key
            using (RegistryKey Key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", RegistryKeyPermissionCheck.ReadWriteSubTree))
                if (Key.GetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe") == null)
                    Key.SetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe", RegVal, RegistryValueKind.DWord);
        }

        private void RemoveById(string id)
        {
            HTMLDocument tmp = (HTMLDocument)canvas.Document.DomDocument;
            IHTMLDOMNode node = tmp.getElementById(id) as IHTMLDOMNode;
            node.parentNode.removeChild(node);
        }

        private IEnumerable<IHTMLDOMNode> GetElementsByClass(HTMLDocument doc, string className)
        {
            foreach (IHTMLDOMNode e in doc.all)
                if (e.parentNode.attributes("className").Contains(className))
                    yield return e;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Console.WriteLine(canvas.Document.GetElementsByTagName("html")[0].InnerHtml);
        }

        private void Resized(object sender, EventArgs e)
        {
            try
            {
                canvas.Document.InvokeScript("scroll");
            }
            catch { }
        }

        private void Closing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.nick = nickname;
            Properties.Settings.Default.FormSize = Size;
            Properties.Settings.Default.Location = Location;
            Properties.Settings.Default.Save();
        }
    }
}