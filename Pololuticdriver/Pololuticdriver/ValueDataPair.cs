using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pololuticdriver
{
    [ProtoContract]
    public class SerializePropertiesObject : INotifyPropertyChanged
    {
        [ProtoMember(1)]
        public List<ValueDataPair> Data { get { return _data; } set { _data = value; OnPropertyChanged(); } }
        List<ValueDataPair> _data = new List<ValueDataPair>();
        public event PropertyChangedEventHandler PropertyChanged;
        public ValueDataPair this[string name]
        {
            get
            {
                ValueDataPair vasr = new ValueDataPair("failed", -1);
                var k = Data.Find(x => x.Key == name);
                return k;
            }
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        public SerializePropertiesObject() { }
    }
    public class ValueDataPair : IConvertible, IComparable, INotifyPropertyChanged
    {
        [ProtoMember(1)]
        string _key = "";
        public string Key { get { return _key; } set { _key = value; NotifyPropertyChanged("Key"); } }
        [ProtoMember(2)]
        int _value;
        public int Value { get { return _value; } set { _value = value; NotifyPropertyChanged("Value"); } }
        //
        public bool Refreshed { get; set; }
        public ValueDataPair()
        {
            Key = "";
        }
        public ValueDataPair(string key, int value)
        {
            Key = key;
            Value = value;
            //Refreshed = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(PropertyInfo propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName.Name));
            }
        }
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public override string ToString()
        {
            return Key;
        }
        public TypeCode GetTypeCode()
        {
            return Value.GetTypeCode();
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            return ((IConvertible)Value).ToBoolean(provider);
        }

        public char ToChar(IFormatProvider provider)
        {
            return ((IConvertible)Value).ToChar(provider);
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            return ((IConvertible)Value).ToSByte(provider);
        }

        public byte ToByte(IFormatProvider provider)
        {
            return ((IConvertible)Value).ToByte(provider);
        }

        public short ToInt16(IFormatProvider provider)
        {
            return ((IConvertible)Value).ToInt16(provider);
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            return ((IConvertible)Value).ToUInt16(provider);
        }

        public int ToInt32(IFormatProvider provider)
        {
            return ((IConvertible)Value).ToInt32(provider);
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            return ((IConvertible)Value).ToUInt32(provider);
        }

        public long ToInt64(IFormatProvider provider)
        {
            return ((IConvertible)Value).ToInt64(provider);
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            return ((IConvertible)Value).ToUInt64(provider);
        }

        public float ToSingle(IFormatProvider provider)
        {
            return ((IConvertible)Value).ToSingle(provider);
        }

        public double ToDouble(IFormatProvider provider)
        {
            return ((IConvertible)Value).ToDouble(provider);
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            return ((IConvertible)Value).ToDecimal(provider);
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            return ((IConvertible)Value).ToDateTime(provider);
        }

        public string ToString(IFormatProvider provider)
        {
            return Value.ToString(provider);
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            return ((IConvertible)Value).ToType(conversionType, provider);
        }

        public int CompareTo(object obj)
        {
            if (obj is ValueDataPair)
            {
                return (obj as ValueDataPair).Key == this.Key && (obj as ValueDataPair).Value == this.Value ? 0 : 1;
            }
            return -1;
        }
    }
}
