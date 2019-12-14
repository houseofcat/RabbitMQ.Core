using CookedRabbit.Core.Configs;

namespace CookedRabbit.Core.Publisher
{
    public class AutoPublisher
    {
        private Config Config { get; }
        private Publisher Publisher { get; }

        private const string ChannelError = "Can't use reader on a closed Threading.Channel.";

        public AutoPublisher(Config config)
        {
            Config = config;
        }
    }
}
