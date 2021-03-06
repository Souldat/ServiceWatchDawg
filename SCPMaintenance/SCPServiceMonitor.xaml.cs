﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.ServiceProcess;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

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
        int retryCount = 0;

        BackgroundWorker bgw = new BackgroundWorker();
        private delegate void ListBoxGetter(System.Windows.Controls.ListBox _lb);

        DateTime timeOfAppStart = DateTime.Now;
        DateTime lastLogTextMatch;
        int timeoutMilliseconds = 20000;
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
                    if (file.Contains(".log") || file.Contains(".txt"))
                    {
                        listBoxFiles.Items.Add(file);
                    }
                }
            }
        }

        private void btnStartMonitor_Click(object sender, RoutedEventArgs e)
        {
            if (listBoxServices.SelectedIndex == -1)
            {
                MessageBox.Show("Select a service(s) to restart.", "No service selected", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }
            else if (listBoxFiles.Items.Count == 0)
            {
                MessageBox.Show("Select a file to monitor.", "No file(s) selected", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }
            else if(string.IsNullOrWhiteSpace(txtBoxSearchText.Text))
            {
                MessageBox.Show("Enter search text.", "No text to search for enetered", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }

            lblPending.Content = "Monitoring...";
            firstRun = true;

            serviceNames.Clear();
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
                    if (GetFileText(_searchText))
                    {
                        ScanServicesToManage();
                    }  
                }
                catch (Exception e)
                {
                    SetMonitorText("File Monitor Main Failure: " + e.ToString());
                }
            }
        }

        public void ScanServicesToManage()
        {
            //lastLogTextMatch = currentLogTextMatchTimeStamp;

            for (int k = 0; k < serviceNames.Count; k++)
            {
                for (int m = 0; m < services.Count(); m++)
                {
                    if (serviceNames[k].ToString() == services[m].ServiceName)
                    {
                        ManageService(services, m);
                    }
                }
            }
        }

        private void ManageService(ServiceController[] services, int m)
        {
            try
            {
                SetMonitorText("Text match found, restarting service: " + services[m].DisplayName);

                Thread.Sleep(500);

                int millisec1 = Environment.TickCount;
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                switch (services[m].Status)
                {
                    case ServiceControllerStatus.Running:
                        SetMonitorText("Service already running, stopping service: " + services[m].DisplayName);
                        services[m].Stop();
                        services[m].WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                        SetMonitorText("Starting service: " + services[m].DisplayName);
                        services[m].Start();
                        services[m].WaitForStatus(ServiceControllerStatus.Running, timeout);
                        SetMonitorText("Complete.");
                        break;
                    case ServiceControllerStatus.Stopped:
                        SetMonitorText("Service already stopped starting service: " + services[m].DisplayName);
                        services[m].Start();
                        services[m].WaitForStatus(ServiceControllerStatus.Running, timeout);
                        SetMonitorText("Complete.");
                        break;
                    case ServiceControllerStatus.Paused:
                        SetMonitorText("Service currently paused stopping service: " + services[m].DisplayName);
                        services[m].Stop();
                        services[m].WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                        SetMonitorText("Starting service: " + services[m].DisplayName);
                        services[m].Start();
                        services[m].WaitForStatus(ServiceControllerStatus.Running, timeout);
                        SetMonitorText("Complete.");
                        break;
                    case ServiceControllerStatus.StopPending:
                        SetMonitorText("Service has stop pending, starting after stop: " + services[m].DisplayName);
                        services[m].WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                        SetMonitorText("Starting service: " + services[m].DisplayName);
                        services[m].Start();
                        services[m].WaitForStatus(ServiceControllerStatus.Running, timeout);
                        SetMonitorText("Complete.");
                        break;
                    case ServiceControllerStatus.StartPending:
                        SetMonitorText("Service currently pending start, restarting when complete: " + services[m].DisplayName);
                        services[m].WaitForStatus(ServiceControllerStatus.Running, timeout);
                        SetMonitorText("Stopping service: " + services[m].DisplayName);
                        services[m].Stop();
                        services[m].WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                        SetMonitorText("Starting service: " + services[m].DisplayName);
                        services[m].Start();
                        services[m].WaitForStatus(ServiceControllerStatus.Running, timeout);
                        SetMonitorText("Complete.");
                        break;
                    default:
                        SetMonitorText("Unable to determine service state: " + services[m].DisplayName);
                        break;
                }

                Thread.Sleep(1000);
                SetMonitorText("");
            }
            catch(Exception e)
            {
                SetMonitorText("Error in service management trying again in 3 seconds.");
                Thread.Sleep(3000);              

                if(retryCount < 3)
                {
                    ManageService(services, m);
                    retryCount++;
                }
                else
                {
                    SetMonitorText(e.ToString());
                    retryCount = 0;
                }
               
            }
        }

        private bool GetFileText(string textToFind)
        {
            //TODO update regext to simplified version: \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}:\d{3} use [0-6] to specify digit cannot be bigger than 6
            string re1 = "((?:2|1)\\d{3}(?:-|\\/)(?:(?:0[1-9])|(?:1[0-2]))(?:-|\\/)(?:(?:0[1-9])|(?:[1-2][0-9])|(?:3[0-1]))(?:T|\\s)(?:(?:[0-1][0-9])|(?:2[0-3])):(?:[0-5][0-9]):(?:[0-5][0-9]))";
            Regex r = new Regex(re1, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            //DateTime lastLogTime = new DateTime();
            StringBuilder sb = new StringBuilder();
            DateTime dtcheck = new DateTime();
            string[] logdate;

            bool matchFound = false;

            try
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
                                if (line.Contains(textToFind))
                                {
                                    Match m = r.Match(line);
                                    
                                    if (m.Success)
                                    {
                                        sb.Clear();

                                        for (int i = 0; i < 23; i++)
                                        {
                                            sb.Append(line[i]);
                                        }

                                        //if this is the first instance found chuck the ol' dog into last found log datetime
                                        if (lastLogTextMatch.ToString() == "1/1/0001 12:00:00 AM")
                                        {
                                            lastLogTextMatch = parseDateFromString(sb.ToString());
                                            matchFound = true;
                                            //fileText.Clear();
                                            //fileText.Add(line);
                                        }
                                        else //else this isn't the first found instance so replace our array with this most recent instance and update last found log time
                                        {
                                            dtcheck = parseDateFromString(sb.ToString());

                                            if (dtcheck > lastLogTextMatch)
                                            {
                                                lastLogTextMatch = dtcheck;
                                                matchFound = true;
                                                //fileText.Clear();
                                                //fileText.Add(line);
                                            }
                                        }                                                                              
                                    }                                    
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                SetMonitorText("Failure reading file: " + e.ToString());
            }

            if (matchFound) return true; else return false;            
        }

        private DateTime parseDateFromString(string s)
        {
            DateTime date = new DateTime();

            string[] formats = new[] { "yyyy-MM-dd HH:mm:ss,fff" };
            DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture,
                                DateTimeStyles.None, out date);
            return date;
        }

        private void BtnStopMonitor_Click(object sender, RoutedEventArgs e)
        {
            run = false;            
            bgw.CancelAsync();
            if (bgw.IsBusy)
            {
                lblPending.Content = "Stop Pending...";
            }              
        }         

        private void bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lblPending.Content = "";
            btnStartMonitor.IsEnabled = true;
        }

        private void BtnClearFiles_Click(object sender, RoutedEventArgs e)
        {
            listBoxFiles.Items.Clear();
        }
    }            
}       
