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

using Microsoft.Win32;
using Microsoft.Kinect;
using Microsoft.Kinect.Tools;
using System.Threading;
using Newtonsoft.Json;

namespace KinectApp
{
    public class Skeleton
    {
        private bool isTracked;
        private int jointCount;
        private ulong trackingId;
        private IReadOnlyDictionary<JointType, Joint> joints;


        public Skeleton(bool isTracked, int count, IReadOnlyDictionary<JointType, Joint> joints, ulong trackingId)
        {
            this.isTracked = isTracked;
            this.jointCount = count;
            this.joints = joints;
            this.trackingId = trackingId;
        }

        public bool IsTracked
        {
            get
            {
                return this.isTracked;
            }

            set
            {
                if (this.isTracked != value)
                {
                    this.isTracked = value;

                }
            }
        }
        public int JointCount
        {
            get
            {
                return this.jointCount;
            }

            set
            {
                if (this.jointCount != value)
                {
                    this.jointCount = value;
                }
            }
        }
        public IReadOnlyDictionary<JointType,Joint> Joints
        {
            get
            {
                return this.joints;
            }
        }
        public ulong TrackingId
        {
            get
            {
                return this.trackingId;
            }

            set
            {
                if (this.trackingId != value)
                {
                    this.trackingId = value;
                }
            }
        }
    }
}
