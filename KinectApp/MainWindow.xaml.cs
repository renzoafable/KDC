using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Threading;

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
        #region properties
        /// <summary> Delegate to use for placing a job with no arguments onto the Dispatcher </summary>
        private delegate void NoArgDelegate();

        /// <summary>
        /// Delegate to use for placing a job with a single string argument onto the Dispatcher
        /// </summary>
        /// <param name="arg">string argument</param>
        private delegate void OneArgDelegate(string arg);

        // declare kinect sensor
        private KinectSensor sensor = null;

        // declare frame reader
        private MultiSourceFrameReader reader = null;

        // declare Kinect Studio client
        private KStudioClient client = KStudio.CreateClient();

        // declare playback object
        private KStudioPlayback playback = null;

        // declare recording object
        private KStudioRecording recording = null;

        /// <summary>
        /// declare list of bodies per frame
        /// </summary>
        private IList<Body> bodies;

        /// <summary>
        /// declare list of bodies to be saved during recording
        /// </summary>
        private List<Skeleton> trackedBodies = new List<Skeleton>();

        /// <summary>
        /// declare list of deserialized bodies to be compared
        /// </summary>
        private List<Skeleton> deserializedBodies = null;

        /// <summary>
        /// declare dictionary of joint comparisons
        /// </summary>
        private Dictionary<string, int> jointComparisons = null;

        /// <summary>
        /// default view for skeleton
        /// </summary>
        private bool displayBody = false;

        /// <summary>
        /// indicates if playback is currently in progress
        /// </summary>
        private bool isPlaying = false;

        /// <summary> Indicates if a recording is currently in progress </summary>
        private bool isRecording = false;

        /// <summary>
        /// Indicates if a comparison is currently in progress
        /// </summary>
        private bool isComparing = false;

        /// <summary>
        /// last file opened
        /// </summary>
        private string lastFile = string.Empty;

        /// <summary>
        /// number of playback iterations
        /// </summary>
        private uint loopCount = 0;

        /// <summary> Recording duration of 5 seconds maximum </summary>
        private TimeSpan duration = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Countdown timer
        /// </summary>
        private DispatcherTimer timer;

        /// <summary>
        /// Countdown time
        /// </summary>
        private TimeSpan time;

        /// <summary>
        /// Number of frames to compared with the live motion
        /// </summary>
        private int frameToCount = 0;

        /// <summary> Counter for the frames to be compared with the live motion </summary>
        private int frameCounter = 0;

        /// <summary>
        /// current kinect sensor status text to display
        /// </summary>
        private string kinectStatusText = string.Empty;

        /// <summary>
        /// current status text to display
        /// </summary>
        private string statusText = string.Empty;
        #endregion

        #region constructor
        public MainWindow()
        {
            InitializeComponent();

            // initialize kinect sensor
            this.sensor = KinectSensor.GetDefault();

            // set kinect availability event notifier
            this.sensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // check if sensor exists
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

            this.DataContext = this;
        }
        #endregion

        #region interface implementations
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

            if (this.playback != null)
            {
                this.playback.Dispose();
                this.playback = null;
            }

            if (this.client != null)
            {
                this.client.Dispose();
                this.playback = null;
            }
        }
        #endregion

        #region properties
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
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    this.OnPropertyChanged("StatusText");
                }
            }
        }

        /// <summary>
        /// Gets or sets the last file opened
        /// </summary>
        public string LastFile
        {
            get
            {
                return this.lastFile;
            }

            set
            {
                if (this.lastFile != value)
                {
                    this.lastFile = value;

                    // notify any bound elements that the text has changed
                    this.OnPropertyChanged("LastFile");
                }
            }
        }
        #endregion

        #region methods

        #region generic methods

        /// <summary>
        /// calls the PropertyChanged invoke method to notify window controls on changed data
        /// </summary>
        /// <param name="property">name of property that has changed</param>
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        /// <summary>
        /// Enables/Disables the record and playback buttons in the UI
        /// </summary>
        private void UpdateState()
        {
            if (this.isPlaying == true)
            {
                this.Playback.IsEnabled = true;
                this.Record.IsEnabled = false;
                this.ComparisonFile.IsEnabled = false;
                this.StartComparison.IsEnabled = false;
                this.Playback.Content = "Stop playback";
            }
            else if (this.isRecording)
            {
                this.Record.IsEnabled = true;
                this.Playback.IsEnabled = false;
                this.ComparisonFile.IsEnabled = false;
                this.StartComparison.IsEnabled = false;
                this.Record.Content = "Stop recording";
            }
            else if (this.isComparing)
            {
                this.Playback.IsEnabled = false;
                this.Record.IsEnabled = false;
                this.ComparisonFile.IsEnabled = false;
                this.StartComparison.IsEnabled = false;
            }
            else
            {
                if (!this.isPlaying)
                {
                    this.Playback.Content = "Play movement";
                }
                if (!this.isRecording)
                {
                    this.Record.Content = "Record movement";
                }
                this.StatusText = string.Empty;
                this.LastFile = string.Empty;
                this.Playback.IsEnabled = true;
                this.Record.IsEnabled = true;
                this.ComparisonFile.IsEnabled = true;
                this.StartComparison.IsEnabled = true;
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

            if (this.playback != null)
            {
                this.playback.Dispose();
            }

            if (this.client != null)
            {
                this.client.Dispose();
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
                    // display the color frame
                    this.Camera.Source = frame.ToBitMap();
                }
            }

            // acquire the body data from the frame
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    // clear the canvas where the skeleton will be drawn
                    this.Canvas.Children.Clear();

                    // initialize tracked body from the sensor
                    // only one body is tracked
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
                                    this.Canvas.DrawSkeleton(body);
                                }
                                if (this.isRecording)
                                {
                                    Skeleton skeleton = new Skeleton(body.IsTracked, body.Joints.Count, body.Joints, body.TrackingId);
                                    this.trackedBodies.Add(skeleton);
                                }
                                if (this.isComparing)
                                {
                                    Skeleton skeleton = new Skeleton(body.IsTracked, body.Joints.Count, body.Joints, body.TrackingId);
                                    Dictionary<string, int> segmentAngleComparison = Skeleton.CompareSkeletons(this.deserializedBodies[this.frameCounter], skeleton);

                                    foreach (var item in segmentAngleComparison)
                                    {
                                        if (!this.jointComparisons.ContainsKey(item.Key))
                                        {
                                            this.jointComparisons.Add(item.Key, item.Value);
                                        }
                                        else
                                        {
                                            this.jointComparisons[item.Key] += item.Value;
                                        }
                                    }

                                    if (this.frameCounter == this.frameToCount - 1)
                                    {
                                        Console.WriteLine("Total number of skeletons: " + this.deserializedBodies.Count);
                                        foreach (var item in this.jointComparisons)
                                        {
                                            double percentageAccuracy = Math.Round(((double)item.Value / this.deserializedBodies.Count) * 100);
                                            Console.WriteLine(item.Key + " matches: " + item.Value + ", \tPercentage Match: " + percentageAccuracy);
                                        }
                                        this.isComparing = false;
                                        this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
                                    }
                                    this.frameCounter += 1;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Toggles skeleton view of the kinect sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Body_Click(object sender, RoutedEventArgs e)
        {
            this.displayBody = !displayBody;
            if (this.displayBody)
            {
                this.DisplayBody.Content = "Hide skeleton";
            }
            else
            {
                this.DisplayBody.Content = "Show skeleton";
            }
        }
        #endregion

        #region playback methods
        /// <summary>
        /// Handles the user clicking on the Play button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void PlayBackFile_Click(object sender, RoutedEventArgs e)
        {
            if (!this.isPlaying)
            {
                this.PlayBackFile();
            }
            else
            {
                this.StopPlayback();
            }
        }

        /// <summary>
        /// Invokes the playback file
        /// </summary>
        private void PlayBackFile()
        {
            string filePath = this.OpenFileForPlayback();

            if (!string.IsNullOrEmpty(filePath))
            {
                this.isPlaying = true;
                this.LastFile = filePath;
                this.StatusText = Properties.Resources.PlaybackInProgressText;
                this.UpdateState();

                // Start running the playback asynchronously
                OneArgDelegate playback = new OneArgDelegate(this.PlaybackClip);
                playback.BeginInvoke(filePath, null, null);
            }
        }

        /// <summary>
        /// Stops the playback upon button click
        /// </summary>
        private void StopPlayback()
        {
            if (this.playback != null)
            {
                if (this.playback.State == KStudioPlaybackState.Playing)
                {
                    this.playback.Stop();

                    // Update the UI after the background playback task has completed
                    this.isPlaying = false;
                    this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
                }
            }
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
            try
            {
                this.client = KStudio.CreateClient();

                this.client.ConnectToService();

                // Create the playback object
                this.playback = client.CreatePlayback(filePath);

                this.playback.LoopCount = this.loopCount;
                this.playback.Start();

                while (this.playback.State == KStudioPlaybackState.Playing)
                {
                    Thread.Sleep(500);
                }

                this.client.DisconnectFromService();


                // Update the UI after the background playback task has completed
                this.isPlaying = false;
                this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
            }
            finally
            {
                if (this.playback != null)
                {
                    this.playback.Dispose();
                }
            }
        }
        #endregion

        #region record methods
        /// <summary>
        /// Handles the user clicking on the Record button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void FileRecord_Click(object sender, RoutedEventArgs e)
        {
            if (!this.isRecording)
            {
                this.FileRecord();
            }
            else
            {
                this.StopRecording();
            }
        }

        /// <summary>
        /// Invokes the recording of movement
        /// </summary>
        private void FileRecord()
        {
            string filePath = this.SaveRecordingAs();

            if (!string.IsNullOrEmpty(filePath))
            {
                // temporarily disable all buttons
                this.Playback.IsEnabled = false;
                this.Record.IsEnabled = false;
                this.ComparisonFile.IsEnabled = false;
                this.StartComparison.IsEnabled = false;

                this.StartTimer(5, () =>
                {
                    this.isRecording = true;
                    this.UpdateState();
                    this.LastFile = filePath;
                    this.StatusText = Properties.Resources.RecordingInProgressText;

                    // clear tracked bodies from previous recording
                    if (this.trackedBodies.Count > 0)
                    {
                        this.trackedBodies.Clear();
                    }

                    // Start running the recording asynchronously
                    OneArgDelegate recording = new OneArgDelegate(this.RecordClip);
                    recording.BeginInvoke(filePath, null, null);
                });
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
        /// Runs countdown timer before starting a recording
        /// </summary>
        /// <param name="delay">Seconds to countdown</param>
        /// <param name="action">Callback function after countdown is finished</param>
        private void StartTimer(int delay, Action action)
        {
            this.time = TimeSpan.FromSeconds(delay);
            this.timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                Console.WriteLine(this.time.Seconds);
                if (this.time > TimeSpan.Zero) this.WriteTime(this.time.Seconds.ToString());
                else if (this.time == TimeSpan.Zero) this.WriteTime("Start");
                else if (this.time < TimeSpan.Zero)
                {
                    action();
                    this.timer.Stop();
                }
                this.time = this.time.Add(TimeSpan.FromSeconds(-1));
            }, Application.Current.Dispatcher);

            this.timer.Start();
        }

        /// <summary>
        /// Writes time left on timer to the canvas
        /// </summary>
        /// <param name="text">time to display in string format</param>
        private void WriteTime(string text)
        {
            TextBox textBox = new TextBox
            {
                Text = text,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 50,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Foreground = new SolidColorBrush(Colors.White)
            };
            Canvas.SetLeft(textBox, 10);
            Canvas.SetTop(textBox, 10);

            this.Canvas.Children.Add(textBox);
        }

        /// <summary>
        /// Records a new .xef file and saves body data to a .txt file
        /// </summary>
        /// <param name="filePath">full path to where the file should be saved to</param>
        private void RecordClip(string filePath)
        {
            try
            {
                this.client = KStudio.CreateClient();

                this.client.ConnectToService();

                // Specify which streams should be recorded
                KStudioEventStreamSelectorCollection streamCollection = new KStudioEventStreamSelectorCollection
                {
                    KStudioEventStreamDataTypeIds.Ir,
                    KStudioEventStreamDataTypeIds.Depth,
                    KStudioEventStreamDataTypeIds.Body,
                    KStudioEventStreamDataTypeIds.BodyIndex,
                    KStudioEventStreamDataTypeIds.UncompressedColor
                };

                // Create the recording object
                this.recording = client.CreateRecording(filePath, streamCollection);
                this.recording.StartTimed(this.duration);

                while (recording.State == KStudioRecordingState.Recording)
                {
                    Thread.Sleep(500);
                }

                this.client.DisconnectFromService();


                // Save trackedBodies after the background recording task has completed
                if (this.trackedBodies.Count > 0)
                {
                    this.SaveBodiesToFile(this.trackedBodies);
                }

                // Update UI after the background recording task has completed
                this.isRecording = false;
                this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
            }
            finally
            {
                if (this.recording != null)
                {
                    this.recording.Dispose();
                }
            }
        }

        /// <summary>
        /// Stops the recording upon button click
        /// </summary>
        private void StopRecording()
        {
            if (this.recording != null)
            {
                if (this.recording.State == KStudioRecordingState.Recording)
                {
                    this.recording.Stop();

                    // Update the UI after the background playback task has completed
                    this.isRecording = false;
                    this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
                }
            }
        }

        /// <summary>
        /// Saves Body data to a new .txt file
        /// </summary>
        /// <param name="bodies">List of Bodies to be saved to a .txt file</param>
        public void SaveBodiesToFile(List<Skeleton> bodies)
        {
            string serializedBodyData = JsonConvert.SerializeObject(this.trackedBodies.ToArray());
            string fileNameOfRecording = this.LastFile;
            fileNameOfRecording = fileNameOfRecording.Split('\\')[this.LastFile.Split('\\').Length - 1];
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
        #endregion

        #region compare methods
        /// <summary>
        /// Selects .xef file to be compared and gets the serialized body data of the movement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComparisonFile_Click(object sender, RoutedEventArgs e)
        {
            string filePath = OpenFileForPlayback();
            string fileName = filePath.Split('\\')[filePath.Split('\\').Length - 1];
            string newFileName = fileName.Replace("xef", "txt");

            if (!string.IsNullOrEmpty(fileName))
            {
                // display file name of movement
                this.ComparisonFile.Content = fileName;

                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory + "body\\";
                this.deserializedBodies = JsonConvert.DeserializeObject<List<Skeleton>>(File.ReadAllText(baseDirectory + newFileName));
                this.frameToCount = this.deserializedBodies.Count;
            }
        }

        /// <summary>
        /// Update app that comparison of the live frames with the recorded frames is currently in progress
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartComparison_Click(object sender, RoutedEventArgs e)
        {
            if (this.deserializedBodies != null)
            {
                // temporarily disable all buttons
                this.Playback.IsEnabled = false;
                this.Record.IsEnabled = false;
                this.ComparisonFile.IsEnabled = false;
                this.StartComparison.IsEnabled = false;

                this.StartTimer(5, () =>
                {
                    this.jointComparisons = new Dictionary<string, int>();
                    this.isComparing = true;
                    this.frameCounter = 0;
                    this.StatusText = Properties.Resources.ComparisonInProgressText;
                    this.UpdateState();
                });
            }
        }
        #endregion

        #endregion
    }
}
