namespace Fury.Storage
{
    public sealed class Singletone<TValue>  : IStorageContainer
        where TValue : class, new()
    {
        readonly string _id;

        private TValue _value;
        public TValue Value
        {
            get
            {
                if (_value == null)
                {
                    _value = new TValue();
                }
                _controller.Keys.Update(_id, _value);
                return _value;
            }
        }

        public Singletone(string id)
        {
            _id = id;
        }

        StorageController _controller;

        public void Reload(StorageController controller)
        {
            _controller = controller;
        }
    }
}