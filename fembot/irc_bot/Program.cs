﻿using System;


namespace twitch_irc_bot
{
    class Program
    {
        static void Main(string[] args)
        {
            var irc = new IrcClient("irc.twitch.tv", 6667, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
            //irc.JoinChannelStartup();
            irc.JoinChannel("whitemarmalade");


            while (true)
            {
                var message = irc.ReadMessage();

                if (message != "")
                {
                    irc.MessageHandler(message);
                    Console.Write(message + "\r\n");
                }
            }
        }
    }
}
