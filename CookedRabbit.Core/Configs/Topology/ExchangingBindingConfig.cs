using System.Collections.Generic;

namespace CookedRabbit.Core
{
    public class ExchangeBindingConfig
    {
        public string ChildExchange { get; set; }
        public string ParentExchange { get; set; }
        public string RoutingKey { get; set; }
        public IDictionary<string, object> Args { get; set; }
    }
}
