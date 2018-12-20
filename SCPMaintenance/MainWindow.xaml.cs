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
using System.ServiceProcess;

using System.IO;
using System.Threading;
using System.ComponentModel;


namespace SCPMaintenance
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
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
        int timeoutMilliseconds = 10000;
        static bool run = true;
        
        public MainWindow()
        {
            InitializeComponent();

            fileDialog.Multiselect = true;          

            foreach (ServiceController service in services)
            {
                listBoxServices.Items.Add(service.ServiceName);
            }

            bgw.DoWork += new DoWorkEventHandler(bgw_DoWork);
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

           
            if (bgw.IsBusy)
            {
                MessageBoxResult dialogResult = MessageBox.Show("You've already started monitoring, would you like to restart?", "Monitoring Already In Progress", MessageBoxButton.YesNoCancel);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    run = false;
                    bgw.CancelAsync();
                    while (bgw.CancellationPending)
                    {

                    }
                    bgw.RunWorkerAsync();
                }
            }
            else
            {
               
                bgw.RunWorkerAsync(args);
            }

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
            List<object> obj = e.Argument as List<object>;

            MonitorFiles(obj[0] as List<string>, obj[1] as string);
                         
        }   
    
        private void MonitorFiles(List<string> serviceNames, string _searchText)
        {
            while (run)
            {
                try
                {
                    GetFileText();

                    for (int k = 0; k < serviceNames.Count; k++)
                    {
                        for (int m = 0; m < services.Count(); m++)
                        {
                            if (serviceNames[k].ToString() == services[m].ServiceName)
                            {
                                for (int i = 0; i < fileText.Count(); i++)
                                {
                                    if (fileText[i].Contains(_searchText))
                                    {
                                        //TODO find last instance and make sure new finding is newer                                       
                                        SetMonitorText("Text match found, restarting service: " + services[m].DisplayName);

                                        int millisec1 = Environment.TickCount;
                                        TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                                        ServiceStatusSwitch: switch (services[m].Status)
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
                                    else
                                    {
                                        //txtBoxMonitor.Text = "No matches found yet.";
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

        private void GetFileText()
        {
            fileText.Clear();

            foreach(var file in fileDialog.FileNames)
            {
               foreach(var line in File.ReadLines(file))
                {
                    fileText.Add(line);
                }
            }          
        }

        private void BtnStopMonitor_Click(object sender, RoutedEventArgs e)
        {
            run = false;
            bgw.CancelAsync();
        }
    }            
}       
