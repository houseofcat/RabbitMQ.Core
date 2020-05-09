namespace RabbitMQ.Client.Impl
{
    internal abstract class RecordedEntity
    {
        protected RecordedEntity(AutorecoveringModel model)
        {
            Model = model;
        }

        public AutorecoveringModel Model { get; protected set; }

        protected IModel ModelDelegate
        {
            get { return Model.Delegate; }
        }
    }
}
