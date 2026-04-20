using System.Collections;
using ByteBrewSDK;

namespace BlossomFlow.Application.SceneLoading
{
    public sealed class ByteBrewStartupServiceInitializer : IStartupServiceInitializer
    {
        private bool _hasInitialized;

        public IEnumerator InitializeIfNeeded()
        {
            if (_hasInitialized || ByteBrew.IsInitilized)
            {
                _hasInitialized = true;
                yield break;
            }

            ByteBrew.InitializeByteBrew();
            _hasInitialized = true;
            yield break;
        }
    }
}
