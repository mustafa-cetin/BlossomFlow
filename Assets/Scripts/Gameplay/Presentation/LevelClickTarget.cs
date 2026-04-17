using System;
using UnityEngine;

namespace BlossomFlow.Gameplay.Presentation
{
    public class LevelClickTarget : MonoBehaviour
    {
        private Action _clicked;

        public void Bind(Action clicked)
        {
            _clicked = clicked;
        }

        public void Click()
        {
            _clicked?.Invoke();
            Debug.Log("Clicked " + name);
        }
    }
}
