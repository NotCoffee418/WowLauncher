using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.ComponentModel;
using System.Net;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Windows.Threading;

namespace WowLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker worker = new BackgroundWorker();
        bool upToDatePatch = true;

        public MainWindow()
        {
            InitializeComponent();

            // Worker for checking installation and downloading patch
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.ProgressChanged += backgroundWorker1_ProgressChanged;
            worker.WorkerReportsProgress = true;

            worker.RunWorkerAsync();
        }


        /// <summary>
        /// Start button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Properties.Settings.Default.WowDir + "\\Wow.exe");
        }
        
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get correct wow dir
            SetStatusText("Gathering info about WoW installation...");
            CheckWowDir();
            worker.ReportProgress(10);

            // Set Realmlist if needed
            SetStatusText("Changing realmlist...");
            SetRealmlist();
            worker.ReportProgress(20);

            // Check if patch is installed
            if (isPatchUpdateNeeded())
                upToDatePatch = InstallPatch();
                     
        }

        

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (upToDatePatch)
            {
                progressBar1.Value = 100;
                SetStatusText("Ready to play!");
                startButton.IsEnabled = true;
            }
            else
            {
                SetStatusText("Error: Make sure your launcher is up to date.");
            }   

            // autostart if checked
        }

        /// <summary>
        /// Updates status text from different thread.
        /// </summary>
        /// <param name="text"></param>
        private void SetStatusText(string text)
        {
            Application.Current.Dispatcher.BeginInvoke(
              DispatcherPriority.Background,
              new Action(() => this.statusLabel.Content = text));
        }


        private string CheckWowDir()
        {
            // get dir from config
            string WowDir = Properties.Settings.Default.WowDir;
            string launcherDir = Directory.GetCurrentDirectory();

            // if wow dir is not set
            if (WowDir == String.Empty)
            {
                if (File.Exists(launcherDir + "\\Wow.exe"))
                {
                    WowDir = launcherDir;
                }
                else
                {
                    // Create OpenFileDialog 
                    Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                    dlg.DefaultExt = "Wow.exe";
                    dlg.Filter = "Wow.exe|Wow.exe";
                    Nullable<bool> result = dlg.ShowDialog();
                    if (result == true)
                        WowDir = System.IO.Path.GetDirectoryName(dlg.FileName);
                }
            }
            else 
            {
                // Check if correct
                try
                {
                    string WowVersion = FileVersionInfo.GetVersionInfo(WowDir + "\\Wow.exe").FileVersion;
                    if (WowVersion != Properties.Settings.Default.WowVersion)
                    {
                        MessageBox.Show(
                            "The World of Warcraft installation you selected is not version " + Properties.Settings.Default.WowVersion,
                            "Invalid Version", MessageBoxButton.OK, MessageBoxImage.Error);
                        WowDir = "";
                    }
                    else return WowDir; // Nothing is wrong
                }
                catch (Exception e) // Protection if wow dir moves
                {
                    WowDir = "";
                }
            }

            // if changes were made save & check for wow version
            Properties.Settings.Default.WowDir = WowDir;
            Properties.Settings.Default.Save();
            return CheckWowDir();
        }

        private string FindRealmlist()
        {
            if (Properties.Settings.Default.RealmlistFile != "")
                return Properties.Settings.Default.RealmlistFile;
            
            string realmlist = "";
            bool asked = false;

            var files = Directory.EnumerateFiles(Properties.Settings.Default.WowDir, "*.*", SearchOption.AllDirectories);
            foreach (string f in files)
            {
                if (!asked && System.IO.Path.GetFileName(f) == "realmlist.wtf")
                {
                    if (realmlist == "")
                        realmlist = f;
                    else // user has multiple realmlist files, ask for correct one.
                    {
                        Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                        dlg.DefaultExt = "realmlist.wtf";
                        dlg.Filter = "realmlist.wtf|realmlist.wtf";
                        Nullable<bool> result = dlg.ShowDialog();
                        if (result == true)
                        {
                            asked = true;
                            realmlist = dlg.FileName;
                        }
                    }
                }
            }

            Properties.Settings.Default.RealmlistFile = realmlist;
            Properties.Settings.Default.Save();
            return realmlist;
        }

        private void SetRealmlist()
        {
            string realmlist = FindRealmlist();
            string[] lines = File.ReadAllLines(realmlist);
            List<string> result = new List<string>();
            bool changed = false;
            bool added = false;

            foreach (string l in lines)
            {
                string r = l;
                if (r == "set realmlist " + Properties.Settings.Default.ServerIP)
                    added = true; // realmlist already addeed
                else if (l.Count() > 0)
                {
                    if (r == "#set realmlist " + Properties.Settings.Default.ServerIP)
                        r = ""; // remove commented line for this server 
                    else if (l[0] != '#')
                        r = '#' + l; // comment out other realmlists
                    changed = true;
                }

                // Add to list if valid line
                if (r != "") result.Add(r);
            }

            if (!added)
            {
                result.Add("set realmlist " + Properties.Settings.Default.ServerIP);
                changed = true;
            }

            if (changed) File.WriteAllLines(realmlist, result);
        }

       

        private bool isPatchUpdateNeeded()
        {
            return false;
        }

        private bool InstallPatch()
        {
            throw new NotImplementedException();
        }
    }
}
