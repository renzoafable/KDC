﻿using System;
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
        private readonly IReadOnlyDictionary<JointType, Joint> joints;
        private readonly Dictionary<string, double> segmentAngles;

        #region constructor
        public Skeleton(bool isTracked, int count, IReadOnlyDictionary<JointType, Joint> joints, ulong trackingId)
        {
            this.isTracked = isTracked;
            this.jointCount = count;
            this.joints = joints;
            this.trackingId = trackingId;
            this.segmentAngles = new Dictionary<string, double>();
            this.GetSegmentAngles();
        }
        #endregion

        #region properties
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
        #endregion

        #region methods

        /// <summary>
        /// Calculate segment angles and store for comparison
        /// </summary>
        public void GetSegmentAngles()
        {
            // get segment angles
            // joint types can be viewed here https://docs.microsoft.com/en-us/previous-versions/windows/kinect/images/dn758662.skeleton_overview_nui_skeleton%28en-us%2cieb.10%29.png

            // head, neck, and spine shoulder
            double headToS_shoulder = Extensions.GetSegmentAngle(this, JointType.Head, JointType.Neck, JointType.SpineShoulder);

            // right hand, right wrist, and right elbow
            double r_handToR_elbow = Extensions.GetSegmentAngle(this, JointType.HandRight, JointType.WristRight, JointType.ElbowRight);
            // right wrist, right elbow, and right shoulder
            double r_wristToR_shoulder = Extensions.GetSegmentAngle(this, JointType.WristRight, JointType.ElbowRight, JointType.ShoulderRight);
            // right elbow, right shoulder, and spine shoulder
            double r_elbowToS_shoulder = Extensions.GetSegmentAngle(this, JointType.ElbowRight, JointType.ShoulderRight, JointType.SpineShoulder);

            // left hand, left wrist, and left elbow
            double l_handToL_elbow = Extensions.GetSegmentAngle(this, JointType.HandLeft, JointType.WristLeft, JointType.ElbowLeft);
            // left wrist, left elbow, and left shoulder
            double l_wristToL_shoulder = Extensions.GetSegmentAngle(this, JointType.WristLeft, JointType.ElbowLeft, JointType.ShoulderLeft);
            // left elbow, left shoulder, and spine shoulder
            double l_elbowToS_shoulder = Extensions.GetSegmentAngle(this, JointType.ElbowLeft, JointType.ShoulderLeft, JointType.SpineShoulder);

            // right shoulder, spine shoulder, and neck
            double r_shoulderToNeck = Extensions.GetSegmentAngle(this, JointType.ShoulderRight, JointType.SpineShoulder, JointType.Neck);
            // left shoulder, spine shoulder, and neck
            double l_shoulderToNeck = Extensions.GetSegmentAngle(this, JointType.ShoulderLeft, JointType.SpineShoulder, JointType.Neck);

            // right shoulder, spine shoulder, and left shoulder
            double r_shoulderToL_shoulder = Extensions.GetSegmentAngle(this, JointType.ShoulderRight, JointType.SpineShoulder, JointType.ShoulderLeft);
            // right shoulder, spine shoulder, and spine mid
            double r_shoulderToS_mid = Extensions.GetSegmentAngle(this, JointType.ShoulderRight, JointType.SpineShoulder, JointType.SpineMid);
            // left shoulder, spine shoulder, and spine mide
            double l_shoulderToS_mid = Extensions.GetSegmentAngle(this, JointType.ShoulderLeft, JointType.SpineShoulder, JointType.SpineMid);

            // spine shoulder, spine mid, and spine base
            double s_shoudlerToS_base = Extensions.GetSegmentAngle(this, JointType.SpineShoulder, JointType.SpineMid, JointType.SpineBase);

            // right hip, spine base, and left hip
            double r_hipToL_hip = Extensions.GetSegmentAngle(this, JointType.HipRight, JointType.SpineBase, JointType.HipLeft);
            // right hip, spine base, and spine mid
            double r_hipToS_mid = Extensions.GetSegmentAngle(this, JointType.HipRight, JointType.SpineBase, JointType.SpineMid);
            // left hip , spine base, and spine mid
            double l_hipToS_mid = Extensions.GetSegmentAngle(this, JointType.HipLeft, JointType.SpineBase, JointType.SpineMid);

            // right knee, right hip, and spine base
            double r_kneeToS_base = Extensions.GetSegmentAngle(this, JointType.KneeRight, JointType.HipRight, JointType.SpineBase);
            // right ankle, right knee, and right hip
            double r_ankleToR_hip = Extensions.GetSegmentAngle(this, JointType.AnkleRight, JointType.KneeRight, JointType.HipRight);
            // right foot, right ankle, right knee
            double r_footToR_knee = Extensions.GetSegmentAngle(this, JointType.FootRight, JointType.AnkleRight, JointType.KneeRight);

            // left knee, left hip, and spine base
            double l_kneeToS_base = Extensions.GetSegmentAngle(this, JointType.KneeLeft, JointType.HipLeft, JointType.SpineBase);
            // left ankle, left knee, and left hip
            double l_ankleToL_hip = Extensions.GetSegmentAngle(this, JointType.AnkleLeft, JointType.KneeLeft, JointType.HipLeft);
            // left foot, left ankle, left knee
            double l_footToL_knee = Extensions.GetSegmentAngle(this, JointType.FootLeft, JointType.AnkleLeft, JointType.KneeLeft);

            // store angles to dictionary
            this.segmentAngles.Add("Head-Shoulder", headToS_shoulder);
            this.segmentAngles.Add("Right hand - Right elbow", r_handToR_elbow);
            this.segmentAngles.Add("Right wrist - Right shoulder", r_wristToR_shoulder);
            this.segmentAngles.Add("Right elbow - Spine", r_elbowToS_shoulder);
            this.segmentAngles.Add("Left hand - Left elbow", l_handToL_elbow);
            this.segmentAngles.Add("Left wrist - Left shoulder", l_wristToL_shoulder);
            this.segmentAngles.Add("Left elbow - Spine", l_elbowToS_shoulder);
            this.segmentAngles.Add("Right shoulder - Neck", r_shoulderToNeck);
            this.segmentAngles.Add("Left shoulder - Neck", l_shoulderToNeck);
            this.segmentAngles.Add("Right shoulder - Left shoulder", r_shoulderToL_shoulder);
            this.segmentAngles.Add("Right shoulder - Middle spine", r_shoulderToS_mid);
            this.segmentAngles.Add("Left shoulder - Middle spine", l_shoulderToS_mid);
            this.segmentAngles.Add("Spine - Spine base", s_shoudlerToS_base);
            this.segmentAngles.Add("Right hip - Left hip", r_hipToL_hip);
            this.segmentAngles.Add("Right hip - Middle spine", r_hipToS_mid);
            this.segmentAngles.Add("Left hip - Middle", l_hipToS_mid);
            this.segmentAngles.Add("Right knee - Spine base", r_kneeToS_base);
            this.segmentAngles.Add("Right ankle - Right hip", r_ankleToR_hip);
            this.segmentAngles.Add("Right foot - Right knee", r_footToR_knee);
            this.segmentAngles.Add("Left knee - Spine base", l_kneeToS_base);
            this.segmentAngles.Add("Left ankle - Left hip", l_ankleToL_hip);
            this.segmentAngles.Add("Left foot - Left knee", l_footToL_knee);
        }

        /// <summary>
        /// Compare segment angles between two skeletons
        /// </summary>
        /// <param name="skeleton0">first skeleton</param>
        /// <param name="skeleton1">second skeleton</param>
        /// <returns>dictionary holding 1 or 0 depending on the segment angle being compared</returns>
        public static Dictionary<string, int> CompareSkeletons(Skeleton skeleton0, Skeleton skeleton1)
        {
            Dictionary<string, int> matches = new Dictionary<string, int>();

            foreach (var item in skeleton0.segmentAngles)
            {
                double difference = item.Value - skeleton1.segmentAngles[item.Key];
                if (Math.Abs(difference) <= 30) // if segment angle differs by at most 20 degrees, it is considered as a match
                {
                    matches.Add(item.Key, 1);
                }
                else
                {
                    matches.Add(item.Key, 0);
                }
            }

            return matches;
        }
        #endregion
    }
}
