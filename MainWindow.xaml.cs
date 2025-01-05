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

namespace ArchiveButler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileEntryList FileEntries = new FileEntryList();
        internal ObservableCollection<DirectoryTreeNode> DirectoryTreeNodes { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog(this) ?? false)
            {
                MessageBox.Show(openFileDialog.FileName);

                using (ZipArchive archive = ZipFile.Open(openFileDialog.FileName, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        //MessageBox.Show(Path.GetExtension(entry.Name));
                        if (Path.GetExtension(entry.Name).Equals(".json") || false)
                        {
                            //MessageBox.Show(entry.Name);

                            JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            };

                            TakeoutFileEntry fileEntry = JsonSerializer.Deserialize<TakeoutFileEntry>(entry.Open(), jsonSerializerOptions);

                            //MessageBox.Show(fileEntry.Title);
                            if (fileEntry.CreationTime != null) // file could also be a json file not related to a file, then the metadata file for it is .json.json lols
                            {
                                DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(fileEntry.CreationTime.Timestamp)).UtcDateTime;

                                string newPath = Path.ChangeExtension(entry.FullName, null);

                                FileEntries.AddEntry(newPath, dateTime);
                            }
                        }
                        else
                        {
                            FileEntries.AddEntry(entry.FullName);
                        }
                    }
                    FileListView.ItemsSource = FileEntries.Entries;
                    DirectoryTreeNodes = BuildDirectoryTree(FileEntries);
                    DirectoryTree.ItemsSource = DirectoryTreeNodes;
                    //FileListView.Sort()
                }
            }
        }

        private ObservableCollection<DirectoryTreeNode> BuildDirectoryTree(FileEntryList fileEntries)
        {
            var rootNodes = new ObservableCollection<DirectoryTreeNode>();

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

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void GridView_ColumnClick(object sender, RoutedEventArgs e)
        {
            //ListViewItemComparer sorter = GetListViewSorter(e.Column);

            //listView.ListViewItemSorter = sorter;
            //listView.Sort();
            //ICollectionView collectionView = CollectionViewSource.GetDefaultView(FileListView.ItemsSource);
            //collectionView.SortDescriptions.Clear();
            //collectionView.SortDescriptions.Add(new SortDescription(

            GridViewColumnHeader column = sender as GridViewColumnHeader;

            ICollectionView collectionView = CollectionViewSource.GetDefaultView(FileListView.ItemsSource);
            if (collectionView != null)
            {
                collectionView.SortDescriptions.Clear();
                string sortBy = column.Tag.ToString();
                collectionView.SortDescriptions.Add(new SortDescription(sortBy, ListSortDirection.Ascending));
            }
        }
    }
}
