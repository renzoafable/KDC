using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectApp
{
    public class AngleStatistics : INotifyPropertyChanged
    {
        private string angleName;
        private int correctMatches;
        private double accuracyInPercentage;

        public AngleStatistics(string angleName, int correctMatches, double accuracyInPercentage)
        {
            this.angleName = angleName;
            this.correctMatches = correctMatches;
            this.accuracyInPercentage = accuracyInPercentage;
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

        // INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
