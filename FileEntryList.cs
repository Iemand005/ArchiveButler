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
            set
            {
                Merge(value);
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

        public void AddEntry(ZipArchiveEntry entry)
        {
            FileEntry fileEntry = new FileEntry(entry);
            if (!AddEntry(fileEntry) && fileEntry.CreationTime.HasValue)
            {
                _entries.TryGetValue(fileEntry, out fileEntry);
                fileEntry.ZipEntry = entry;
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

        public void Merge(FileEntryList fileEntryList)
        {
            Merge(fileEntryList.Entries);
        }

        public void Merge(List<FileEntry> fileEntryList)
        {
            foreach (FileEntry entry in fileEntryList) AddEntry(entry);
        }

        internal FileEntry GetEntry(ZipArchiveEntry archiveEntry)
        {
            FileEntry fileEntry = new FileEntry(archiveEntry);
            _entries.TryGetValue(fileEntry, out fileEntry);
            return fileEntry;
        }
    }
}
