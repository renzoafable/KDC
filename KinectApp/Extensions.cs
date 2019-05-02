using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Microsoft.Kinect;
using System.Numerics;
using System.Collections.Generic;

namespace KinectApp
{
    public static class Extensions
    {
        public static ImageSource ToBitMap(this ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height * ((format.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        public static ImageSource ToBitmap(this DepthFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            ushort minDepth = frame.DepthMinReliableDistance;
            ushort maxDepth = frame.DepthMaxReliableDistance;

            ushort[] pixelData = new ushort[width * height];
            byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(pixelData);

            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < pixelData.Length; ++depthIndex)
            {
                ushort depth = pixelData[depthIndex];

                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                pixels[colorIndex++] = intensity; // Blue
                pixels[colorIndex++] = intensity; // Green
                pixels[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        public static ImageSource ToBitmap(this InfraredFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            ushort[] frameData = new ushort[width * height];
            byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(frameData);

            int colorIndex = 0;
            for (int infraredIndex = 0; infraredIndex < frameData.Length; infraredIndex++)
            {
                ushort ir = frameData[infraredIndex];

                byte intensity = (byte)(ir >> 7);

                pixels[colorIndex++] = (byte)(intensity / 1); // Blue
                pixels[colorIndex++] = (byte)(intensity / 1); // Green   
                pixels[colorIndex++] = (byte)(intensity / 0.4); // Red

                colorIndex++;
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        public static Joint ScaleTo(this Joint joint, double width, double height, float skeletonMaxX, float skeletonMaxY)
        {
            joint.Position = new CameraSpacePoint
            {
                X = Scale(width, skeletonMaxX, joint.Position.X),
                Y = Scale(height, skeletonMaxY, -joint.Position.Y),
                Z = joint.Position.Z
            };

            return joint;
        }

        public static Joint ScaleTo(this Joint joint, double width, double height)
        {
            return ScaleTo(joint, width, height, 2.0f, 1.0f);
        }

        public static float Scale(double maxPixel, double maxSkeleton, float position)
        {
            float value = (float)((((maxPixel / maxSkeleton) / 2) * position) + (maxPixel / 2));

            if (value > maxPixel)
            {
                return (float)maxPixel;
            }

            if (value < 0)
            {
                return 0;
            }

            return value;
        }

        public static void DrawSkeleton(this Canvas canvas, Body body)
        {
            if (body == null) return;

            canvas.DrawLine(body.Joints[JointType.Head], body.Joints[JointType.Neck]);
            canvas.DrawLine(body.Joints[JointType.Neck], body.Joints[JointType.SpineShoulder]);
            canvas.DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderLeft]);
            canvas.DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderRight]);
            canvas.DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.SpineMid]);
            canvas.DrawLine(body.Joints[JointType.ShoulderLeft], body.Joints[JointType.ElbowLeft]);
            canvas.DrawLine(body.Joints[JointType.ShoulderRight], body.Joints[JointType.ElbowRight]);
            canvas.DrawLine(body.Joints[JointType.ElbowLeft], body.Joints[JointType.WristLeft]);
            canvas.DrawLine(body.Joints[JointType.ElbowRight], body.Joints[JointType.WristRight]);
            canvas.DrawLine(body.Joints[JointType.WristLeft], body.Joints[JointType.HandLeft]);
            canvas.DrawLine(body.Joints[JointType.WristRight], body.Joints[JointType.HandRight]);
            canvas.DrawLine(body.Joints[JointType.HandLeft], body.Joints[JointType.HandTipLeft]);
            canvas.DrawLine(body.Joints[JointType.HandRight], body.Joints[JointType.HandTipRight]);
            canvas.DrawLine(body.Joints[JointType.HandTipLeft], body.Joints[JointType.ThumbLeft]);
            canvas.DrawLine(body.Joints[JointType.HandTipRight], body.Joints[JointType.ThumbRight]);
            canvas.DrawLine(body.Joints[JointType.SpineMid], body.Joints[JointType.SpineBase]);
            canvas.DrawLine(body.Joints[JointType.SpineBase], body.Joints[JointType.HipLeft]);
            canvas.DrawLine(body.Joints[JointType.SpineBase], body.Joints[JointType.HipRight]);
            canvas.DrawLine(body.Joints[JointType.HipLeft], body.Joints[JointType.KneeLeft]);
            canvas.DrawLine(body.Joints[JointType.HipRight], body.Joints[JointType.KneeRight]);
            canvas.DrawLine(body.Joints[JointType.KneeLeft], body.Joints[JointType.AnkleLeft]);
            canvas.DrawLine(body.Joints[JointType.KneeRight], body.Joints[JointType.AnkleRight]);
            canvas.DrawLine(body.Joints[JointType.AnkleLeft], body.Joints[JointType.FootLeft]);
            canvas.DrawLine(body.Joints[JointType.AnkleRight], body.Joints[JointType.FootRight]);

            foreach (Joint joint in body.Joints.Values)
            {
                canvas.DrawPoint(joint);
            }
        }

        public static void DrawSkeleton(this Canvas canvas, Body body, List<Tuple<string, bool>> segmentAngleComparison)
        {
            if (body == null) return;

            canvas.DrawLine(body.Joints[JointType.Head], body.Joints[JointType.Neck]);
            canvas.DrawLine(body.Joints[JointType.Neck], body.Joints[JointType.SpineShoulder]);
            canvas.DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderLeft]);
            canvas.DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderRight]);
            canvas.DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.SpineMid]);
            canvas.DrawLine(body.Joints[JointType.ShoulderLeft], body.Joints[JointType.ElbowLeft]);
            canvas.DrawLine(body.Joints[JointType.ShoulderRight], body.Joints[JointType.ElbowRight]);
            canvas.DrawLine(body.Joints[JointType.ElbowLeft], body.Joints[JointType.WristLeft]);
            canvas.DrawLine(body.Joints[JointType.ElbowRight], body.Joints[JointType.WristRight]);
            canvas.DrawLine(body.Joints[JointType.WristLeft], body.Joints[JointType.HandLeft]);
            canvas.DrawLine(body.Joints[JointType.WristRight], body.Joints[JointType.HandRight]);
            canvas.DrawLine(body.Joints[JointType.HandLeft], body.Joints[JointType.HandTipLeft]);
            canvas.DrawLine(body.Joints[JointType.HandRight], body.Joints[JointType.HandTipRight]);
            canvas.DrawLine(body.Joints[JointType.HandTipLeft], body.Joints[JointType.ThumbLeft]);
            canvas.DrawLine(body.Joints[JointType.HandTipRight], body.Joints[JointType.ThumbRight]);
            canvas.DrawLine(body.Joints[JointType.SpineMid], body.Joints[JointType.SpineBase]);
            canvas.DrawLine(body.Joints[JointType.SpineBase], body.Joints[JointType.HipLeft]);
            canvas.DrawLine(body.Joints[JointType.SpineBase], body.Joints[JointType.HipRight]);
            canvas.DrawLine(body.Joints[JointType.HipLeft], body.Joints[JointType.KneeLeft]);
            canvas.DrawLine(body.Joints[JointType.HipRight], body.Joints[JointType.KneeRight]);
            canvas.DrawLine(body.Joints[JointType.KneeLeft], body.Joints[JointType.AnkleLeft]);
            canvas.DrawLine(body.Joints[JointType.KneeRight], body.Joints[JointType.AnkleRight]);
            canvas.DrawLine(body.Joints[JointType.AnkleLeft], body.Joints[JointType.FootLeft]);
            canvas.DrawLine(body.Joints[JointType.AnkleRight], body.Joints[JointType.FootRight]);

            foreach (Joint joint in body.Joints.Values)
            {
                if (joint.JointType == JointType.Neck)
                {
                    bool isMatch = segmentAngleComparison.FirstOrDefault(item => item.Item1.Equals("Neck")).Item2;
                    SolidColorBrush color = isMatch ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                    canvas.DrawPoint(joint, color);
                }
                else if (joint.JointType == JointType.WristRight)
                {
                    bool isMatch = segmentAngleComparison.FirstOrDefault(item => item.Item1.Equals("Right wrist")).Item2;
                    SolidColorBrush color = isMatch ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                    canvas.DrawPoint(joint, color);
                }
                else if (joint.JointType == JointType.ElbowRight)
                {
                    bool isMatch = segmentAngleComparison.FirstOrDefault(item => item.Item1.Equals("Right elbow")).Item2;
                    SolidColorBrush color = isMatch ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                    canvas.DrawPoint(joint, color);
                }
                else if (joint.JointType == JointType.ShoulderRight)
                {
                    bool isMatch = segmentAngleComparison.FirstOrDefault(item => item.Item1.Equals("Right shoulder")).Item2;
                    SolidColorBrush color = isMatch ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                    canvas.DrawPoint(joint, color);
                }
                else if (joint.JointType == JointType.WristLeft)
                {
                    bool isMatch = segmentAngleComparison.FirstOrDefault(item => item.Item1.Equals("Left wrist")).Item2;
                    SolidColorBrush color = isMatch ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                    canvas.DrawPoint(joint, color);
                }
                else if (joint.JointType == JointType.ElbowLeft)
                {
                    bool isMatch = segmentAngleComparison.FirstOrDefault(item => item.Item1.Equals("Left elbow")).Item2;
                    SolidColorBrush color = isMatch ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                    canvas.DrawPoint(joint, color);
                }
                else if (joint.JointType == JointType.ShoulderLeft)
                {
                    bool isMatch = segmentAngleComparison.FirstOrDefault(item => item.Item1.Equals("Left shoulder")).Item2;
                    SolidColorBrush color = isMatch ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                    canvas.DrawPoint(joint, color);
                }
                else if (joint.JointType == JointType.SpineShoulder)
                {
                    bool isMatch = segmentAngleComparison.FirstOrDefault(item => item.Item1.Equals("Right spine") || item.Item1.Equals("Left spine") || item.Item1.Equals("Spine") || item.Item1.Equals("Right lower spine") || item.Item1.Equals("Left lower spine")).Item2;
                    SolidColorBrush color = isMatch ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                    canvas.DrawPoint(joint, color);
                }
                else if (joint.JointType == JointType.SpineMid)
                {
                    bool isMatch = segmentAngleComparison.FirstOrDefault(item => item.Item1.Equals("Mid spine")).Item2;
                    SolidColorBrush color = isMatch ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                    canvas.DrawPoint(joint, color);
                }
                else if (joint.JointType == JointType.SpineBase)
                {
                    bool isMatch = segmentAngleComparison.FirstOrDefault(item => item.Item1.Equals("Base spine") || item.Item1.Equals("Right base spine") || item.Item1.Equals("Left base spine")).Item2;
                    SolidColorBrush color = isMatch ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                    canvas.DrawPoint(joint, color);
                }
                else if (joint.JointType == JointType.HipRight)
                {
                    bool isMatch = segmentAngleComparison.FirstOrDefault(item => item.Item1.Equals("Right hip")).Item2;
                    SolidColorBrush color = isMatch ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                    canvas.DrawPoint(joint, color);
                }
                else if (joint.JointType == JointType.KneeRight)
                {
                    bool isMatch = segmentAngleComparison.FirstOrDefault(item => item.Item1.Equals("Right knee")).Item2;
                    SolidColorBrush color = isMatch ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                    canvas.DrawPoint(joint, color);
                }
                else if (joint.JointType == JointType.AnkleRight)
                {
                    bool isMatch = segmentAngleComparison.FirstOrDefault(item => item.Item1.Equals("Right ankle")).Item2;
                    SolidColorBrush color = isMatch ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                    canvas.DrawPoint(joint, color);
                }
                else if (joint.JointType == JointType.HipLeft)
                {
                    bool isMatch = segmentAngleComparison.FirstOrDefault(item => item.Item1.Equals("Left hip")).Item2;
                    SolidColorBrush color = isMatch ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                    canvas.DrawPoint(joint, color);
                }
                else if (joint.JointType == JointType.KneeLeft)
                {
                    bool isMatch = segmentAngleComparison.FirstOrDefault(item => item.Item1.Equals("Left knee")).Item2;
                    SolidColorBrush color = isMatch ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                    canvas.DrawPoint(joint, color);
                }
                else if (joint.JointType == JointType.AnkleLeft)
                {
                    bool isMatch = segmentAngleComparison.FirstOrDefault(item => item.Item1.Equals("Left ankle")).Item2;
                    SolidColorBrush color = isMatch ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                    canvas.DrawPoint(joint, color);
                }
                else canvas.DrawPoint(joint, new SolidColorBrush(Colors.Green));
            }
        }

        public static void DrawPoint(this Canvas canvas, Joint joint)
        {
            if (joint.TrackingState == TrackingState.NotTracked) return;

            joint = joint.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);

            Ellipse ellipse = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Colors.White)
            };

            Canvas.SetLeft(ellipse, joint.Position.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, joint.Position.Y - ellipse.Height / 2);

            canvas.Children.Add(ellipse);
        }

        public static void DrawPoint(this Canvas canvas, Joint joint, SolidColorBrush color)
        {
            if (joint.TrackingState == TrackingState.NotTracked) return;

            joint = joint.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);

            Ellipse ellipse = new Ellipse
            {
                Width = 15,
                Height = 15,
                Fill = color
            };

            Canvas.SetLeft(ellipse, joint.Position.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, joint.Position.Y - ellipse.Height / 2);

            canvas.Children.Add(ellipse);
        }

        public static void DrawLine(this Canvas canvas, Joint first, Joint second)
        {
            if (first.TrackingState == TrackingState.NotTracked || second.TrackingState == TrackingState.NotTracked) return;

            first = first.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);
            second = second.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);

            Line line = new Line
            {
                X1 = first.Position.X,
                Y1 = first.Position.Y,
                X2 = second.Position.X,
                Y2 = second.Position.Y,
                StrokeThickness = 8,
                Stroke = new SolidColorBrush(Colors.Yellow)
            };

            canvas.Children.Add(line);
        }

        public static double GetSegmentAngle(Skeleton skeleton, JointType type0, JointType type1, JointType type2)
        {
            Vector3 crossProduct;
            Vector3 joint0ToJoint1;
            Vector3 joint1ToJoint2;

            Joint joint0 = skeleton.Joints[type0];
            Joint joint1 = skeleton.Joints[type1];
            Joint joint2 = skeleton.Joints[type2];

            // calculate vector joining the points
            joint0ToJoint1 = new Vector3(joint0.Position.X - joint1.Position.X, joint0.Position.Y - joint1.Position.Y, joint0.Position.Z - joint1.Position.Z);
            joint1ToJoint2 = new Vector3(joint2.Position.X - joint1.Position.X, joint2.Position.Y - joint1.Position.Y, joint2.Position.Z - joint1.Position.Z);

            // normalize the vectors
            joint0ToJoint1 = Vector3.Normalize(joint0ToJoint1);
            joint1ToJoint2 = Vector3.Normalize(joint1ToJoint2);

            // find dot product between vectors
            float dotProduct = Vector3.Dot(joint0ToJoint1, joint1ToJoint2);

            // find cross product between vectors
            crossProduct = Vector3.Cross(joint0ToJoint1, joint1ToJoint2);
            float crossProductLength = crossProduct.Length();

            // calculate angle formed in radians
            double angleFormed = Math.Atan2(crossProductLength, dotProduct);
            // calculate angle formed in degree
            double angleInDegrees = angleFormed * (180 / Math.PI);
            // round to two decimal places
            double roundedAngle = Math.Round(angleInDegrees, 2);

            return roundedAngle;
        }
    }
}
