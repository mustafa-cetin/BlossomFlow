using nickeltin.DebugPanel.Runtime.Binding;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace nickeltin.DebugPanel.Examples.Runtime
{
    public class Player : MonoBehaviour
    {
        private static Player instance;

        [SerializeField] private TMP_Text _text;
        [SerializeField] private Slider _slider;
        
        private float _health = 100;

        private float Health
        {
            get => _health;
            set
            {
                _health = value;
                _health = Mathf.Clamp(_health, 0, 100);
                _text.text = _health.ToString("F0");
                _slider.value = Mathf.InverseLerp(0, 100, _health);
            }
        }
        
        private void Awake() => instance = this;

        public void Heal(float amount)
        {
            Health += amount;
            Debug.Log("Player healed! HP: " + Health);
        }

        public void Kill()
        {
            Health = 0;
            Debug.Log("Player killed! HP: " + Health);
        }

        public void Revive()
        {
            Health = 100;
            Debug.Log("Player revived! HP: " + Health);
        }

        #region Debug
        
        private const string DEBUG_ROOT = "Player/";
        private const string HEAL = DEBUG_ROOT + "Heal/";

        [DebugPanelButton(HEAL + "5")]
        private static void Heal5() => instance.Heal(5);

        [DebugPanelButton(HEAL + "15")]
        private static void Heal15() => instance.Heal(15);

        [DebugPanelButton(HEAL + "30")]
        private static void Heal30() => instance.Heal(30);

        [DebugPanelButton(DEBUG_ROOT + "Kill")]
        private static void Kill_Debug() => instance.Kill();

        [DebugPanelButton(DEBUG_ROOT + "Revive")]
        private static void Revive_Debug() => instance.Revive();
        
        [DebugPanelButton("1/2/3/4", PreferHorizontalGroups = true)]
        private static void HorizontalAlignment1() { } 
        
        [DebugPanelButton("1/5/6/7")]
        private static void HorizontalAlignment2() { }

        [DebugPanelButton("", "Secret Bindings", "Player/PlayerSecretOption")]
        private static void PlayerSecretOption()
        {
            Debug.Log("You found a player secret option!");
            Application.OpenURL("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        }
        
        #endregion
    }
}