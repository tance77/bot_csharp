using System;
using System.IO;
using System.Text.RegularExpressions;

namespace twitch_irc_bot
{

    internal class TwitchMessage
    {
        #region Constructors


        public TwitchMessage (string data)
        {
            Command = null;
            FromChannel = null;
            MsgSender = null;
            MsgRecipient = null;
            Msg = null;
            Joiner = null;
            Privlages = null;
            OperatingUser = null;
            Color = null;
            DisplayName = null;
            Emotes = null;
            UserType = null;
            Subscriber = false;
            Turbo = false;
            HostingChannel = null;
            TargetChannel = null;
            HostViewerCount = null;
            UserId = 0;
            MessageHandler (data);

        }

        #endregion

        #region Geters/Setters


        public string HostingChannel { get; set; }

        public string HostViewerCount { get; set; }
        public string TargetChannel { get; set; }
        public string Command { get; set; }

        public string FromChannel { get; set; }

        public string MsgSender { get; set; }

        public string MsgRecipient { get; set; }

        public string Msg { get; set; }

        public string Joiner { get; set; }

        public string Privlages { get; set; }

        public string OperatingUser { get; set; }

        public string Color { get; set; }

        public string DisplayName { get; set; }

        public string Emotes { get; set; }

        public string UserType { get; set; }

        public bool Subscriber { get; set; }

        public bool Turbo { get; set; }

        public int UserId { get; set; }

        #endregion

        #region Methods

        public string MessageHandler (string m)
        {
            //var colenCount = 0;
            //var shit = m.Split(' ');
            //var totalM = "";
            //foreach (var a in shit)
            //{
            //    if (a.StartsWith(":"))
            //    {
            //        colenCount++;
            //    }
            //    if (colenCount == 2)
            //    {
            //        totalM += a + " ";
            //    }
            //    else
            //    {
            //Console.WriteLine(a);
            //    }
                
            //}
            //totalM = totalM.Trim();
            //if (totalM != "")
            //{
            //    totalM = totalM.Substring(1);
            //Console.WriteLine("ACTUAL MESSAGE");
            //Console.WriteLine(totalM);
            //}
            /*------- Successfull Twitch Connection -----------*/
            if (Regex.Match (m, @"tmi.twitch.tv").Success) {
                string[] messageArray = m.Split (' ');
                if (messageArray.Length != 2) {
                    MsgSender = messageArray [0];
                    Command = messageArray [1];
                    MsgRecipient = messageArray [2];
                    Msg = "";
                    for (int i = 3; i < messageArray.Length; i++) {
                        if (i == messageArray.Length - 1) {
                            Msg += messageArray [i];
                        } else {
                            Msg += messageArray [i] + " ";
                        }
                    }
                }
            } else if (Regex.Match (m, @"tmi.twitch.tv JOIN").Success) {
                FromChannel = m.Split ('#') [1];
                Joiner = m.Split ('!') [0].Split (':') [1];
                Joiner = Joiner.ToLower ();
                Command = "JOIN";

            } else if (Regex.Match (m, @":tmi.twitch.tv HOSTTARGET").Success) {
                HostingChannel = m.Split(' ')[1];
                TargetChannel = m.Split(' ')[2];
                if (TargetChannel == ":-")
                {
                    //Target Channel is offline
                }
                else
                {
                    HostViewerCount = m.Split(' ')[3];
                    //Channel is offline
                }
                Console.WriteLine ("something happened");
                Command = "HOSTTARGET";
  
            } else if (Regex.Match (m, @":tmi.twitch.tv PART").Success) {
                Command = "PART";
            } else if (Regex.Match (m, @":tmi.twitch.tv 353").Success || Regex.Match (m, @"tmi.twitch.tv 366").Success) {
            } else if (Regex.Match (m, @":jtv MODE").Success) {
                //:jtv MODE #channel +o operator_user
                //:jtv MODE #channel -o operator_user
                string[] messageParts = m.Split (' ');
                FromChannel = messageParts [2].Split ('#') [1];
                Privlages = messageParts [3];
                OperatingUser = messageParts [4];
            } else if (Regex.Match (m, @"msg-id=subs_on").Success) {
            } else if (Regex.Match (m, @"msg-id=subs_off").Success) {
            } else if (Regex.Match (m, @"msg-id=slow_on").Success) {
            } else if (Regex.Match (m, @"msg-id=slow_off").Success) {
            } else if (Regex.Match (m, @"msg-id=r9k_on").Success) {
            } else if (Regex.Match (m, @"msg-id=r9k_off").Success) {
            } else if (Regex.Match (m, @"msg-id=host_on").Success) {
                //:tmi.twitch.tv HOSTTARGET #hosting_channel :target_channel [number]
                Console.WriteLine ("Go Check out _______");
            } else if (Regex.Match (m, @"msg-id=host_off").Success) {
//                Console.WriteLine ("unhosting");
                //> :tmi.twitch.tv HOSTTARGET #hosting_channel :- [number]
            } else if (Regex.Match (m, @":tmi\.twitch\.tv CLEARCHAT").Success) { //see if this regex matches
                Command = "CLEARCHAT";
                File.WriteAllText ("./bad.txt", m);
            } else if (Regex.Match (m, @":tmi.twitch.tv USERSTATE").Success) {
            } else if (Regex.Match (m, @":twitchnotify!twitchnotify@twitchnotify.tmi.twitch.tv").Success) {
                Console.WriteLine ("something happened");
            } else if (Regex.Match (m, @"tmi.twitch.tv PRIVMSG").Success) {
                /*
                 * @color=#FFFFFF;display-name=TWITCHNAME;emotes=;subscriber=0;turbo=0;user-id=0000000;user-type=mod
                 * :CHANNEL!CHANNEL@CHANNEL.tmi.twitch.tv PRIVMSG #RECIPIENT :asd
                 */
                string[] msgArray = m.Split (' ');
                //Msg = "";
                FromChannel = msgArray [3].Split ('#') [1];
                MsgSender = msgArray [1].Split (':') [1].Split ('!') [0];
                Command = msgArray [2];
                //form the message since we split on space
                for (var s = 4; s < msgArray.Length; s++) {
                    if (s == msgArray.Length - 1)
                        Msg += msgArray [s];
                    else
                        Msg += msgArray [s] + " ";
                }
                Msg = Msg.TrimStart (':');
                string[] prefix = msgArray [0].Split (';');
                Color = prefix [1].Split ('=') [1].Split ('"') [0];
                DisplayName = prefix [2].Split ('=') [1].Split ('"') [0];
                try {
                    Emotes = prefix [3].Split ('=') [1].Split ('"') [0];
                } catch (IndexOutOfRangeException) {
                    Emotes = "";
                }

                Subscriber = prefix [6].Split ('=') [1].Split ('"') [0] != "0";
                Turbo = prefix [7].Split ('=') [1].Split ('"') [0] != "0";
                var tmp = 0;
                if (!Int32.TryParse (prefix [5].Split ('=') [1].Split ('"') [0], out tmp)) {
                    UserId = tmp;
                }
                UserType = prefix [9].Split ('=') [1].Split ('"') [0].Split (' ') [0];
                if (MsgSender.ToLower () == FromChannel.ToLower ()) {
                    UserType = "mod";
                }
                OperatingUser = prefix [4].Split ('=') [1].Split ('"') [0].Split (' ') [0];
                Command = "PRIVMSG";
            }
            return Command;
        }

        #endregion

    }
}
