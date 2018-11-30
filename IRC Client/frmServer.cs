using System;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Windows.Forms;

namespace IRC_Client{
    public partial class frmServer : Form
    {
        private StreamWriter writer;
        private static bool isConnected = false;
        private const char PREFIX = '/';
        public delegate void InvokeDelegate(string arg);

        public frmServer()
        {
            InitializeComponent();
            //Control.CheckForIllegalCrossThreadCalls = false;
            //webBrowser1.Navigate("about:blank");
            webBrowser1.DocumentText = "0";
            webBrowser1.Document.OpenNew(true);
            webBrowser1.Document.Write("<html><head><link rel='stylesheet' type='text/css' href='file://"+ Directory.GetCurrentDirectory() + "/styles.css'></head><body>\n");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TcpClient irc = new TcpClient();
            irc.BeginConnect("irc.quakenet.org", 6667, CallbackMethod, irc);
        }

        private void CallbackMethod(IAsyncResult ar)
        {
            webBrowser1.BeginInvoke(new InvokeDelegate(writeLine),"Connecting...");
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
                                String tmp = "";
                                for (int i = 2; i < splitInput.Length; i++)
                                    tmp += " " + splitInput[i];
                                tmp = tmp.TrimStart();
                                if (tmp[0] == ':') tmp = tmp.Remove(0, 1);  //remove ':'
                                writeLineA("-" + splitInput[0] + "- " + tmp);
                            }
                            if (inputLine.Length > 1)
                            { 
                                if (splitInput[1] == "001" | splitInput[1] == "002" | splitInput[1] == "003" | splitInput[1] == "004" | splitInput[1] == "005" | splitInput[1] == "251" | splitInput[1] == "252" | splitInput[1] == "254" | splitInput[1] == "255" | splitInput[1] == "005" | splitInput[1] == "375" | splitInput[1] == "372")
                                {
                                    string tmp = "";
                                    for (int i = 3; i < splitInput.Length; i++)
                                        tmp += " " + splitInput[i];
                                    tmp = tmp.TrimStart();
                                    if (tmp[0] == ':') tmp = tmp.Remove(0, 1);  //remove ':'
                                    writeLineA("-Server- " + tmp);
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

        private void work(object sender, DoWorkEventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox2.Text[0] == PREFIX)
            {
                String cmd = textBox2.Text.Remove(0, 1);
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
                textBox2.Clear();
            }
            else
            {
                writer.WriteLine(textBox2.Text);
                writer.Flush();
                Console.WriteLine("-> " + textBox2.Text);
                textBox2.Clear();
            }
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            //webBrowser1.Document.Write("<html><head><link rel='stylesheet' type='text/css' href='styles.css'></head><body>\n");
        }
        private void writeLine(string text)
        {
            webBrowser1.Document.Write(text+"<br>\n");
            webBrowser1.Document.Window.ScrollTo(0, webBrowser1.Document.Body.ScrollRectangle.Height);
        }
        private void writeLineA(string text)
        {
            webBrowser1.BeginInvoke(new InvokeDelegate(writeLine), text);
        }
    }
}