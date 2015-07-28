using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
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
using System.Windows.Shapes;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;


namespace MameFileWatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.changeEventTimerHi.Interval = 1000;
            this.changeEventTimerHi.Elapsed += changeEventTimer_Elapsed;
            this.changeEventTimerHi.AutoReset = false;

            this.changeEventTimerNv.Interval = 1000;
            this.changeEventTimerNv.Elapsed += changeEventTimer_Elapsed;
            this.changeEventTimerNv.AutoReset = false;

            this.WatchFolders();
        }

        void changeEventTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("Timer finished");
        }

        private void btnHiDirectory_Click(object sender, RoutedEventArgs e)
        {
            this.SelectFolder(this.tbHiPath);
        }

        private void btnNvDirectory_Click(object sender, RoutedEventArgs e)
        {
            this.SelectFolder(this.tbNvPath);
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            using (var client = new WebClient())
            {
                try
                {
                    Uri baseUrl = new Uri(this.tbSiteUrl.Text);
                    Uri serverAddress = new Uri(baseUrl, "/game/upload_test/" + this.tbApiKey.Text);

                    byte[] myDataBuffer = client.DownloadData(serverAddress);

                    // Display the downloaded data. 
                    string download = Encoding.ASCII.GetString(myDataBuffer);

                    string messageBoxText = "Everything looks ok";
                    string caption = "Success!";
                    MessageBoxButton button = MessageBoxButton.OK;
                    MessageBoxImage icon = MessageBoxImage.Information;

                    MessageBox.Show(messageBoxText, caption, button, icon);

                    Console.WriteLine(client.ResponseHeaders.ToString());
                    Console.WriteLine(download);
                }
                catch (WebException webEx)
                {
                    var resp = new StreamReader(webEx.Response.GetResponseStream()).ReadToEnd();
                    
                    System.Web.Script.Serialization.JavaScriptSerializer jsSer = new System.Web.Script.Serialization.JavaScriptSerializer();

                    Dictionary<string, object> error = (Dictionary < string, object> )jsSer.DeserializeObject(resp);

                    string messageBoxText = (string)error["error"];
                    if (error.ContainsKey("error"))
                    {
                        messageBoxText = (string)error["error"];
                    }
                    else
                    {
                        messageBoxText = "Unknown Error";
                    }
                    
                    string caption = webEx.Message;
                    MessageBoxButton button = MessageBoxButton.OK;
                    MessageBoxImage icon = MessageBoxImage.Error;

                    MessageBox.Show(messageBoxText, caption, button, icon);
                }
                catch (Exception ex)
                {
                    string messageBoxText = "Somthing is not right. Error was: " + ex.Message;
                    string caption = "Error!";
                    MessageBoxButton button = MessageBoxButton.OK;
                    MessageBoxImage icon = MessageBoxImage.Error;

                    MessageBox.Show(messageBoxText, caption, button, icon);
                }

            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnUploadFile_Click(object sender, RoutedEventArgs e)
        {

            //TODO: need to work out the interface for manually selecting files
            //the user needs to be able to privide a game name, or have some smarts and try and work it out


            //allow a user to manually upload a file
            //Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            
            //Nullable<bool> result = dlg.ShowDialog();
                        
            //if (result == true)
            //{
            //    // Open document 
            //    string filename = dlg.FileName;
            //    this.UploadFile(dlg.FileName, "");
            //}
        }

        private StreamContent CreateFileContent(Stream stream, string fileName, string contentType)
        {
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"file\"",
                FileName = "\"" + fileName + "\""
            }; // the extra quotes are key here
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            return fileContent;
        }

        // Perform the equivalent of posting a form with a filename and two files, in HTML:
        // <form action="{url}" method="post" enctype="multipart/form-data">
        //     <input type="text" name="filename" />
        //     <input type="file" name="file1" />
        //     <input type="file" name="file2" />
        // </form>
        private Stream Upload(string url, string filename)
        {


            var fileContent = this.CreateFileContent(
                new FileStream(filename, System.IO.FileMode.Open), 
                System.IO.Path.GetFileName(filename), 
                "application/octet-stream");
            
            using (var client = new HttpClient())
            using (var formData = new MultipartFormDataContent())
            {   
                formData.Add(fileContent);
                var response = client.PostAsync(url, formData).Result;

                // equivalent of pressing the submit button on the form
                if (!response.IsSuccessStatusCode)
                {
                    //return null;
                }
                return response.Content.ReadAsStreamAsync().Result;
            }
        }

        private void UploadFile(string filePath, string gameName)
        {
            using (var client = new WebClient())
            {
                try
                {
                    Uri baseUrl = new Uri(this.tbSiteUrl.Text);
                    Uri serverAddress = new Uri(baseUrl, "/game/upload/" + this.tbApiKey.Text + "?gamename=" + gameName);
                    
                    using (Stream s = this.Upload(serverAddress.ToString(), filePath))
                    using (StreamReader sr = new StreamReader(s))
                    {
                        Console.WriteLine(sr.ReadToEnd());
                    }

                }
                catch (WebException ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine("Problem uploading file. Exception: " + ex.Message);

                    if (ex.Response != null)
                    {
                        using (StreamReader objStream = new StreamReader(ex.Response.GetResponseStream()))
                        {
                            var resp = objStream.ReadToEnd();

                            objStream.Close();
                            ex.Response.Close();
                            ex.Response.GetResponseStream().Close();
                            Console.WriteLine("Body: " + resp);
                        }
                    }
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Problem uploading file. Exception: " + ex.Message);
                }
            }


        }

        private void SelectFolder(TextBox textBoxControl)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                textBoxControl.Text = dialog.SelectedPath;
            }
        }
        
        private void WatchFolders()
        {
            string hiPath = this.tbHiPath.Text;
            string nvPath = this.tbNvPath.Text;
            
            FileSystemWatcher hiWatcher = new FileSystemWatcher(hiPath, "*.hi");
            FileSystemWatcher nvWatcher = new FileSystemWatcher(nvPath);

            hiWatcher.NotifyFilter = NotifyFilters.LastWrite;            
            hiWatcher.Changed += new FileSystemEventHandler(FileWatcherOnChanged);
            hiWatcher.Created += new FileSystemEventHandler(FileWatcherOnChanged);
            hiWatcher.EnableRaisingEvents = true;

            nvWatcher.NotifyFilter = NotifyFilters.LastWrite;
            nvWatcher.IncludeSubdirectories = true;

            nvWatcher.Changed += new FileSystemEventHandler(FileWatcherOnChanged);
            nvWatcher.Created += new FileSystemEventHandler(FileWatcherOnChanged);
            nvWatcher.EnableRaisingEvents = true;

        }
        
        private System.Timers.Timer changeEventTimerHi = new System.Timers.Timer();
        private System.Timers.Timer changeEventTimerNv = new System.Timers.Timer();

        private static bool processingHiFile = false;
        private static bool processingNvFile = false;


        // Define the event handlers. 
        private void FileWatcherOnChanged(object source, FileSystemEventArgs e)
        {
            string fileExt = System.IO.Path.GetExtension(e.Name);
            string filePath = System.IO.Path.GetDirectoryName(e.FullPath);
            string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(e.Name);

            Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);

            //lets wait for a bit before processing the file as windows might still be 
            //doing some more file event that can cause file locked exceptions
            //super hacky, probably want some sort of queue system or only allow one file event at a time
            System.Threading.Thread.Sleep(100); 


            //need to push to the ui thread so we can read the text boxes
            this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
            {
                //handling a .hi file
                if (filePath == tbHiPath.Text)
                {
                    if (!this.changeEventTimerHi.Enabled)
                    {
                        this.changeEventTimerHi.Enabled = true;
                        this.UploadFile(e.FullPath, fileNameWithoutExt);
                    }
                }
            });

            //need to push to the ui thread so we can read the text boxes
            this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
            {
                if (filePath == tbNvPath.Text)
                {
                    if (!this.changeEventTimerNv.Enabled)
                    {
                        //if the nv files are in sub folder then we need to use the folder name to work out the game name
                        string[] directories = filePath.Split(System.IO.Path.DirectorySeparatorChar);

                        bool useSubFolders = cbNvSubFolder.IsChecked.HasValue && (bool)cbNvSubFolder.IsChecked;
                        string gameName = useSubFolders ? directories.Last() : fileNameWithoutExt;

                        this.changeEventTimerNv.Enabled = true;

                        this.UploadFile(e.FullPath, gameName);
                    }
                }
            });


        }

        private static void FileWatcherOnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
        }

        
    }
}
