using System.Collections.Generic;

namespace CookedRabbit.Core
{
    public class LetterMetadata
    {
        public string Id { get; set; }
        public Dictionary<string, string> Data = new Dictionary<string, string>();
    }
}
