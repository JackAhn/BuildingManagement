using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AngularSimulator.Comm
{
    class Port : INotifyPropertyChanged
    {
        private int portNum;
        private String portName;

        public int PortNumber
        {
            get { return portNum; }
            set
            {
                if (value == portNum) return;
                portNum = value;
                OnPropertyChanged();
            }
        }

        [DisplayName("PortName")]
        public String PortName
        {
            get { return portName;}
            set
            {
                if (value == portName) return;
                portName = value;
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
