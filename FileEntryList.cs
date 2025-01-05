using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveButler
{
    internal class FileEntryList
    {
        private readonly HashSet<FileEntry> _entries;
        public List<FileEntry> Entries
        {
            get
            {
                return _entries.ToList();
            }
        }

        public FileEntryList()
        {
            _entries = new HashSet<FileEntry>();
        }

        public void AddEntry(string fullName)
        {
            AddEntry(fullName, null);
        }

        public void AddEntry(string fullName, DateTime? dateTime)
        {
            FileEntry entry = new FileEntry { FullName = fullName, CreationTime = dateTime };
            if (!_entries.Contains(entry))
            {
                _entries.Add(entry);
            }
            else if (entry.CreationTime.HasValue)
            {
                _entries.TryGetValue(entry, out entry);
                entry.CreationTime = dateTime;
            }
        }
    }
}
