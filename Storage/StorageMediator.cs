using UnityEngine;

namespace Fury.Storage
{
    public class StorageMediator : MonoBehaviour
    {
        StorageController _controller;
        public StorageController Controller => _controller;

        internal void Register(StorageController controller)
        {
            _controller = controller;
        }

        private void LateUpdate()
        {
            _controller?.Keys.LateUpdate();
        }
    }

}