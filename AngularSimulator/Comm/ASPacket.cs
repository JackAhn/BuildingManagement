using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AngularSimulator.Comm
{
    class ASPacket : INotifyPropertyChanged
    {
        private string status;
        private string timeStamp;
        private int id;
        private double x;
        private double y;

        public int Id { get => id;
            set {
                if (id == value) return;
                id = value;
                OnPropertyChanged();
            }
        }
        public string TimeStamp
        {
            get => timeStamp;
            set
            {
                if (timeStamp == value) return;
                timeStamp = value;
                OnPropertyChanged();
            }
        }

        public double X { get => x; set
            {
                if (x == value) return;
                x = value;
                OnPropertyChanged();
            }
        }
        public double Y { get => y;
            set
            {
                if (y == value) return;
                y = value;
                OnPropertyChanged();
            }
        }



        public string Status { get => status;
            set
            {
                if (status == value) return;
                status = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }



        [AttributeUsage(AttributeTargets.Method)]
        public sealed class NotifyPropertyChangedInvocatorAttribute : Attribute
        {
            public NotifyPropertyChangedInvocatorAttribute() { }
            public NotifyPropertyChangedInvocatorAttribute(string parameterName)
            {
                ParameterName = parameterName;
            }

            public string ParameterName { get; private set; }
        }
    }
}
