using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TileMapGenerator;

namespace TileMapGenerator
{
    public class OrderedHashSet<T> : HashSet<T>, IEnumerable<T> where T : IDed
    {

        internal OrderedHashSet (IEnumerable<T> collection) : base(collection){}
        internal OrderedHashSet (ICollection<T> collection) : base(collection){}
        internal OrderedHashSet (ISet<T> collection) : base(collection){}
        internal OrderedHashSet (IList<T> collection) : base(collection){}

        internal OrderedHashSet () : base(){}

        public new IEnumerator<T> GetEnumerator()
        {
            return this.OrderBy(item=>item.ID).GetEnumerator();
        }

        public IEnumerable<T> GetEnumerable()
        {
            return this.OrderBy(item=>item.ID);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.OrderBy(item=>item.ID).GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.OrderBy(item=>item.ID).GetEnumerator();
        }
    }
}