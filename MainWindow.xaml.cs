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
using System.IO.Pipes;
//using static System.Net.Mime.MediaTypeNames;

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

            var a = LoadDatabase();
            FileEntries.Merge(a);
            NotifyPropertyChanged("FileEntries");
            NotifyPropertyChanged("FileEntries.Entries");
            NotifyPropertyChanged("FileCount");
            FileListView.ItemsSource = FileEntries.Entries;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog(this) ?? false)
            {
                Task.Factory.StartNew(() =>
                {
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
                        //FileListView.ItemsSource = FileEntries.Entries;
                        NotifyPropertyChanged("FileCount");
                        NotifyPropertyChanged("FileEntries");

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
                            string newPath = Path.ChangeExtension(entry.FullName, null);
                            FileEntry fileEntry1 = FileEntries.GetEntry(entry);
                            if (fileEntry1 == null || !fileEntry1.Date.HasValue)
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


                                if (fileEntry != null && fileEntry.CreationTime != null) // file could also be a json file not related to a file, then the metadata file for it is .json.json lols
                                {
                                    DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(fileEntry.CreationTime.Timestamp)).UtcDateTime;


                                    FileEntries.SetEntryDate(newPath, dateTime);
                                    isFile = false;
                                }
                            }
                        }
                        else isFile = false;
                    }

                    if (isFile)
                    {
                        FileEntries.AddEntry(entry);
                        fileCount++;
                    } else metaCount++;

                    iteration++;
                    if (iteration == callbackInterval)
                    {
                        iteration = 0;
                        entryLoaded(fileCount, metaCount);
                    }
                }
                SaveDatabase(FileEntries);
            }
            entryLoaded(fileCount, metaCount);
        }

        public string DatabaseName { get; set; } = "database.json";

        private void SaveDatabase(FileEntryList fileEntries)
        {
            File.WriteAllText(DatabaseName, JsonSerializer.Serialize(fileEntries));
        }

        private FileEntryList LoadDatabase()
        {
            if (File.Exists(DatabaseName))
            try
            {
                FileEntries.Merge(JsonSerializer.Deserialize<FileEntryList>(File.ReadAllText(DatabaseName)));
                //return File.Exists(DatabaseName) ?  : FileEntries;
            } catch
            {
                MessageBox.Show("failed to load database barf");
            }
            return FileEntries;
        }

        private ObservableCollection<DirectoryTreeNode> BuildDirectoryTree(FileEntryList fileEntries)
        {
            foreach (FileEntry fileEntry in fileEntries.Entries)
            {
                var pathSegments = fileEntry.Path.Replace('\\', '/').Split('/');
                //pathSegments = fileEntry.FullName.Split('/');
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

        private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".ico"
    };

        private static readonly HashSet<string> TextExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".log", ".csv", ".json", ".xml", ".html"
    };
        private static readonly HashSet<string> VideoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v"
    };

        public UIElement OpenFileBasedOnExtension(string fileName, Stream stream)
        {
            UIElement element = new UIElement();
            string extension = Path.GetExtension(fileName);

            if (ImageExtensions.Contains(extension))
            {
                Image image = new Image();
                image.Source = LoadImageFromStream(stream);
                element = image;
            }
            else if (VideoExtensions.Contains(extension))
            {
                //MediaElement video = new MediaElement();
                //video.Source
            }
            else
            {
                TextBlock textBlock = new TextBlock();
                textBlock.Text = new StreamReader(stream).ReadToEnd();
                element = textBlock;
                //OpenAsText(fileName);
            }

            return element;
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

            foreach (object item in e.AddedItems)
                if (item is FileEntry)
                {
                    FileEntry entry = item as FileEntry;
                    if (entry.ZipEntry != null) try
                        {
                            Stream fileStream = entry.ZipEntry.Open();
                            var element = OpenFileBasedOnExtension(entry.Name, fileStream);
                            EntryPreview.Children.Add(element);
                            elements[entry] = element;
                        }
                        catch { }
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
