using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.ServiceProcess;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Globalization;

namespace SCPMaintenance
{ 
    public partial class SCPServiceMonitor : Window
    {
        System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
        System.Windows.Forms.DialogResult dialogResult = new System.Windows.Forms.DialogResult();
        ServiceController[] services = ServiceController.GetServices();
        List<String> fileText = new List<String>();
        List<String> serviceNames = new List<string>();
        private delegate void TextChanger(string s);

        BackgroundWorker bgw = new BackgroundWorker();
        private delegate void ListBoxGetter(System.Windows.Controls.ListBox _lb);

        DateTime timeOfAppStart = DateTime.Now;
        DateTime lastLogTextMatch;
        int timeoutMilliseconds = 10000;
        static bool run = true;
        bool firstRun = true;
        
        public SCPServiceMonitor()
        {
            InitializeComponent();

            fileDialog.Multiselect = true;          

            foreach (ServiceController service in services)
            {
                listBoxServices.Items.Add(service.ServiceName);
            }

            bgw.DoWork += new DoWorkEventHandler(bgw_DoWork);
            bgw.RunWorkerCompleted += bgw_RunWorkerCompleted;
            bgw.WorkerSupportsCancellation = true;
            
            bgw.WorkerReportsProgress = false;
        }

        private void btnFileSelect_Click(object sender, RoutedEventArgs e)
        {
            dialogResult = fileDialog.ShowDialog();

            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                foreach (String file in fileDialog.FileNames)
                {
                    listBoxFiles.Items.Add(file);
                }
            }
        }

        private void btnStartMonitor_Click(object sender, RoutedEventArgs e)
        {

            if(listBoxServices.SelectedIndex == -1)
            {
                MessageBox.Show("Select a service(s) to restart.", "No service selected.", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }
            else if (listBoxFiles.Items.Count == 0)
            {
                MessageBox.Show("Select a file to monitor.", "No file(s) selected.", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }
            else if(string.IsNullOrWhiteSpace(txtBoxSearchText.Text))
            {
                MessageBox.Show("Enter search text.", "No text to search for enetered.", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }

            foreach (string s in listBoxServices.SelectedItems)
            {
                serviceNames.Add(s);
            }

            string m = txtBoxSearchText.Text;

            List<object> args = new List<object>();
            args.Add(serviceNames);
            args.Add(m);
                       
            bgw.RunWorkerAsync(args);
            btnStartMonitor.IsEnabled = false;

            run = true;                      
        }

        public void SetMonitorText(string s)
        {
            if (this.txtBoxMonitor.Dispatcher.CheckAccess())
            {
                this.txtBoxMonitor.Text = s;
            }
            else
            {
                this.txtBoxMonitor.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new TextChanger(this.SetMonitorText), s);
            }
        }

        private void bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            //Abort thread if cancelation requested
            if(bgw.CancellationPending)
            {
                e.Cancel = true;
                return;
            }           

            List<object> obj = e.Argument as List<object>;

            MonitorFiles(obj[0] as List<string>, obj[1] as string);                         
        }   
    
        private void MonitorFiles(List<string> serviceNames, string _searchText)
        {            
            while (run)
            {
                try
                {
                    GetFileText(_searchText);

                    for (int k = 0; k < serviceNames.Count; k++)
                    {
                        for (int m = 0; m < services.Count(); m++)
                        {
                            if (serviceNames[k].ToString() == services[m].ServiceName)
                            {
                                for (int i = 0; i < fileText.Count(); i++)
                                {                                   
                                    //TODO find last instance and make sure new finding is newer 
                                    StringBuilder sb = new StringBuilder();
                                    DateTime currentLogTextMatchTimeStamp = new DateTime();

                                    for (int p = 0; p < 23; p++)
                                    {
                                        sb.Append(fileText[i][p]);
                                    }

                                    try
                                    {                                           
                                        string[] formats = new[] { "yyyy-MM-dd HH:mm:ss,fff" };
                                        DateTime.TryParseExact(sb.ToString(), formats, CultureInfo.InvariantCulture,
                                                            DateTimeStyles.None, out currentLogTextMatchTimeStamp);
                                    }
                                    catch(Exception e)
                                    {
                                        MessageBox.Show("There was an error determining timestamp in log file, please verify the files selected to monitor are log files.", "Log File Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                                    }

                                    if (firstRun)
                                    {
                                        if (currentLogTextMatchTimeStamp > timeOfAppStart)
                                        {
                                            lastLogTextMatch = currentLogTextMatchTimeStamp;

                                            ManageService(services, m);

                                            firstRun = false;
                                        }
                                    }
                                    else if(!firstRun)
                                    {
                                        if (currentLogTextMatchTimeStamp > lastLogTextMatch)
                                        {
                                            lastLogTextMatch = currentLogTextMatchTimeStamp;

                                            ManageService(services, m);
                                        }
                                    } 
                                }
                            }
                        }
                    }
                }catch (Exception e)
                {

                }
            }
        }

        private void ManageService(ServiceController[] services, int m)
        {
            SetMonitorText("Text match found, restarting service: " + services[m].DisplayName);

            Thread.Sleep(500);

            int millisec1 = Environment.TickCount;
            TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

            switch (services[m].Status)
            {
                case ServiceControllerStatus.Running:
                    SetMonitorText("Service already running, stopping service.");
                    services[m].Stop();
                    services[m].WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                    SetMonitorText("Starting service.");
                    services[m].Start();
                    services[m].WaitForStatus(ServiceControllerStatus.Running, timeout);
                    SetMonitorText("Complete.");
                    break;
                case ServiceControllerStatus.Stopped:
                    SetMonitorText("Service already stopped starting service.");
                    services[m].Start();
                    services[m].WaitForStatus(ServiceControllerStatus.Running, timeout);
                    SetMonitorText("Complete.");
                    break;
                case ServiceControllerStatus.Paused:
                    SetMonitorText("Service currently paused restarting service.");
                    services[m].Stop();
                    services[m].WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                    SetMonitorText("Starting service.");
                    services[m].Start();
                    services[m].WaitForStatus(ServiceControllerStatus.Running, timeout);
                    SetMonitorText("Complete.");
                    break;
                case ServiceControllerStatus.StopPending:
                    SetMonitorText("Service has stop pending, starting after stop.");
                    services[m].WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                    services[m].Start();
                    services[m].WaitForStatus(ServiceControllerStatus.Running, timeout);
                    SetMonitorText("Complete.");
                    break;
                case ServiceControllerStatus.StartPending:
                    SetMonitorText("Service currently pending start, restarting when complete.");
                    services[m].WaitForStatus(ServiceControllerStatus.Running, timeout);
                    services[m].Stop();
                    services[m].WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                    SetMonitorText("Starting service.");
                    services[m].Start();
                    services[m].WaitForStatus(ServiceControllerStatus.Running, timeout);
                    SetMonitorText("Complete.");
                    break;
                default:
                    SetMonitorText("Unable to determine service state.");
                    break;
            }
        }

        private void GetFileText(string textToFind)
        {
            string line;

            fileText.Clear();        

            foreach (var file in fileDialog.FileNames)
            {              
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        while ((line = sr.ReadLine()) != null)
                        {
                            if(line.Contains(textToFind))
                            fileText.Add(line);
                        }
                    }
                }
            }            
        }

        private void BtnStopMonitor_Click(object sender, RoutedEventArgs e)
        {
            run = false;
            bgw.CancelAsync();
            lblPending.Content ="Stop Pending...";
        }
         

        private void bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lblPending.Content = "";
            btnStartMonitor.IsEnabled = true;
        }
    }            
}       
