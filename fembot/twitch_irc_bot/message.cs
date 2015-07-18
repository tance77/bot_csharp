using System.Collections.Generic;

namespace twitch_irc_bot
{
    class Message
    {
        private string _cmd;
        private CommandPrefix _commandPrefix;
        private List<string> _params;

        public Message()
        {

        }
        public Message(string cmd, CommandPrefix p, List<string> parameters)
        {
            _cmd = cmd;
            _commandPrefix = p;
            _params = parameters;
        }
    }

    class CommandPrefix
    {
        private string _prefix;
        private string _nick;
        private string _user;
        private string _host;
        private string[] _tokens;

        public CommandPrefix()
        {

        }

        public void Parse(string data)
        {
            if (data == "") return;
            _prefix = data.Substring(1, data.IndexOf(' ') - 1);
            if (_prefix.IndexOf('@') != -1)
            {
                _tokens = _prefix.Split('@');
                _nick = _tokens[0];
                _host = _tokens[1];
            }
            if (_nick != "" && _nick.IndexOf('!') != -1)
            {
                _tokens = _nick.Split('!');
                _nick = _tokens[0];
                _user = _tokens[1];
            }
        }
    }
}
