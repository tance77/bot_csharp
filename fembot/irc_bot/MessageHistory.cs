using System;
using System.Collections.Generic;

namespace twitch_irc_bot
{
    internal class MessageHistory
    {
        private readonly string _channel;
        private readonly List<string> _messages;
        private readonly string _sender;
        private DateTime _time;
        private Dictionary<string,DateTime> _messagesWithTimes; 

        public MessageHistory(string channel, string sender, string message)
        {
            _channel = channel;
            _sender = sender;
            _time = DateTime.Now;
            _messagesWithTimes = new Dictionary<string, DateTime>();
            if (!_messagesWithTimes.ContainsKey(message))
            {
                _messagesWithTimes.Add(message, _time);
            }
            _messages = new List<string> {message};
        }

        public void ClearMessageHistory(){
            _messagesWithTimes.Clear();
            _messages.Clear();
        }

        public Dictionary<string, DateTime> GetMessagesWithTimes(){
            return _messagesWithTimes;
        }

        public List<string> GetMessages()
        {
            return _messages;
        }

        public void AddMessage(string message)
        {
            _messages.Add(message);
            _time = DateTime.Now;

        }

        public void RemoveFirst()
        {
            _messages.RemoveAt(0);
        }

        public bool ContainsMessage(string messasge)
        {
            return _messages.Contains(messasge);
        }

        public string GetSender()
        {
            return _sender;
        }

        public string GetChannel()
        {
            return _channel;
        }

        public int Count()
        {
            return _messages.Count;
        }

        public bool LastMessageTime()
        {
            return DateTime.Now > _time.AddHours(3);
        }

        public void DumpMessagesToFile() { 

        }
    }
}
