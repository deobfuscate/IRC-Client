using Microsoft.Win32;
using mshtml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IRC_Client
{
    [ComVisible(true)]
    public partial class frmServer : Form
    {
        private StreamWriter writer;
        private static bool isConnected = false;
        private const char PREFIX = '/';
        private readonly List<string> SERVER_CODES = new List<string>() { "001", "002", "003", "004", "005", "251", "252", "254", "255", "375", "372" };
        public delegate void InvokeDelegate1(string arg);
        public delegate void InvokeDelegate(string arg, string arg2);
        private string nickname = "UniqueNickname";
        private List<string> windows = new List<string>() { "main" };
        private Dictionary<string, string> topics = new Dictionary<string, string>() { { "main", "Server Window" } };
        private string activeWindow;

        public frmServer()
        {
            UseLatestIEVersion();
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            ui.DocumentText = "0";
            ui.Document.OpenNew(true);
            ui.ObjectForScripting = this;
            ui.Document.Write(ReadEmbeddedFile("ui2.html"));
            // Load external CSS file
            HtmlElement css = ui.Document.CreateElement("link");
            css.SetAttribute("rel", "stylesheet");
            css.SetAttribute("type", "text/css");
            css.SetAttribute("href", $"file://{Directory.GetCurrentDirectory()}/styles.css");
            ui.Document.GetElementsByTagName("head")[0].AppendChild(css);
            //webBrowser1.Document.Window.ScrollTo(0, webBrowser1.Document.Body.ScrollRectangle.Height);
        }

        private void CallbackMethod(IAsyncResult ar)
        {
            ui.BeginInvoke(new InvokeDelegate(WriteLine), "main", "Connecting...");
            try
            {
                isConnected = false;
                TcpClient tcpclient = ar.AsyncState as TcpClient;

                if (tcpclient.Client != null)
                {
                    tcpclient.EndConnect(ar);
                    isConnected = true;

                    NetworkStream stream = tcpclient.GetStream();
                    StreamReader reader = new StreamReader(stream);
                    writer = new StreamWriter(stream);
                    string inputLine;
                    writer.WriteLine("USER default 0 * :default");
                    writer.Flush();
                    writer.WriteLine("NICK UniqueNickname");
                    writer.Flush();
                    while(true)
                    {
                        while ((inputLine = reader.ReadLine()) != null)
                        {
                            Console.WriteLine("<- " + inputLine);
                            string[] tokens = inputLine.Split(new Char[] { ' ' });
                            if (tokens.Length < 2) continue;
                            if (tokens[0] == "PING")
                            {
                                string key = tokens[1];
                                Console.WriteLine("-> PONG " + key);
                                writer.WriteLine("PONG " + key);
                                writer.Flush();
                            }
                            if (tokens[1] == "NOTICE")
                            {
                                WriteLineA("main", $"-<span class=\"notice_nick\">{tokens[0].Substring(1)}</span>- {FormatString(tokens)}");
                            }
                            if (tokens[1] == "JOIN")
                            {
                                string channelName = NoColon(tokens[2]).Remove(0, 1); // rem #
                                if (NoColon(tokens[0]).Substring(0, nickname.Length).Equals(nickname))
                                {
                                    ui.BeginInvoke(new InvokeDelegate1(MakeWindow), channelName);
                                    windows.Add(channelName);
                                    WriteLineA(channelName, $"* You have joined #{channelName}");
                                }
                                else
                                    if (windows.Contains(channelName))
                                        WriteLineA(channelName, $"* {NoColon(tokens[0])} has joined #{channelName}");
                            }
                            if (tokens[1] == "PRIVMSG")
                            {
                                string channelName = tokens[2].Remove(0, 1);
                                if (windows.Contains(channelName))
                                {
                                    WriteLineA(channelName, $"<span class=\"chat_nick\">&lt;{NoColon(tokens[0]).Split('!')[0]}&gt;</span> {NoColon(string.Join(" ", tokens.Skip(3).ToArray()))}");
                                    ui.BeginInvoke(new InvokeDelegate1(ChannelListNotify), channelName);
                                }
                            }
                            // topic
                            if (tokens[1] == "332")
                            {
                                topics.Add(tokens[3].Remove(0, 1), NoColon(string.Join(" ", tokens.Skip(4).ToArray())));
                                //if (windows.Contains(splitInput[3].Remove(0, 1)))
                                //{
                                //    WriteLineA(splitInput[3].Remove(0, 1), $"* Topic is: {NoColon(string.Join(" ", splitInput.Skip(4).ToArray()))}");
                                //}
                            }
                            // userlist
                            if (tokens[1] == "353")
                            {
                                string channelName = tokens[4].Remove(0, 1);
                                if (windows.Contains(channelName))
                                {
                                    tokens[5] = NoColon(tokens[5]);
                                    foreach (string u in tokens.Skip(5).Where(x => !string.IsNullOrEmpty(x)).ToArray())
                                    //WriteLineA($"{channelName}_users", $"<span class=\"user_{u}\">{u}</span>");
                                    {
                                        ui.BeginInvoke(new InvokeDelegate(AddUserToList), channelName, u);
                                    }
                                }
                            }

                            if (tokens[1] == "001")
                            {
                                nickname = tokens[2];
                            }
                            if (tokens[1] == "QUIT")
                            {
                                string nick = NoColon(tokens[0]).Split('!')[0];
                                ui.BeginInvoke(new InvokeDelegate1(RemoveFromUserList), nick);
                            }
                            if (tokens[1] == "ERROR")
                            {
                                // todo
                            }
                            if (inputLine.Length > 1)
                            {
                                if (SERVER_CODES.Contains(tokens[1]))
                                {
                                    WriteLineA("main", $"-<span class=\"notice_nick\">Server</span>- {FormatString(tokens)}");
                                }
                            }
                        }
                        writer.Close();
                        reader.Close();
                        tcpclient.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                isConnected = false;
                Console.WriteLine(ex);
                //writeLine("<- <span color='red'>DISCONNECTED</span>");
            }
        }

        private void AddUserToList(string channelName, string u)
        {
            HtmlElement ul = ui.Document.CreateElement("li");
            ul.SetAttribute("className", $"user_{u}");
            ul.InnerHtml = $"{u}";
            ui.Document.GetElementById($"{channelName}_users_list").AppendChild(ul);
        }

        private void RemoveFromUserList(string nick)
        {
            //foreach (IHTMLDOMNode x in GetElementsByClass((HTMLDocument)ui.Document.DomDocument, $"user_{nick}"))
            //    x.parentNode.removeNode();
            HTMLDocument tmp = (HTMLDocument)ui.Document.DomDocument;

            foreach (IHTMLDOMNode el in tmp.getElementsByTagName("li"))
            {
                if (el.nodeValue.getAttribute("className").Equals($"user_{nick}")) {
                    el.parentNode.removeChild(el);
                }
            }
        }

        private void ChannelListNotify(string channelName)
        {
            string currentClass = ui.Document.GetElementById($"{channelName}_link").GetAttribute("className");
            if (!activeWindow.Equals(channelName))
                if (!currentClass.Contains("channels_unread"))
                    ui.Document.GetElementById($"{channelName}_link").SetAttribute("className", currentClass + " channels_unread");
        }

        public void Send(string input)
        {
            if (input == null) return;
            if (input[0] == PREFIX)
            {
                string cmd = input.Remove(0, 1);
                string[] parts = cmd.Split(' ');
                switch (parts[0])
                {
                    case "server":
                        TcpClient irc = new TcpClient();
                        irc.BeginConnect(parts[1], 6667, CallbackMethod, irc);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                if (isConnected)
                {
                    writer.WriteLine(input);
                    writer.Flush();
                    Console.WriteLine("-> " + input);
                }
                else
                {
                    WriteLine("main", "* You are not connected to a server");
                }
            }
        }

        private void WriteLine(string window, string text)
        {
            HtmlElement tmp = ui.Document.CreateElement("span");
            tmp.InnerHtml = text + "<br>\n";
            ui.Document.GetElementById(window).AppendChild(tmp);
            //webBrowser1.Document.Window.ScrollTo(0, webBrowser1.Document.Body.ScrollRectangle.Height);
            ui.Document.InvokeScript("scroll");
            ui.Update();
        }

        private void WriteLineA(string window, string text)
        {
            ui.BeginInvoke(new InvokeDelegate(WriteLine), window, text);
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

        private string ReadEmbeddedFile(string file)
        {
            string result;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"IRC_Client.{file}"))
            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
                reader.Close();
            }
            return result;
        }

        private void Resized(object sender, EventArgs e)
        {
            try
            {
                ui.Document.InvokeScript("scroll");
            }
            catch { }
        }

        private void MakeWindow(string windowName)
        {
            HtmlElement channelDiv = ui.Document.CreateElement("div");
            channelDiv.SetAttribute("id", windowName);
            channelDiv.SetAttribute("className", "window");
            channelDiv.Style = "display:none";
            ui.Document.GetElementsByTagName("body")[0].AppendChild(channelDiv);

            HtmlElement channelLink = ui.Document.CreateElement("a");
            channelLink.SetAttribute("id", $"{windowName}_link");
            channelLink.InnerHtml = $"#{windowName}";
            ui.Document.GetElementById("channel_list").AppendChild(channelLink);
            foreach (HtmlElement el in ui.Document.GetElementsByTagName("a"))
            {
                if (el.Id != null && el.Id.Equals($"{windowName}_link"))
                    el.AttachEventHandler("onclick", (sender1, e1) => ClickEventHandler(el, EventArgs.Empty));
            }

            HtmlElement br = ui.Document.CreateElement("br");
            ui.Document.GetElementById("channel_list").AppendChild(br);

            HtmlElement userDiv = ui.Document.CreateElement("div");
            userDiv.SetAttribute("id", $"{windowName}_users");
            userDiv.SetAttribute("className", "sider");
            userDiv.Style = "display:none";
            ui.Document.GetElementsByTagName("body")[0].AppendChild(userDiv);
            HtmlElement ul = ui.Document.CreateElement("ul");
            ul.SetAttribute("id", $"{windowName}_users_list");
            ui.Document.GetElementById($"{windowName}_users").AppendChild(ul);

            windows.Add(windowName);
            SwitchTo(windowName);
        }

        public void ClickEventHandler(object sender, EventArgs e)
        {
            var tmp = (HtmlElement)sender;
            SwitchTo(tmp.InnerHtml.Remove(0, 1));
        }

        public void SwitchTo(string window)
        {
            if (windows.Contains(window))
            {
                foreach (var w in windows)
                {
                    ui.Document.GetElementById(w).Style = "display:none";
                    ui.Document.GetElementById($"{w}_users").Style = "display:none";
                }
                ui.Document.GetElementById(window).Style = "display:block";
                ui.Document.GetElementById($"{window}_users").Style = "display:block";
                // add scroll
                activeWindow = window;
                // unread channel message notify
                if (!window.Equals("main"))
                {
                    string currentClass = ui.Document.GetElementById($"{window}_link").GetAttribute("className");
                    if (currentClass.Contains("channels_unread"))
                        ui.Document.GetElementById($"{window}_link").SetAttribute("className", "");
                }

                // update topic
                if (topics.ContainsKey(window))
                {
                    ui.Document.GetElementById("topic").InnerHtml = $"#{window} | {topics[window]}";
                }
            }
        }

        private string NoColon(string input) => (input[0]==':') ? input.Remove(0, 1) : input;

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
            HTMLDocument tmp = (HTMLDocument)ui.Document.DomDocument;
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
            Console.WriteLine(ui.Document.GetElementsByTagName("html")[0].InnerHtml);
        }
    }
}