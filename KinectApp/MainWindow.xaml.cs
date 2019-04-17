using System;
using System.IO;
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
using Newtonsoft.Json;

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

        /// <summary>
        /// declare list of bodies per frame
        /// </summary>
        private IList<Body> bodies;

        /// <summary>
        /// declare list of bodies to be saved during recording
        /// </summary>
        private List<Body> trackedBodies = new List<Body>();

        /// <summary>
        /// default view for skeleton
        /// </summary>
        private bool displayBody = true;

        /// <summary>
        /// indicates if playback is currently in progress
        /// </summary>
        private bool isPlaying = false;

        /// <summary> Indicates if a recording is currently in progress </summary>
        private bool isRecording = false;

        /// <summary>
        /// last file opened
        /// </summary>
        private string lastFile = string.Empty;

        /// <summary>
        /// number of playback iterations
        /// </summary>
        private uint loopCount = 0;

        /// <summary> Recording duration of 5 seconds maximum </summary>
        private TimeSpan duration = TimeSpan.FromSeconds(20);

        /// <summary> Counter for the frames to be compared with the live motion </summary>
        private int frameCounter = 0;

        /// <summary>
        /// current kinect sensor status text to display
        /// </summary>
        private string kinectStatusText = string.Empty;

        // current playback status text to display
        private string playStatusText = string.Empty;

        /// <summary>
        /// current record status text to display
        /// </summary>
        private string recordStatusText = string.Empty;

        // <summary>
        /// Color visualizer
        /// </summary>
        private KinectColorViewer kinectColorView = null;

        /// <summary>
        /// Body visualizer
        /// </summary>
        private KinectBodyViewer kinectBodyView = null;

        public MainWindow()
        {
            InitializeComponent();
            Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);

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
            this.kinectBodyView = new KinectBodyViewer(this.sensor);

            this.DataContext = this;
            this.playback.DataContext = this.kinectColorView;
            this.record.DataContext = this.kinectBodyView;
        }

        // INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        public event PropertyChangedEventHandler PropertyChanged;

        // implement Dispose method from IDisposable interface
        public void Dispose()
        {
            if (this.reader != null)
            {
                this.reader.Dispose();
                this.reader = null;
            }
        }

        // calls the PropertyChanged invoke method to notify window controls on changed data
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


        /// <summary>
        /// Gets or sets the current status text to display for the playback features
        /// </summary>
        public string PlaybackStatusText
        {
            get
            {
                return this.playStatusText;
            }

            set
            {
                if (this.playStatusText != value)
                {
                    this.playStatusText = value;

                    // notify any bound elements that the text has changed
                    this.OnPropertyChanged("PlaybackStatusText");
                }
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display for the record features
        /// </summary>
        public string RecordStatusText
        {
            get
            {
                return this.recordStatusText;
            }

            set
            {
                if (this.recordStatusText != value)
                {
                    this.recordStatusText = value;

                    // notify any bound elements that the text has changed
                    this.OnPropertyChanged("RecordStatusText");
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
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

        /// <summary>
        /// Handles the event in which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // set the kinect status
            this.KinectStatusText = this.sensor.IsAvailable ? Properties.Resources.RunningStatusText : Properties.Resources.SensorNotAvailableStatusText;
        }


        /// <summary>
        /// Handles the event in which the reader receives a frame.
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            // acquire a reference to the received frame
            var reference = e.FrameReference.AcquireFrame();

            // acquire the color data from the frame
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (this.mode == Mode.Color)
                    {
                        // display the color frame
                        this.camera.Source = frame.ToBitMap();
                        this.recordCamera.Source = frame.ToBitMap();
                        this.playbackCamera.Source = frame.ToBitMap();
                    }
                }
            }

            // acquire the body data from the frame
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    // clear the canvas where the skeleton will be drawn
                    this.canvas.Children.Clear();
                    this.playbackCanvas.Children.Clear();
                    this.recordCanvas.Children.Clear();

                    // initialize the tracked bodies from the sensor
                    this.bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(bodies);

                    foreach (var body in bodies)
                    {
                        if (body != null)
                        {
                            if (body.IsTracked)
                            {
                                if (this.displayBody)
                                {
                                    this.canvas.DrawSkeleton(body);
                                    this.playbackCanvas.DrawSkeleton(body);
                                    this.recordCanvas.DrawSkeleton(body);
                                }
                                if (this.isRecording)
                                {
                                    this.trackedBodies.Add(body);
                                }
                            }
                        }
                    }
                }
            }
        }


        // toggles body view of the kinect sensor
        private void Body_Click(object sender, RoutedEventArgs e)
        {
            this.displayBody = !displayBody;
        }


        /// <summary>
        /// Handles the user clicking on the Play button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
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

            OpenFileDialog dlg = new OpenFileDialog
            {
                FileName = this.lastFile,
                DefaultExt = Properties.Resources.XefExtension, // Default file extension
                Filter = Properties.Resources.EventFileDescription + " " + Properties.Resources.EventFileFilter // Filter files by extension 
            };
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
            if (this.isPlaying || this.isRecording)
            {
                this.PlayBackFile.IsEnabled = false;
                this.PausePlayback.IsEnabled = true;
            }
            else
            {
                this.PlaybackStatusText = string.Empty;
                this.RecordStatusText = string.Empty;
                this.PlayBackFile.IsEnabled = true;
                this.PausePlayback.IsEnabled = false;
            }
        }

        /// <summary>
        /// Handles the user clicking on the Record button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void FileRecord_Click(object sender, RoutedEventArgs e)
        {
            string filePath = this.SaveRecordingAs();

            if (!string.IsNullOrEmpty(filePath))
            {
                this.lastFile = filePath;
                this.isRecording = true;
                this.RecordStatusText = Properties.Resources.RecordingInProgressText;
                this.UpdateState();

                // clear tracked bodies from previous recording
                if (this.trackedBodies.Count > 0)
                {
                    this.trackedBodies.Clear();
                }

                // Start running the recording asynchronously
                OneArgDelegate recording = new OneArgDelegate(this.RecordClip);
                recording.BeginInvoke(filePath, null, null);
            }
        }

        /// <summary>
        /// Launches the SaveFileDialog window to help user create a new recording file
        /// </summary>
        /// <returns>File path to use when recording a new event file</returns>
        private string SaveRecordingAs()
        {
            string fileName = string.Empty;

            SaveFileDialog dlg = new SaveFileDialog
            {
                FileName = "recordAndPlaybackBasics.xef",
                DefaultExt = Properties.Resources.XefExtension,
                AddExtension = true,
                Filter = Properties.Resources.EventFileDescription + " " + Properties.Resources.EventFileFilter,
                CheckPathExists = true
            };
            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                fileName = dlg.FileName;
            }

            return fileName;
        }

        /// <summary>
        /// Records a new .xef file and saves body data to a .txt file
        /// </summary>
        /// <param name="filePath">Full path to where the file should be saved to</param>
        private void RecordClip(string filePath)
        {
            using (KStudioClient client = KStudio.CreateClient())
            {
                client.ConnectToService();

                // Specify which streams should be recorded
                KStudioEventStreamSelectorCollection streamCollection = new KStudioEventStreamSelectorCollection();
                streamCollection.Add(KStudioEventStreamDataTypeIds.Ir);
                streamCollection.Add(KStudioEventStreamDataTypeIds.Depth);
                streamCollection.Add(KStudioEventStreamDataTypeIds.Body);
                streamCollection.Add(KStudioEventStreamDataTypeIds.BodyIndex);
                streamCollection.Add(KStudioEventStreamDataTypeIds.UncompressedColor);

                // Create the recording object
                using (KStudioRecording recording = client.CreateRecording(filePath, streamCollection))
                {
                    recording.StartTimed(this.duration);
                    while (recording.State == KStudioRecordingState.Recording)
                    {
                        Thread.Sleep(500);
                    }
                }

                client.DisconnectFromService();
            }

            // Save trackedBodies after the background recording task has completed
            if (this.trackedBodies.Count > 0)
            {
                SaveBodiesToFile(this.trackedBodies);
            }

            // Update UI after the background recording task has completed
            this.isRecording = false;
            this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
        }

        /// <summary>
        /// Saves Body data to a new .txt file
        /// </summary>
        /// <param name="bodies">List of Bodies to be saved to a .txt file</param>
        public void SaveBodiesToFile(List<Body> bodies)
        {
            string serializedBodyData = JsonConvert.SerializeObject(this.trackedBodies.ToArray());
            string fileNameOfRecording = lastFile.Split('\\')[this.lastFile.Split('\\').Length - 1];
            string newFileName = fileNameOfRecording.Replace("xef", "txt");

            // check if directory exists
            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "body\\"))
            {
                // create directory if it does not exist
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "body\\");
            }

            // save file
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "body\\" + newFileName, serializedBodyData);
        }
    }

    public enum Mode
    {
        Color,
        Depth,
        Infrared
    }
}
