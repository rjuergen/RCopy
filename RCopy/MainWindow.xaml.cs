using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace RCopy
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool copying = false;
        private CopyThread copy;        
        private long copied = 0;

        public MainWindow()
        {
            InitializeComponent();

            txtSource.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            txtDestination.Text = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)+"\\Documents_save";
            lbCurrentFolder.TextWrapping = TextWrapping.Wrap;
            pbCopyState.Maximum = 1;
        }

        private void BtnSource_Click(object sender, RoutedEventArgs e)
        {
            UpdatePathTxt(txtSource);
        }

        private void BtnDestination_Click(object sender, RoutedEventArgs e)
        {
            UpdatePathTxt(txtDestination);
        }

        private void UpdatePathTxt(TextBox txt)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.SelectedPath = txt.Text;
            bool? b = dialog.ShowDialog();
            if (b.HasValue && b.Value)
                txt.Text = dialog.SelectedPath;
        }
        
        private void BtnStartCopy_Click(object sender, RoutedEventArgs e)
        {
            copied = 0;
            txtState.Content = "";
            lbCurrentFolder.Text = "";
            copying = !copying;
            
            if (copying)
            {
                btnStartCopy.Content = "Stop";
                txtProgressBar.Visibility = System.Windows.Visibility.Visible;
                txtSource.IsEnabled = btnSelectSource.IsEnabled = txtDestination.IsEnabled = btnSelectDestination.IsEnabled = false;                
                try
                {
                    copy = new CopyThread(txtSource.Text, txtDestination.Text, Finished, UpdateProgressGUI, UpdateCurrentFolderGUI);
                    copy.Start();
                    DirectoryInfo from = new DirectoryInfo(txtSource.Text);                 
                    Task.Factory.StartNew(() =>
                    {                        
                        long size = copy.GetRealSizeSafe(from);
                        double lengthInMB = (double)size / 1024 / 1024;
                        Dispatcher.Invoke(() =>
                        {
                            pbCopyState.Maximum = Math.Max(lengthInMB, 1.2);
                            if(lengthInMB / 1024 > 1)
                                txtState.Content = Math.Round(lengthInMB / 1024, 3).ToString() + " GB to copy ";
                        });
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    StopCopy();
                }
            }
            else
            {
                StopCopy();
                txtState.Content = "Cancelled!";
            }            
        }

        private void Finished()
        {            
            Dispatcher.Invoke(() => {
                StopCopy();
                txtState.Content = "Finished!";
                lbCurrentFolder.Text = "";
                pbCopyState.Value = pbCopyState.Maximum;
                UpdateSizeLabel(lblDestinationSize, txtDestination.Text);
            });
        }

        private void StopCopy()
        {
            copying = false;
            btnStartCopy.Content = "Start copy";
            pbCopyState.Value = 0;
            lbCurrentFolder.Text = "";
            txtProgressBar.Visibility = System.Windows.Visibility.Hidden;
            txtSource.IsEnabled = btnSelectSource.IsEnabled = txtDestination.IsEnabled = btnSelectDestination.IsEnabled = true; 
            if (copy != null)
                copy.Stop();
            copy = null;
        }

        private void txtSource_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (pbCopyState != null)
            {                
                pbCopyState.Maximum = 1;
                pbCopyState.Value = 0;
                txtState.Content = "";
            }
            UpdateSizeLabel(lblSourceSize, txtSource.Text);
        }

        private void txtDestination_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSizeLabel(lblDestinationSize, txtDestination.Text);
        }

        private void UpdateSizeLabel(Label l, string dir)
        {
            if (l == null)
                return;
            l.Content = "";
            if (Directory.Exists(dir))
            {
                Task.Factory.StartNew(() =>
                {
                    long size = IOUtil.GetSizeSafe(new DirectoryInfo(dir));
                    double lengthInMB = (double)size / 1024 / 1024;
                    Dispatcher.Invoke(() =>
                    {
                        l.Content = Math.Round(lengthInMB / 1024, 3).ToString() + " GB";
                    });
                });
            }
        }         

        private void UpdateProgressGUI(long copied)
        {
            Dispatcher.Invoke(() =>
            {                
                this.copied += copied;                
                if (pbCopyState.Maximum > 1.1)
                {
                    double progress = (double)this.copied / 1024 / 1024;
                    pbCopyState.Value = progress;                    
                }
            });
        }

        private void UpdateCurrentFolderGUI(string currentFolder)
        {            
            Dispatcher.Invoke(() =>
            {
                lbCurrentFolder.Text = "Copy from: "+currentFolder;
            });
        }

    }
}
