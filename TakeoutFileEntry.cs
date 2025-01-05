using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveButler
{
    internal class TakeoutFileEntry
    {
        public string Title { get; set; }
        public TakeoutTime CreationTime { get; set; }
        public TakeoutTime PhotoTakenTime { get; set; }
    }
}
