using System;
using System.Collections.Generic;
using System.IO;
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
        public delegate void InvokeDelegate(string arg);

        public frmServer()
        {
            InitializeComponent();
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
            //webBrowser1.Document.Window.ScrollTo(0, webBrowser1.Document.Body.ScrollRectangle.Height);
        }

        private void CallbackMethod(IAsyncResult ar)
        {
            webBrowser1.BeginInvoke(new InvokeDelegate(writeLine), "Connecting...");
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
                            //webBrowser1.BeginInvoke(new InvokeDelegate(writeLine), "<- " + inputLine + "\n");
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
                                writeLineA($"-<span class=\"notice_nick\">{splitInput[0].Substring(1)}</span>- {formatString(splitInput)}");
                            }
                            if (inputLine.Length > 1)
                            {
                                if (SERVER_CODES.Contains(splitInput[1]))
                                {
                                    writeLineA($"-<span class=\"notice_nick\">Server</span>- {formatString(splitInput)}");
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
                writeLine("<- <span color='red'>DISCONNECTED</span>");
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
                    writeLine("* You are not connected to a server");
                }
            }
        }

        private void writeLine(string text)
        {
            HtmlElement tmp = webBrowser1.Document.CreateElement("span");
            tmp.InnerHtml = text + "<br>\n";
            webBrowser1.Document.GetElementById("main").AppendChild(tmp);
            //webBrowser1.Document.Window.ScrollTo(0, webBrowser1.Document.Body.ScrollRectangle.Height);
            webBrowser1.Document.InvokeScript("scroll");
            webBrowser1.Update();
        }

        private void writeLineA(string text)
        {
            webBrowser1.BeginInvoke(new InvokeDelegate(writeLine), text);
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

        private void button1_Click(object sender, EventArgs e)
        {
            HtmlElement csss = webBrowser1.Document.CreateElement("div");
            csss.SetAttribute("id", "chan");
            csss.SetAttribute("className", "maincont");

            csss.InnerHtml = "owjhfoiwe fewoifwoe ifwewife";
            webBrowser1.Document.GetElementById("mainParent").AppendChild(csss);
            
            ///HtmlElement css1 = webBrowser1.Document.CreateElement("p");
            ///css1.InnerHtml = "wow<br>ttesrhntrhn<br>jhndrmnntrfd<br>ohfneowihbng";
            
            ///csss.AppendChild(css1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            webBrowser1.Document.GetElementById("main").Style = "display:none";
            webBrowser1.Document.GetElementById("chan").Style = "display:block";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            webBrowser1.Document.GetElementById("main").Style = "display:block";
            webBrowser1.Document.GetElementById("chan").Style = "display:none";
        }
    }
}