using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Text.Json.Serialization;

namespace ArchiveButler
{
    internal class FileEntry
    {
        [JsonIgnore]
        public string Name {
            get
            {
                return System.IO.Path.GetFileName(FullName);
            }
        }
        [JsonIgnore]
        public string Path {
            get
            {
                return System.IO.Path.GetDirectoryName(FullName);
            }
        }
        public DateTime? Date { get; set; }

        [JsonIgnore]
        public ZipArchiveEntry ZipEntry { get; set; }

        [JsonIgnore]
        public string CreationTimeString
        {
            get
            {
                return Date != null ? Date.ToString() : "undefined";
            }
        }
        public string FullName { get; set; }

        public long Size { get; set; }

        public FileEntry() { }

        public FileEntry(ZipArchiveEntry entry)
        {
            FullName = entry.FullName;
            Size = entry.Length;
            ZipEntry = entry;
            Date = entry.LastWriteTime.UtcDateTime;
        }

        public bool Meta { get; set; }

        // override object.Equals
        public override bool Equals(object obj)
        {
            //       
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237  
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            FileEntry fileEntry = obj as FileEntry;

            return Name == fileEntry.Name && Path == fileEntry.Path;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Path.GetHashCode();
        }

        //public bool Equals(FileEntry x, FileEntry y)
        //{

        //    //Check whether the compared objects reference the same data.
        //    if (ReferenceEquals(x, y)) return true;

        //    //Check whether any of the compared objects is null.
        //    if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;

        //    //Check whether the products' properties are equal.
        //    return x.Name == y.Name && x.Path == y.Path;
        //}

        //public int GetHashCode(FileEntry fileEntry)
        //{
        //    //Check whether the object is null
        //    if (ReferenceEquals(fileEntry, null)) return 0;

        //    int hashFileEntryName = fileEntry.Name == null ? 0 : fileEntry.Name.GetHashCode();
        //    int hashFileEntryPath = fileEntry.Path == null ? 0 : fileEntry.Path.GetHashCode();

        //    return hashFileEntryName ^ hashFileEntryPath;
        //}
    }
}
