using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace KinectApp
{
    public class AngleStatistics : INotifyPropertyChanged
    {
        private string angleName;
        private int correctMatches;
        private double accuracyInPercentage;
        private SolidColorBrush color;

        public AngleStatistics(string angleName, int correctMatches, double accuracyInPercentage, SolidColorBrush textColor)
        {
            this.angleName = angleName;
            this.correctMatches = correctMatches;
            this.accuracyInPercentage = accuracyInPercentage;
            this.color = textColor;
        }

        public string AngleName
        {
            get
            {
                return this.angleName;
            }

            set
            {
                if (this.angleName != value)
                {
                    this.angleName = value;
                }
            }
        }

        public int CorrectMatches
        {
            get
            {
                return this.correctMatches;
            }

            set
            {
                if (this.correctMatches != value)
                {
                    this.correctMatches = value;
                    this.OnPropertyChanged("CorrectMatches");
                }
            }
        }

        public double AccuracyInPercentage
        {
            get
            {
                return this.accuracyInPercentage;
            }

            set
            {
                if (this.accuracyInPercentage != value)
                {
                    this.accuracyInPercentage = value;
                    this.OnPropertyChanged("AccuracyInPercentage");
                }
            }
        }

        public SolidColorBrush Color
        {
            get
            {
                return this.color;
            }

            set
            {
                if (this.color.Color != value.Color)
                {
                    this.color = value;
                    this.OnPropertyChanged("Color");
                }
            }
        }

        // INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
