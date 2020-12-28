using System.ComponentModel.DataAnnotations;
using static CookedRabbit.Core.Constants;
using static CookedRabbit.Core.Enums;

namespace CookedRabbit.Core
{
    public class RoutingOptions
    {
        [Range(1, 2, ErrorMessage = RangeErrorMessage)]
        public byte DeliveryMode { get; set; } = 2;

        public bool Mandatory { get; set; }

        // Max Priority letter level is 255, however, the max-queue priority though is 10, so > 10 is treated as 10.
        [Range(0, 10, ErrorMessage = RangeErrorMessage)]
        public byte PriorityLevel { get; set; } = 0;

        public string MessageType = $"{ContentType.Json.Description()} {Charset.Utf8.Description()}";
    }
}
