using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        /// Maximum deviation  of angles to be considered for matching
        /// </summary>
        private static int angleDeviation = 30;

        /// <summary>
        /// Minimum acceptable percentage for each joint to be considered a match
        /// </summary>
        private static int angleError = 60;

        /// <summary>
        /// Delegate to use for placing a job with a single string argument onto the Dispatcher
        /// </summary>
        /// <param name="arg">string argument</param>
        private delegate void OneArgDelegate(string arg);

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
        private List<Skeleton> trackedBodies = new List<Skeleton>();

        /// <summary>
        /// declare list of deserialized bodies to be compared
        /// </summary>
        private List<Skeleton> deserializedBodies = null;

        /// <summary>
        /// Collection of joint comparisons
        /// </summary>
        private ObservableCollection<AngleStatistics> jointComparisons = null;

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

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        // implement Dispose method from IDisposable interface
        public void Dispose()
        {
            if (this.reader != null)
            {
                this.reader.Dispose();
                this.reader = null;
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

        public int FrameToCount
        {
            get
            {
                return this.frameToCount;
            }

            set
            {
                if (this.frameToCount != value)
                {
                    this.frameToCount = value;

                    this.OnPropertyChanged("FrameToCount");
                }
            }
        }

        public static int AngleDeviation
        {
            get
            {
                return angleDeviation;
            }

            set
            {
                if (angleDeviation != value)
                {
                    angleDeviation = value;

                    OnStaticPropertyChanged("AngleDeviation");
                }
            }
        }

        public static int AngleError
        {
            get
            {
                return angleError;
            }

            set
            {
                if (angleError != value)
                {
                    angleError = value;

                    OnStaticPropertyChanged("AngleError");
                }
            }
        }

        public int Duration
        {
            get
            {
                return this.duration.Seconds;
            }

            set
            {
                if (this.duration.Seconds != value)
                {
                    this.duration = TimeSpan.FromSeconds(value);

                    OnPropertyChanged("Duration");
                }
            }
        }

        public uint LoopCount
        {
            get
            {
                return this.loopCount;
            }

            set
            {
                if (this.loopCount != value)
                {
                    this.loopCount = value;
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
        /// calls the StaticPropertyChanged to notify window controls
        /// </summary>
        /// <param name="property">static property that changed</param>
        private static void OnStaticPropertyChanged(string property)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(property));
        }

        /// <summary>
        /// Enables/Disables the record and playback buttons in the UI
        /// </summary>
        private void UpdateState()
        {
            if (this.isPlaying || this.isRecording || this.isComparing || !this.sensor.IsAvailable)
            {
                this.Playback.IsEnabled = false;
                this.Record.IsEnabled = false;
                this.ComparisonFile.IsEnabled = false;
                this.StartComparison.IsEnabled = false;
            }
            else
            {
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
                                    List<Tuple<string, bool>> segmentAngleComparison = Skeleton.CompareSkeletons(this.deserializedBodies[this.frameCounter], skeleton, angleDeviation);

                                    foreach (var angleComparison in segmentAngleComparison)
                                    {
                                        var angle = this.jointComparisons.FirstOrDefault(i => i.AngleName == angleComparison.Item1);
                                        if (angle == null)
                                        {
                                            AngleStatistics angleStatistics = new AngleStatistics(angleComparison.Item1, 0, 0, new SolidColorBrush(Colors.Red));
                                            this.jointComparisons.Add(angleStatistics);
                                        }
                                        else
                                        {
                                            if (angleComparison.Item2)
                                            {
                                                int angleIndex = this.jointComparisons.IndexOf(angle);
                                                this.jointComparisons[angleIndex].CorrectMatches = this.jointComparisons[angleIndex].CorrectMatches + 1;
                                                this.jointComparisons[angleIndex].AccuracyInPercentage = Math.Round(((double)this.jointComparisons[angleIndex].CorrectMatches / this.frameToCount) * 100, 2);
                                                if (this.jointComparisons[angleIndex].AccuracyInPercentage >= (double)angleError)
                                                {
                                                    this.jointComparisons[angleIndex].Color = new SolidColorBrush(Colors.Green);
                                                }
                                            }
                                        }
                                    }

                                    if (this.frameCounter == this.frameToCount - 1)
                                    {
                                        Console.WriteLine("Total number of skeletons: " + this.deserializedBodies.Count);
                                        foreach (var item in this.jointComparisons)
                                        {
                                            Console.WriteLine(item.AngleName + " matches: " + item.CorrectMatches + ", \tPercentage Match: " + item.AccuracyInPercentage);
                                        }
                                        this.DisplayEvaluation(this.jointComparisons, angleError);
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

        /// <summary>
        /// Filters characters allowed in a textbox
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[\d]{1,4}([.,][\d]{1,2})?");
            e.Handled = !Regex.IsMatch(e.Text, @"[\d]{1,4}([.,][\d]{1,2})?");
        }

        /// <summary>
        /// Changes the settings if input are correct
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Changes_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(angleDeviationTextbox.Text)) // if angle deviation textbox is empty
            {
                MessageBox.Show("Value of angle deviation cannot be empty!");
                return;
            }
            if (string.IsNullOrEmpty(angleErrorTextbox.Text)) // if angle error textbox is empty
            {
                MessageBox.Show("Value of angle error cannot be empty!");
                return;
            }
            else if (Int32.Parse(angleErrorTextbox.Text) > 100) // if value of angle error greater than 100
            {
                MessageBox.Show("Value of angle error cannot exceed 100%!");
                return;
            }
            else if (Int32.Parse(angleErrorTextbox.Text) < 0) // if value of angle error is less than 0
            {
                MessageBox.Show("Value of angle error cannot go lower than 0%!");
                return;
            }
            if (string.IsNullOrEmpty(durationTextbox.Text))
            {
                MessageBox.Show("Value of recording duration cannot be empty!");
                return;
            }
            else if (Int32.Parse(durationTextbox.Text) > 20)
            {
                MessageBox.Show("Recording duration cannot exceed 20 seconds!");
                return;
            }
            if (string.IsNullOrEmpty(loopCountTextbox.Text))
            {
                MessageBox.Show("Value of loop count cannot be empty!");
                return;
            }
            else if (UInt32.Parse(loopCountTextbox.Text) > 10)
            {
                MessageBox.Show("Value of loop count cannot exceed 10!");
            }

            AngleDeviation = Int32.Parse(this.angleDeviationTextbox.Text);
            AngleError = Int32.Parse(this.angleErrorTextbox.Text);
            this.Duration = Int32.Parse(durationTextbox.Text);
            this.LoopCount = UInt32.Parse(loopCountTextbox.Text);
            this.SavedChanges.Visibility = System.Windows.Visibility.Visible;
            this.StartTimer(3, false, () =>
            {
                this.SavedChanges.Visibility = System.Windows.Visibility.Hidden;
            });
        }

        /// <summary>
        /// Display evaluation results for a period of time
        /// </summary>
        /// <param name="jointComparisons">collection of joint comparisons</param>
        /// <param name="acceptablePercentage">acceptable accuracy percentage for each joint comparison</param>
        private void DisplayEvaluation(ObservableCollection<AngleStatistics> jointComparisons, int acceptablePercentage)
        {
            bool isMatch = true;

            // iterates through the collection to check each accuracy percentage
            foreach (AngleStatistics angle in jointComparisons)
            {
                // if an accuracy percentage is lower than the acceptable percentage, then the movement was not matched
                if (angle.AccuracyInPercentage < acceptablePercentage)
                {
                    isMatch = false;
                    break;
                }
            }

            if (isMatch)
            {
                this.Evaluation.Text = "SUCCESS";
                this.Evaluation.Background = new SolidColorBrush(Colors.Green);
                this.Evaluation.Foreground = new SolidColorBrush(Colors.White);
                this.Evaluation.Visibility = System.Windows.Visibility.Visible;

                this.StartTimer(3, false, () =>
                {
                    this.Evaluation.Visibility = System.Windows.Visibility.Hidden;
                });
            }
            else
            {
                this.Evaluation.Text = "FAIL";
                this.Evaluation.Background = new SolidColorBrush(Colors.Red);
                this.Evaluation.Foreground = new SolidColorBrush(Colors.White);
                this.Evaluation.Visibility = System.Windows.Visibility.Visible;

                this.StartTimer(3, false, () =>
                {
                    this.Evaluation.Visibility = System.Windows.Visibility.Hidden;
                });
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

                // Update the UI after the background playback task has completed
                this.isPlaying = false;
                this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
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
            string filePath = this.SaveRecordingAs();

            if (!string.IsNullOrEmpty(filePath))
            {
                // temporarily disable all buttons
                this.Playback.IsEnabled = false;
                this.Record.IsEnabled = false;
                this.ComparisonFile.IsEnabled = false;
                this.StartComparison.IsEnabled = false;

                this.StartTimer(5, true, () =>
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
        private void StartTimer(int delay, bool writeTime,Action action)
        {
            this.time = TimeSpan.FromSeconds(delay);
            this.timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                Console.WriteLine(this.time.Seconds);
                if (this.time > TimeSpan.Zero)
                {
                    if (writeTime) this.WriteTime(this.time.Seconds.ToString());
                }
                else if (this.time == TimeSpan.Zero)
                {
                    if (writeTime) this.WriteTime("Start!");
                }
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
            using (KStudioClient client = KStudio.CreateClient())
            {
                client.ConnectToService();

                KStudioEventStreamSelectorCollection streamCollection = new KStudioEventStreamSelectorCollection
                {
                    KStudioEventStreamDataTypeIds.Ir,
                    KStudioEventStreamDataTypeIds.Depth,
                    KStudioEventStreamDataTypeIds.Body,
                    KStudioEventStreamDataTypeIds.BodyIndex,
                    KStudioEventStreamDataTypeIds.UncompressedColor
                };

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

            if (this.trackedBodies.Count > 0)
            {
                this.SaveBodiesToFile(this.trackedBodies);
            }

            // Update UI after the background recording task has completed
            this.isRecording = false;
            this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
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
                this.FrameToCount = this.deserializedBodies.Count;
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

                this.StartTimer(5, false, () =>
                {
                    this.jointComparisons = new ObservableCollection<AngleStatistics>();
                    this.JointComparisons.ItemsSource = this.jointComparisons;
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
