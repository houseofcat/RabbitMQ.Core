using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CookedRabbit.Core.WorkEngines
{
    public interface IWorkState
    {
        IDictionary<string, object> Data { get; set; }
        IReceivedData ReceivedData { get; set; }
        string StepIdentifier { get; set; }
    }

    public abstract class WorkState : IWorkState
    {
        public virtual string StepIdentifier { get; set; }

        [IgnoreDataMember]
        public virtual IReceivedData ReceivedData { get; set; }

        [IgnoreDataMember]
        public virtual IDictionary<string, object> Data { get; set; }
    }
}
