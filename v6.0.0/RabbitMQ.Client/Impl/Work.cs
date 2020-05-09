using System.Threading.Tasks;

namespace RabbitMQ.Client.Impl
{
    public abstract class Work
    {
        private readonly IAsyncBasicConsumer _asyncConsumer;

        protected Work(IBasicConsumer consumer)
        {
            _asyncConsumer = (IAsyncBasicConsumer)consumer;
        }

        public async Task Execute(ModelBase model)
        {
            try
            {
                await Task.Yield();
                await Execute(model, _asyncConsumer).ConfigureAwait(false);
            }
            catch
            {
                // intentionally caught
            }
        }

        protected abstract Task Execute(ModelBase model, IAsyncBasicConsumer consumer);
    }
}
