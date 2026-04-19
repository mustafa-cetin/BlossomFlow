using System;
using UnityEngine;

namespace BlossomFlow.Gameplay.Presentation
{
    public class LevelClickTarget : MonoBehaviour
    {
        private Action _clicked;

        public LevelClickTargetKind Kind { get; private set; }
        public int Index { get; private set; } = -1;

        public void Bind(Action clicked)
        {
            _clicked = clicked;
        }

        public void SetTarget(LevelClickTargetKind kind, int index)
        {
            Kind = kind;
            Index = index;
        }

        public bool Is(LevelClickTargetKind kind, int index)
        {
            return Kind == kind && Index == index;
        }

        public void Click()
        {
            _clicked?.Invoke();
            Debug.Log("Clicked " + name);
        }
    }
}
