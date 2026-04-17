using System.Collections.Generic;
using nickeltin.DebugPanel.Runtime.Binding;
using UnityEngine;

namespace nickeltin.DebugPanel.Examples.Runtime
{
    /// <summary>
    /// Used for static binding together with <see cref="BindingsContentProviderExample"/>
    /// </summary>
    public class MobFactory : MonoBehaviour
    {
        private static MobFactory instance;

        [SerializeField] private float _radius;
        
        private List<GameObject> _mobs;

        private void Awake()
        {
            instance = this;
            _mobs = new List<GameObject>();
        }

        public void SpawnMob()
        {
            var mob = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var rot = Quaternion.Euler(
                Random.value * 360,
                Random.value * 360,
                Random.value * 360);
            
            mob.transform.position = transform.position + (rot * new Vector3(0, 0, Random.value * _radius));
            _mobs.Add(mob);
        }

        public void SpawnMobs(int count)
        {
            for (var i = 0; i < count; i++) SpawnMob();
        }

        public void DespawnMob(int at)
        {
            if (_mobs.Count == 0) return;
            
            var mob = _mobs[at];
            Destroy(mob.gameObject);
            _mobs.RemoveAt(at);
        }

        #region Debug
        
        private const string DEBUG_ROOT = "Mobs/";
        private const string SPAWN_ROOT = DEBUG_ROOT + "Spawn/";
        private const string DESPAWN_ROOT = DEBUG_ROOT + "Despawn/";

        [DebugPanelButton(SPAWN_ROOT + "1")]
        private static void Spawn1() => instance.SpawnMobs(1);

        [DebugPanelButton(SPAWN_ROOT + "5")]
        private static void Spawn5() => instance.SpawnMobs(5);

        [DebugPanelButton(SPAWN_ROOT + "10")]
        private static void Spawn10() => instance.SpawnMobs(10);
        

        [DebugPanelButton(DESPAWN_ROOT + "1")]
        private static void Despawn1() => instance.DespawnMob(0);
        
        [DebugPanelButton(DESPAWN_ROOT + "All")]
        private static void DespawnAll()
        {
            while (instance._mobs.Count > 0) instance.DespawnMob(0);
        }

        #endregion 
    }
}