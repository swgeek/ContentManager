using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viweer
{
    class SingleRow
    {
        string hash;
        long filesize;
    }
    class TempDataInterface : IList<SingleRow>
    {
        SingleRow this[int index] { get; set; }
        int IndexOf(SingleRow item);
        void Insert(int index, SingleRow item);
        void RemoveAt(int index);
    }
}
