using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BlossomFlow.Gameplay.Domain.Levels
{
    [Serializable]
    public sealed class PotQueueColumnDefinition : IEnumerable<PotDefinition>
    {
        public PotQueueColumnDefinition()
            : this(Array.Empty<PotDefinition>())
        {
        }

        public PotQueueColumnDefinition(IEnumerable<PotDefinition> pots)
        {
            Pots = (pots ?? throw new ArgumentNullException(nameof(pots))).ToArray();
        }

        public PotDefinition[] Pots;
        public int Count => Pots?.Length ?? 0;

        public IEnumerator<PotDefinition> GetEnumerator()
        {
            return ((IEnumerable<PotDefinition>)(Pots ?? Array.Empty<PotDefinition>())).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
