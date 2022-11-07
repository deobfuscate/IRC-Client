using System;
using System.IO;
using System.Net.Sockets;

namespace IRC_Client {
    class IRC {
        public bool isConnected = false;
        private StreamWriter writer;
        private string nick;

        public void Connect(string server, int port, string nickname) {
            nick = nickname;
            TcpClient irc = new TcpClient();
            irc.BeginConnect(server, port, CallbackMethod, irc);
        }

        private void CallbackMethod(IAsyncResult result) {
            isConnected = false;
            TcpClient tcpclient = result.AsyncState as TcpClient;

            if (tcpclient.Client == null) return;
            try {
                tcpclient.EndConnect(result);
            }
            catch (SocketException ex) {
                RaiseSocketExceptionEvent(new TokenEventArgs(new string[] { "main", ex.Message }));
                return;
            }
            isConnected = true;

            NetworkStream stream = tcpclient.GetStream();
            StreamReader reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
            string inputLine;
            writer.WriteLine($"USER {nick} 0 * :{nick}");
            writer.Flush();
            writer.WriteLine($"NICK {nick}");
            writer.Flush();
            while (true) {
                while (tcpclient.Connected && (inputLine = reader.ReadLine()) != null) {
                    #if DEBUG
                        Console.WriteLine("<- " + inputLine);
                    #endif
                    string[] tokens = inputLine.Split(new char[] { ' ' });
                    if (tokens.Length < 2) continue;
                    if (tokens[0] == "PING") {
                        string key = tokens[1];
                        #if DEBUG
                            Console.WriteLine($"-> PONG {key}");
                        #endif
                        writer.WriteLine($"PONG {key}");
                        writer.Flush();
                        continue;
                    }
                    switch (tokens[1]) {
                        /*case "001":
                            ChangeNick(tokens[2]);
                            break;*/
                        case "PRIVMSG":
                            RaisePrivMsgEvent(new TokenEventArgs(tokens));
                            break;
                        case "NOTICE":
                            RaiseNoticeEvent(new TokenEventArgs(tokens));
                            break;
                        case "NICK":
                            RaiseNickEvent(new TokenEventArgs(tokens));
                            break;
                        case "JOIN":
                            RaiseJoinEvent(new TokenEventArgs(tokens));
                            break;
                        case "PART":
                            RaisePartEvent(new TokenEventArgs(tokens));
                            break;
                        case "QUIT":
                            RaiseQuitEvent(new TokenEventArgs(tokens));
                            break;
                        case "MODE":
                            RaiseModeEvent(new TokenEventArgs(tokens));
                            break;
                        case "332": // topic
                            RaiseTopicEvent(new TokenEventArgs(tokens));
                            break;
                        case "353": // userlist
                            RaiseUserEvent(new TokenEventArgs(tokens));
                            break;
                        default:
                            RaiseDefaultEvent(new TokenEventArgs(tokens));
                            break;
                    }
                }
                writer.Close();
                reader.Close();
                tcpclient.Close();
                isConnected = false;
                RaiseDisconnectEvent(EventArgs.Empty);
            }
        }

        public void Write(string input) {
            writer.WriteLine(input);
            writer.Flush();
        }

        // Events
        public event EventHandler<TokenEventArgs> PrivMsgEvent;
        protected virtual void RaisePrivMsgEvent(TokenEventArgs e) {
            PrivMsgEvent?.Invoke(this, e);
        }

        public event EventHandler<TokenEventArgs> NoticeEvent;
        protected virtual void RaiseNoticeEvent(TokenEventArgs e) {
            NoticeEvent?.Invoke(this, e);
        }

        public event EventHandler<TokenEventArgs> NickEvent;
        protected virtual void RaiseNickEvent(TokenEventArgs e) {
            NickEvent?.Invoke(this, e);
        }

        public event EventHandler<TokenEventArgs> JoinEvent;
        protected virtual void RaiseJoinEvent(TokenEventArgs e) {
            JoinEvent?.Invoke(this, e);
        }

        public event EventHandler<TokenEventArgs> QuitEvent;
        protected virtual void RaiseQuitEvent(TokenEventArgs e) {
            QuitEvent?.Invoke(this, e);
        }

        public event EventHandler<TokenEventArgs> TopicEvent;
        protected virtual void RaiseTopicEvent(TokenEventArgs e) {
            TopicEvent?.Invoke(this, e);
        }

        public event EventHandler<TokenEventArgs> UserEvent;
        protected virtual void RaiseUserEvent(TokenEventArgs e) {
            UserEvent?.Invoke(this, e);
        }

        public event EventHandler<TokenEventArgs> DefaultEvent;
        protected virtual void RaiseDefaultEvent(TokenEventArgs e) {
            DefaultEvent?.Invoke(this, e);
        }

        public event EventHandler<TokenEventArgs> PartEvent;
        protected virtual void RaisePartEvent(TokenEventArgs e) {
            PartEvent?.Invoke(this, e);
        }

        public event EventHandler<TokenEventArgs> ModeEvent;
        protected virtual void RaiseModeEvent(TokenEventArgs e) {
            ModeEvent?.Invoke(this, e);
        }

        public event EventHandler<TokenEventArgs> SocketExceptionEvent;
        protected virtual void RaiseSocketExceptionEvent(TokenEventArgs e) {
            SocketExceptionEvent?.Invoke(this, e);
        }

        public event EventHandler DisconnectEvent;
        protected virtual void RaiseDisconnectEvent(EventArgs e) {
            DisconnectEvent?.Invoke(this, e);
        }
    }

    public class TokenEventArgs : EventArgs {
        public string[] tokens;

        public TokenEventArgs(string[] tokens) {
            this.tokens = tokens;
        }
    }
}
