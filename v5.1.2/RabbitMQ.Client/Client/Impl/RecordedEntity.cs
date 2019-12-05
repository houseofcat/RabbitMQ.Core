namespace RabbitMQ.Client.Impl
{
    public abstract class RecordedEntity
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
