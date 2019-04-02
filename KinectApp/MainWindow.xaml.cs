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

namespace KinectApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IDisposable
    {
        // initialize default view mode
        private Mode mode = Mode.Color;

        // declare kinect sensor
        private KinectSensor sensor = null;

        // declare frame reader
        private MultiSourceFrameReader reader = null;

        // declare list of bodies per frame
        private IList<Body> bodies;

        // default view for skeleton
        private bool displayBody = false;

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

        public MainWindow()
        {
            InitializeComponent();
        }

        // INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        public event PropertyChangedEventHandler PropertyChanged;

        // gets or sets current kinect status text to display
        // @TODO notify component on property change
        public string KinectStatusText
        {
            get
            {
                return this.kinectStatusText;
            }

            set
            {
                if (this.kinectStatusText != value)
                {
                    this.kinectStatusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("KinectStatusText"));
                    }
                }
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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlaybackStatusText"));
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.sensor = KinectSensor.GetDefault();

            // set kinect availability event notifier
            this.sensor.IsAvailableChanged += Sensor_IsAvailableChanged;

            if (this.sensor != null)
            {
                this.sensor.Open();

                this.kinectStatusText = sensor.IsAvailable ? Properties.Resources.RunningStatusText : Properties.Resources.NoSensorStatusText;

                this.reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color |
                                                           FrameSourceTypes.Depth |
                                                           FrameSourceTypes.Infrared |
                                                           FrameSourceTypes.Body);
                this.reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }

            this.DataContext = this;
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
            this.kinectStatusText = this.sensor.IsAvailable ? Properties.Resources.RunningStatusText : Properties.Resources.SensorNotAvailableStatusText;
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
                        this.playbackCamera.Source = frame.ToBitMap();
                    }
                }
            }

            using (var frame = reference.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (this.mode == Mode.Depth)
                    {
                        this.camera.Source = frame.ToBitmap();
                        this.r_camera.Source = frame.ToBitmap();
                    }
                }
            }

            using (var frame = reference.InfraredFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (this.mode == Mode.Infrared)
                    {
                        this.camera.Source = frame.ToBitmap();
                        this.r_camera.Source = frame.ToBitmap();
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

        

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            this.mode = Mode.Color;
        }

        private void Depth_Click(object sender, RoutedEventArgs e)
        {
            this.mode = Mode.Depth;
        }

        private void Infrared_Click(object sender, RoutedEventArgs e)
        {
            this.mode = Mode.Infrared;
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
    }

    public enum Mode
    {
        Color,
        Depth,
        Infrared
    }
}
