using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Data.SqlClient;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;

namespace ArchiveButler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private FileEntryList FileEntries = new FileEntryList();
        internal ObservableCollection<DirectoryTreeNode> DirectoryTreeNodes { get; set; }
        private List<ZipArchive> zipArchives { get; set; } = new List<ZipArchive>();
        private ObservableCollection<DirectoryTreeNode> rootNodes = new ObservableCollection<DirectoryTreeNode>();

        private bool LoadFileDates { get; set; } = false;

        public long FileCount {
            get
            {
                return FileEntries.Entries.Count;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog(this) ?? false)
            {
                foreach (string fileName in openFileDialog.FileNames)
                {
                    ZipArchive archive = ZipFile.Open(fileName, ZipArchiveMode.Read);

                    zipArchives.Add(archive);
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (Path.GetExtension(entry.Name).Equals(".json") && LoadFileDates)
                        {

                            JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            };

                            TakeoutFileEntry fileEntry = new TakeoutFileEntry();

                            try
                            {

                            fileEntry = JsonSerializer.Deserialize<TakeoutFileEntry>(entry.Open(), jsonSerializerOptions);
                            } catch
                            { }


                            if (fileEntry.CreationTime != null) // file could also be a json file not related to a file, then the metadata file for it is .json.json lols
                            {
                                DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(fileEntry.CreationTime.Timestamp)).UtcDateTime;

                                string newPath = Path.ChangeExtension(entry.FullName, null);

                                FileEntries.AddEntry(newPath, dateTime);
                            }
                            else FileEntries.AddEntry(entry.FullName, entry);
                        }
                        else
                        {
                            FileEntries.AddEntry(entry.FullName, entry);
                        }
                    }
                    FileListView.ItemsSource = FileEntries.Entries;
                    NotifyPropertyChanged("FileCount");
                    DirectoryTreeNodes = BuildDirectoryTree(FileEntries);
                    DirectoryTree.ItemsSource = DirectoryTreeNodes;
                }
            }
        }

        private async Task LoadTakeoutFiles(string[] fileNames)
        {
            foreach (string fileName in fileNames)
            {
                ZipArchive archive = ZipFile.Open(fileName, ZipArchiveMode.Read);

                zipArchives.Add(archive);
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (Path.GetExtension(entry.Name).Equals(".json") && LoadFileDates)
                    {

                        JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };

                        TakeoutFileEntry fileEntry = new TakeoutFileEntry();

                        try
                        {

                            fileEntry = JsonSerializer.Deserialize<TakeoutFileEntry>(entry.Open(), jsonSerializerOptions);
                        }
                        catch
                        { }


                        if (fileEntry.CreationTime != null) // file could also be a json file not related to a file, then the metadata file for it is .json.json lols
                        {
                            DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(fileEntry.CreationTime.Timestamp)).UtcDateTime;

                            string newPath = Path.ChangeExtension(entry.FullName, null);

                            FileEntries.AddEntry(newPath, dateTime);
                        }
                        else FileEntries.AddEntry(entry.FullName, entry);
                    }
                    else
                    {
                        FileEntries.AddEntry(entry.FullName, entry);
                    }
                }
                FileListView.ItemsSource = FileEntries.Entries;
                NotifyPropertyChanged("FileCount");
                DirectoryTreeNodes = BuildDirectoryTree(FileEntries);
                DirectoryTree.ItemsSource = DirectoryTreeNodes;
            }
        }

        private ObservableCollection<DirectoryTreeNode> BuildDirectoryTree(FileEntryList fileEntries)
        {
            foreach (FileEntry fileEntry in fileEntries.Entries)
            {
                var pathSegments = fileEntry.Path.Replace('\\', '/').Split('/');
                AddPathToTree(rootNodes, pathSegments, 0);
            }

            return rootNodes;
        }

        private void AddPathToTree(ObservableCollection<DirectoryTreeNode> currentLevel, string[] segments, int index)
        {
            if (index >= segments.Length) return;

            var segment = segments[index];
            var existingNode = currentLevel.FirstOrDefault(n => n.Name == segment);

            if (existingNode == null)
            {
                existingNode = new DirectoryTreeNode { Name = segment };
                currentLevel.Add(existingNode);
            }

            AddPathToTree(existingNode.Children, segments, index + 1);
        }

        private BitmapImage LoadImageFromStream(Stream stream)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // Ensures the stream can be closed after loading
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            if (bitmap.CanFreeze)
            {
                bitmap.Freeze();
            }//bitmap.Freeze(); // Makes the image thread-safe
            return bitmap;
        }

        private Dictionary<FileEntry, UIElement> elements = new Dictionary<FileEntry, UIElement>();

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (FileEntry item in e.RemovedItems)
            {
                if (elements.ContainsKey(item)) EntryPreview.Children.Remove(elements[item]);
            }

            foreach (FileEntry entry in e.AddedItems)
            {
                if (entry.ZipEntry != null)
                {
                    try
                    {
                        Stream fileStream = entry.ZipEntry.Open();
                        Image image = new Image();
                        image.Source = LoadImageFromStream(fileStream);

                        EntryPreview.Children.Add(image);
                        elements[entry] = image;
                    } catch
                    { }
                }
            }
        }

        private void GridView_ColumnClick(object sender, RoutedEventArgs e)
        {
            DataGridColumnHeader column = sender as DataGridColumnHeader;

            ICollectionView collectionView = CollectionViewSource.GetDefaultView(FileListView.ItemsSource);
            if (collectionView != null)
            {
                collectionView.SortDescriptions.Clear();
                string sortBy = column.Tag.ToString();
                collectionView.SortDescriptions.Add(new SortDescription(sortBy, ListSortDirection.Ascending));
            }
        }

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
