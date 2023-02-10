using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class DualList<A, B> : IEnumerable<(A first, B second)>, IEnumerable//, ICollection<(A first, B second)>, IList<(A first, B second)>
    {
        private List<(A first, B second)> list = new List<(A first, B second)>();

        public DualList(IEnumerable<A> listA, IEnumerable<B> listB)
        {
            IEnumerator<A> iterA = listA.GetEnumerator();
            IEnumerator<B> iterB = listB.GetEnumerator();
            while(iterA.MoveNext() && iterB.MoveNext())
            {
                Add(iterA.Current, iterB.Current);
            }
        }
        public void Add(A first, B second) => list.Add((first, second));
        public IEnumerator<(A first, B second)> GetEnumerator() => list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
    }
}
