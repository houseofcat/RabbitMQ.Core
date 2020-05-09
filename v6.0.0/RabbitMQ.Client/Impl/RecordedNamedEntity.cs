namespace RabbitMQ.Client.Impl
{
    internal class RecordedNamedEntity : RecordedEntity
    {
        public RecordedNamedEntity(AutorecoveringModel model, string name) : base(model)
        {
            Name = name;
        }

        public string Name { get; protected set; }
    }
}
