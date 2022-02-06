using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class DualList<A, B> : IEnumerable<Pair<A, B>>, IEnumerable//, ICollection<Pair<A, B>>, IList<Pair<A, B>>
    {
        private List<Pair<A, B>> list = new List<Pair<A, B>>();

        public DualList(IEnumerable<A> listA, IEnumerable<B> listB)
        {
            IEnumerator<A> iterA = listA.GetEnumerator();
            IEnumerator<B> iterB = listB.GetEnumerator();
            while(iterA.MoveNext() && iterB.MoveNext())
            {
                Add(iterA.Current, iterB.Current);
            }
        }
        public void Add(A first, B second) => list.Add(new Pair<A, B>(first, second));
        public IEnumerator<Pair<A, B>> GetEnumerator() => list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
    }
}
