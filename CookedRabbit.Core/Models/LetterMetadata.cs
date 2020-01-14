using System.Collections.Generic;

namespace CookedRabbit.Core
{
    public class LetterMetadata
    {
        public string Id { get; set; }
        public bool Encrypted { get; set; }
        public bool Compressed { get; set; }

        public IDictionary<string, string> CustomFields = new Dictionary<string, string>();
    }
}
