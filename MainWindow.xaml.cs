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

        public bool LoadFileDates { get; set; } = false;

        public long FileCount {
            get
            {
                return FileEntries.Entries.Count;
            }
        }

        public long LoadingFileCount { get; set; } = 0;
        public long LoadingMetaCount { get; set; } = 0;

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
                Task.Factory.StartNew(() =>
                {
                    //LoadTakeoutFiles(openFileDialog.FileNames, () =>
                    //{

                    //});

                    LoadTakeoutArchives(openFileDialog.FileNames, ref FileEntries, (long fileCount, long metaCount) =>
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            LoadingFileCount = fileCount;
                            LoadingMetaCount = metaCount;

                            NotifyPropertyChanged("LoadingFileCount");
                            NotifyPropertyChanged("LoadingMetaCount");
                        }));
                    });

                    Dispatcher.Invoke(new Action(() =>
                    {
                        FileListView.ItemsSource = FileEntries.Entries;
                        NotifyPropertyChanged("FileCount");
                        DirectoryTreeNodes = BuildDirectoryTree(FileEntries);
                        DirectoryTree.ItemsSource = DirectoryTreeNodes;
                    }));
                });
            }
        }

        public delegate void EntryLoadedCallBack(long fileCount, long metaCount);

        private void LoadTakeoutArchives(string[] fileNames, ref FileEntryList fileList, EntryLoadedCallBack entryLoaded, ulong callbackInterval = 1000)
        {
            long fileCount = 0;
            long metaCount = 0;
            ulong iteration = 0;
            foreach (string fileName in fileNames)
            {

                ZipArchive archive = ZipFile.Open(fileName, ZipArchiveMode.Read);

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    bool isFile = true;
                    if (Path.GetExtension(entry.Name).Equals(".json"))
                    {
                        if (LoadFileDates)
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
                            catch { }


                            if (fileEntry.CreationTime != null) // file could also be a json file not related to a file, then the metadata file for it is .json.json lols
                            {
                                DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(fileEntry.CreationTime.Timestamp)).UtcDateTime;

                                string newPath = Path.ChangeExtension(entry.FullName, null);

                                FileEntries.AddEntry(newPath, dateTime);
                                isFile = false;
                            }
                        }
                        else isFile = false;
                    }

                    if (isFile)
                    {
                        FileEntries.AddEntry(entry.FullName, entry);
                        fileCount++;
                    } else metaCount++;

                    iteration++;
                    if (iteration == callbackInterval)
                    {
                        iteration = 0;
                        entryLoaded(fileCount, metaCount);
                    }
                }
            }
            entryLoaded(fileCount, metaCount);
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
                string sortBy = "Name";
                switch (column.Name)
                {
                    case "DateColumn":
                        sortBy = "CreationTime";
                        break;
                }
                
                ListSortDirection direction = ListSortDirection.Ascending;
                if (collectionView.SortDescriptions.Count > 0)
                {
                    SortDescription sortDescription = collectionView.SortDescriptions[0];
                    direction = sortDescription.Direction;
                    if (sortDescription.PropertyName == sortBy)
                        direction = direction == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                }
                collectionView.SortDescriptions.Clear();
                
                collectionView.SortDescriptions.Add(new SortDescription(sortBy, direction));
            }
        }

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
