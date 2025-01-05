using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveButler
{
    internal class DirectoryTreeNode
    {
        public string Name { get; set; }
        public ObservableCollection<DirectoryTreeNode> Children { get; set; } = new ObservableCollection<DirectoryTreeNode>();
    }
}
