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
        private string activeWindow;

        public frmServer()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            webBrowser1.DocumentText = "0";
            webBrowser1.Document.OpenNew(true);
            webBrowser1.ObjectForScripting = this;
            webBrowser1.Document.Write(ReadEmbeddedFile("ui.html"));
            // Load external CSS file
            HtmlElement css = webBrowser1.Document.CreateElement("link");
            css.SetAttribute("rel", "stylesheet");
            css.SetAttribute("type", "text/css");
            css.SetAttribute("href", $"file://{Directory.GetCurrentDirectory()}/styles.css");
            webBrowser1.Document.GetElementsByTagName("head")[0].AppendChild(css);
            var ree = webBrowser1.Document.GetElementsByTagName("head")[0].DomElement;
            //webBrowser1.Document.Window.ScrollTo(0, webBrowser1.Document.Body.ScrollRectangle.Height);
        }

        private void CallbackMethod(IAsyncResult ar)
        {
            webBrowser1.BeginInvoke(new InvokeDelegate(writeLine), "main", "Connecting...");
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
                            string[] splitInput = inputLine.Split(new Char[] { ' ' });
                            if (splitInput.Length < 2)
                                continue;
                            if (splitInput[0] == "PING")
                            {
                                string key = splitInput[1];
                                Console.WriteLine("-> PONG " + key);
                                writer.WriteLine("PONG " + key);
                                writer.Flush();
                            }
                            if (splitInput[1] == "NOTICE")
                            {
                                writeLineA("main", $"-<span class=\"notice_nick\">{splitInput[0].Substring(1)}</span>- {formatString(splitInput)}");
                            }
                            if (splitInput[1] == "JOIN")
                            {
                                // todo: filter by current nick
                                string channelName = noColon(splitInput[2]).Remove(0, 1); // rem #
                                webBrowser1.BeginInvoke(new InvokeDelegate1(makeWindow), channelName);
                                windows.Add(channelName);
                                writeLineA(channelName, $"* You have joined #{channelName}");
                            }
                            if (splitInput[1] == "PRIVMSG")
                            {
                                if (windows.Contains(splitInput[3].Remove(0, 1)))
                                {
                                    writeLineA(splitInput[3].Remove(0, 1), $"<{noColon(splitInput[0])}> {noColon(string.Join(" ", splitInput.Skip(4).ToArray()))}");
                                }
                            }
                            // topic
                            if (splitInput[1] == "332")
                            {
                                if (windows.Contains(splitInput[3].Remove(0, 1)))
                                {
                                    writeLineA(splitInput[3].Remove(0, 1), $"* Topic is: {noColon(string.Join(" ", splitInput.Skip(4).ToArray()))}");
                                }
                            }
                            if (splitInput[1] == "ERROR")
                            {
                                //todo
                            }
                            if (inputLine.Length > 1)
                            {
                                if (SERVER_CODES.Contains(splitInput[1]))
                                {
                                    writeLineA("main", $"-<span class=\"notice_nick\">Server</span>- {formatString(splitInput)}");
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

        public void send(string input)
        {
            if (input == null) return;
            if (input[0] == PREFIX)
            {
                String cmd = input.Remove(0, 1);
                String[] parts = cmd.Split(' ');
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
                    writeLine("main", "* You are not connected to a server");
                }
            }
        }

        private void writeLine(string window, string text)
        {
            HtmlElement tmp = webBrowser1.Document.CreateElement("span");
            tmp.InnerHtml = text + "<br>\n";
            webBrowser1.Document.GetElementById(window).AppendChild(tmp);
            //webBrowser1.Document.Window.ScrollTo(0, webBrowser1.Document.Body.ScrollRectangle.Height);
            webBrowser1.Document.InvokeScript("scroll");
            webBrowser1.Update();
        }

        private void writeLineA(string window, string text)
        {
            webBrowser1.BeginInvoke(new InvokeDelegate(writeLine), window, text);
        }

        private string formatString(string[] input)
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

        private void resized(object sender, EventArgs e)
        {
            try
            {
                webBrowser1.Document.InvokeScript("scroll");
            }
            catch { }
        }

        private void makeWindow(string windowName)
        {
            HtmlElement channelDiv = webBrowser1.Document.CreateElement("div");
            channelDiv.SetAttribute("id", windowName);
            channelDiv.SetAttribute("className", "maincont");
            webBrowser1.Document.GetElementById("mainParent").AppendChild(channelDiv);

            HtmlElement channelLink = webBrowser1.Document.CreateElement("a");
            channelLink.SetAttribute("id", $"{windowName}_link");
            channelLink.InnerHtml = $"#{windowName}";
            webBrowser1.Document.GetElementById("channel_list").AppendChild(channelLink);
            foreach (HtmlElement el in webBrowser1.Document.GetElementsByTagName("a"))
            {
                if (el.Id != null && el.Id.Equals($"{windowName}_link"))
                    el.AttachEventHandler("onclick", (sender1, e1) => clickEventHandler(el, EventArgs.Empty));
            }

            HtmlElement br = webBrowser1.Document.CreateElement("br");
            webBrowser1.Document.GetElementById("channel_list").AppendChild(br);
            windows.Add(windowName);
            switchTo(windowName);
        }
        public void clickEventHandler(object sender, EventArgs e)
        {
            var tmp = (HtmlElement)sender;
            switchTo(tmp.InnerHtml.Remove(0, 1));
        }

        public void switchTo(string window)
        {
            if (windows.Contains(window))
            {
                foreach (var w in windows)
                    webBrowser1.Document.GetElementById(w).Style = "display:none";
                webBrowser1.Document.GetElementById(window).Style = "display:block";
                // add scroll
                activeWindow = window;
            }
        }

        private string noColon(string input) => (input[0]==':') ? input.Remove(0, 1) : input;
        
    }
}