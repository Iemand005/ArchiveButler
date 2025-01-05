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

namespace ArchiveButler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileEntryList fileEntries = new FileEntryList();

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
                        if (Path.GetExtension(entry.Name).Equals(".json"))
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

                                fileEntries.AddEntry(entry.FullName, dateTime);
                            }
                        }
                        else
                        {
                            fileEntries.AddEntry(entry.FullName);
                        }
                    }
                    FileListView.ItemsSource = fileEntries.Entries;
                }
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
