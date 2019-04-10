using System;
using System.ComponentModel;
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

using Microsoft.Win32;
using Microsoft.Kinect;
using Microsoft.Kinect.Tools;
using System.Threading;

namespace KinectApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IDisposable
    {
        /// <summary> Delegate to use for placing a job with no arguments onto the Dispatcher </summary>
        private delegate void NoArgDelegate();

        /// <summary>
        /// Delegate to use for placing a job with a single string argument onto the Dispatcher
        /// </summary>
        /// <param name="arg">string argument</param>
        private delegate void OneArgDelegate(string arg);

        // initialize default view mode
        private Mode mode = Mode.Color;

        // declare kinect sensor
        private KinectSensor sensor = null;

        // declare frame reader
        private MultiSourceFrameReader reader = null;

        // declare list of bodies per frame
        private IList<Body> bodies;

        // default view for skeleton
        private bool displayBody = true;

        // indicates if playback is currently in progress
        private bool isPlaying = false;

        // last file opened
        private string lastFile = string.Empty;

        // number of playback iterations
        private uint loopCount = 0;

        // current kinect sensor status text to display
        private string kinectStatusText = string.Empty;

        // current playback status text to display
        private string playbackStatusText = string.Empty;

        // <summary>
        /// Color visualizer
        /// </summary>
        private KinectColorViewer kinectColorView = null;

        public MainWindow()
        {
            InitializeComponent();

            // initialize kinect sensor
            this.sensor = KinectSensor.GetDefault();

            // set kinect availability event notifier
            this.sensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            if (this.sensor != null)
            {
                this.sensor.Open();

                this.kinectStatusText = this.sensor.IsAvailable ? Properties.Resources.RunningStatusText : Properties.Resources.NoSensorStatusText;

                this.reader = this.sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color |
                                                           FrameSourceTypes.Depth |
                                                           FrameSourceTypes.Infrared |
                                                           FrameSourceTypes.Body);
                this.reader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;
            }

            this.kinectColorView = new KinectColorViewer(this.sensor);
            
            this.DataContext = this;
            this.playback.DataContext = this.kinectColorView;
        }

        // INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        // gets or sets current kinect status text to display
        public string KinectStatusText
        {
            get
            {
                return this.kinectStatusText;
            }

            set
            {
                this.kinectStatusText = value;

                // notify any bound elements that the text has changed
                this.OnPropertyChanged("KinectStatusText");
            }
        }


        public string PlaybackStatusText
        {
            get
            {
                return this.playbackStatusText;
            }
            
            set
            {
                if (this.playbackStatusText != value)
                {
                    this.playbackStatusText = value;

                    // notify any bound elements that the text has changed
                    this.OnPropertyChanged("PlaybackStatusText");
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (this.reader != null)
            {
                this.reader.Dispose();
            }

            if (this.sensor != null)
            {
                this.sensor.Close();
            }
        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // set the kinect status
            this.KinectStatusText = this.sensor.IsAvailable ? Properties.Resources.RunningStatusText : Properties.Resources.SensorNotAvailableStatusText;
        }

        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (this.mode == Mode.Color)
                    {
                        this.camera.Source = frame.ToBitMap();
                        this.r_camera.Source = frame.ToBitMap();
                    }
                }
            }

            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    this.canvas.Children.Clear();
                    this.r_canvas.Children.Clear();

                    this.bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(bodies);

                    foreach (var body in bodies)
                    {
                        if (body != null)
                        {
                            if (body.IsTracked)
                            {
                                if (displayBody)
                                {
                                    this.canvas.DrawSkeleton(body);
                                    this.r_canvas.DrawSkeleton(body);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void Body_Click(object sender, RoutedEventArgs e)
        {
            this.displayBody = !displayBody;
        }

        public void Dispose()
        {
            if (this.reader != null)
            {
                this.reader.Dispose();
                this.reader = null;
            }
        }

        private void PlayBackFile_Click(object sender, RoutedEventArgs e)
        {
            string filePath = this.OpenFileForPlayback();

            if (!string.IsNullOrEmpty(filePath))
            {
                this.lastFile = filePath;
                this.isPlaying = true;
                this.PlaybackStatusText = Properties.Resources.PlaybackInProgressText;
                this.UpdateState();

                // Start running the playback asynchronously
                OneArgDelegate playback = new OneArgDelegate(this.PlaybackClip);
                playback.BeginInvoke(filePath, null, null);
            }
            
            this.PlayBackFile.Content = this.lastFile;
        }

        /// <summary>
        /// Launches the OpenFileDialog window to help user find/select an event file for playback
        /// </summary>
        /// <returns>Path to the event file selected by the user</returns>
        private string OpenFileForPlayback()
        {
            string fileName = string.Empty;

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.FileName = this.lastFile;
            dlg.DefaultExt = Properties.Resources.XefExtension; // Default file extension
            dlg.Filter = Properties.Resources.EventFileDescription + " " + Properties.Resources.EventFileFilter; // Filter files by extension 
            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                fileName = dlg.FileName;
            }

            return fileName;
        }

        /// <summary>
        /// Plays back a .xef file to the Kinect sensor
        /// </summary>
        /// <param name="filePath">Full path to the .xef file that should be played back to the sensor</param>
        private void PlaybackClip(string filePath)
        {
            using (KStudioClient client = KStudio.CreateClient())
            {
                client.ConnectToService();

                // Create the playback object
                using (KStudioPlayback playback = client.CreatePlayback(filePath))
                {
                    playback.LoopCount = this.loopCount;
                    playback.Start();

                    while (playback.State == KStudioPlaybackState.Playing)
                    {
                        Thread.Sleep(500);
                    }
                }

                client.DisconnectFromService();
            }

            // Update the UI after the background playback task has completed
            this.isPlaying = false;
            this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
        }

        /// <summary>
        /// Enables/Disables the record and playback buttons in the UI
        /// </summary>
        private void UpdateState()
        {
            if (this.isPlaying)
            {
                this.PlayBackFile.IsEnabled = false;
                this.PausePlayback.IsEnabled = true;
            }
            else
            {
                this.PlaybackStatusText = string.Empty;
                this.PlayBackFile.IsEnabled = true;
                this.PausePlayback.IsEnabled = false;
            }
        }
    }

    public enum Mode
    {
        Color,
        Depth,
        Infrared
    }
}
