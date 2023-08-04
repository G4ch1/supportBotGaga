using Discord;
using System;

namespace supportBotGaga
{
    public class CommandResult
    {
        public string Text { get; private set; }
        public bool IsTTS { get; private set; }
        public Embed Embed { get; private set; }
        public RequestOptions RequestOptions { get; private set; }
        public AllowedMentions AllowedMentions { get; private set; }
        public MessageComponent MessageComponent { get; private set; }
        public Embed[] Embeds { get; private set; }
        public Exception Exception { get; private set; }
        public bool Ephemeral { get; private set; } = false;

        public CommandResult()
        {
            Text = null;
            IsTTS = false;
            Embed = null;
            RequestOptions = null;
            AllowedMentions = null;
            MessageComponent = null;
            Embeds = null;
            Exception = null;
        }

        public CommandResult WithText(string text)
        {
            Text = text;
            return this;
        }

        public CommandResult WithIsTTS(bool isTTS)
        {
            IsTTS = isTTS;
            return this;
        }

        public CommandResult WithEmbed(EmbedBuilder embedBuilder)
        {
            Embed = embedBuilder.Build();
            return this;
        }

        public CommandResult WithRequestOptions(RequestOptions requestOptions)
        {
            RequestOptions = requestOptions;
            return this;
        }

        public CommandResult WithAllowedMentions(AllowedMentions allowedMentions)
        {
            AllowedMentions = allowedMentions;
            return this;
        }


        public CommandResult WithMessageComponent(MessageComponent messageComponent)
        {
            MessageComponent = messageComponent;
            return this;
        }

        public CommandResult WithEmbeds(Embed[] embeds)
        {
            Embeds = embeds;
            return this;
        }

        public CommandResult WithException(Exception exception)
        {
            Exception = exception;
            return this;
        }

        public CommandResult WithException(string message)
        {
            Exception = new Exception(message);
            return this;
        }

        public CommandResult WithEphemeral(bool ephemeral)
        {
            Ephemeral = ephemeral;
            return this;
        }
    }
}
