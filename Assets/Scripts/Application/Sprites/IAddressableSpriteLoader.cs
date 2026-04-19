using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BlossomFlow.Application.Sprites
{
    public interface IAddressableSpriteLoader
    {
        IDisposable Load(AssetReferenceT<Sprite> spriteReference, Action<Sprite> onLoaded);
    }
}
