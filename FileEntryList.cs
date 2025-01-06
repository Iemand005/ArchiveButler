using System;
using System.Collections.Generic;
using System.IO.Compression;
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

        public bool AddEntry(FileEntry entry)
        {
            return _entries.Add(entry);
        }

        public bool AddEntry(string fullName)
        {
            return AddEntry(new FileEntry { FullName = fullName });
        }

        public void AddEntry(string fullName, ZipArchiveEntry zipEntry)
        {
            FileEntry fileEntry = new FileEntry { FullName = fullName, ZipEntry = zipEntry };
            if (!AddEntry(fileEntry) && fileEntry.CreationTime.HasValue)
            {
                _entries.TryGetValue(fileEntry, out fileEntry);
                fileEntry.ZipEntry = zipEntry;
            }
        }

        public void AddEntry(string fullName, DateTime? dateTime)
        {
            FileEntry entry = new FileEntry { FullName = fullName, CreationTime = dateTime };
            if (!AddEntry(entry) && entry.CreationTime.HasValue)
            {
                _entries.TryGetValue(entry, out entry);
                entry.CreationTime = dateTime;
            }
        }
    }
}
