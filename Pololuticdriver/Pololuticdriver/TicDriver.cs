using Pololuticdriver;
//using RaspberryPiNETMF;
//using CncSerializeObject;
using RPi.I2C.Net;
using System;
using System.Collections.Generic;
//using Microsoft.SPOT;
//using SecretLabs.NETMF.Hardware.Netduino;
//using Microsoft.SPOT.Hardware;
using System.Threading;
//using WiringPiNet.Wrapper;

namespace PololuTicDriver
{
    [System.AttributeUsage(System.AttributeTargets.Class |
                       System.AttributeTargets.Struct | AttributeTargets.Property)
]
    public class SerialVisible : System.Attribute
    {
        public bool Visible { get; set; }
        public bool Refreshed { get; set; }
        public bool RefreshRequired { get; private set; }
        public SerialVisible(bool IsVisible, bool RefreshRequired = true)
        {
            Visible = IsVisible;
            this.RefreshRequired = RefreshRequired;
        }
    }
    public class PololuTicI2C
    {
        byte _address;
        const int _timeout = 300;
        const int _clockRateKhz = 400;
        /// This constant is used by the library to convert between milliamps and the
        /// Tic's native current unit, which is 32 mA.
        const Int16 TicCurrentUnits = 32;
        /// This is used to represent a null or missing value for some of the Tic's
        /// 16-bit input variables.
        const Int32 TicInputNull = 0xFFFF;
        [SerialVisible(false)]
        public byte Address { get { return _address; } private set { _address = value; } }
        byte _readAddress;
        byte _writeAddress;
        public PololuTicI2C SlaveTic { get; set; }
        SerializePropertiesObject _storProp = new SerializePropertiesObject();
        public object this[string propertyName]
        {
            get
            {
                if (this.GetType().GetProperty(propertyName).GetValue(this, null) != null)
                    return this.GetType().GetProperty(propertyName).GetValue(this, null);
                else
                    return null;
            }
            set
            {
                try
                {
                    //Console.WriteLine(propertyName + ":" + value);
                    var _prop = this.GetType().GetProperty(propertyName);
                    if (_prop != null)
                    {
                        if (_prop.CanWrite)
                        {
                            if (CanChangeType(value, _prop.PropertyType))
                            {
                                try
                                {
                                    _prop.SetValue(this, Convert.ChangeType(value, _prop.PropertyType));
                                }
                                catch (Exception ex) { Console.WriteLine("[set] error:" + ex.ToString()); }
                            }
                        }
                        Thread.Sleep(5);
                    }
                    else
                    {
                        var _meth = this.GetType().GetMethod(propertyName);
                        if (_meth != null)
                        {
                            _meth.Invoke(this, new object[1] { value });
                            Thread.Sleep(5);
                        }
                    }
                    //this.GetType().GetProperty(propertyName).SetValue(this, Convert.ChangeType(value, this.GetType().GetProperty(propertyName).GetType()));
                }
                catch (Exception ex) { Console.WriteLine("Failed to set value @tic:" + Address + ":" + value); Console.WriteLine(ex.ToString()); Console.WriteLine("Value size Invalid:" + System.Runtime.InteropServices.Marshal.SizeOf(value).ToString()); }
            }
        }
        /// <summary>
        /// Get a list of publicly visible properties of this class. Set properties using this properties Set.
        /// Usage Example:
        ///    SerializePropertiesObject spo = new SerializePropertiesObject();
        ///    spo.Data.Add(new ValueDataPair("IsEnergized",1));
        ///    Properties = spo;
        ///    "IsEnergized" is the property name you wish to modify followed by a int value. Property must have SerialVisible(true) attribute.
        /// </summary>
        [SerialVisible(false)]// do not change to true or you will have a infinite loop
        public SerializePropertiesObject Properties
        {
            get
            {
                foreach (var _k in this.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                {
                    System.Attribute[] attrs = System.Attribute.GetCustomAttributes(_k);
                    foreach (var att in attrs)
                    {
                        if (att is SerialVisible)
                        {
                            if (((SerialVisible)att).Visible)
                            {
                                try
                                {
                                    if (((SerialVisible)att).RefreshRequired)
                                    {
                                        if (!_storProp.Data.Exists((x) => { return x.Key == _k.Name; }))
                                        {
                                            Thread.Sleep(10);
                                            _storProp.Data.Add(new ValueDataPair(_k.Name, (int)Convert.ChangeType(_k.GetValue(this), _k.PropertyType)) { Refreshed = true });
                                        }
                                        else
                                        {
                                            Thread.Sleep(10);
                                            _storProp.Data[_storProp.Data.FindIndex(x => x.Key == _k.Name)] = new ValueDataPair(_k.Name, (int)Convert.ChangeType(_k.GetValue(this), _k.PropertyType)) { Refreshed = true };
                                        }
                                    }
                                    else if (!((SerialVisible)att).RefreshRequired)
                                    {
                                        if (!_storProp.Data.Exists((x) => { return x.Key == _k.Name; }))
                                        {
                                            Thread.Sleep(15);
                                            _storProp.Data.Add(new ValueDataPair(_k.Name, (int)Convert.ChangeType(_k.GetValue(this), _k.PropertyType)) { Refreshed = true });
                                        }
                                        else
                                        {
                                            if (!_storProp.Data[_storProp.Data.FindIndex(x => x.Key == _k.Name)].Refreshed)
                                            {
                                                _storProp.Data[_storProp.Data.FindIndex(x => x.Key == _k.Name)] = new ValueDataPair(_k.Name, (int)Convert.ChangeType(_k.GetValue(this), _k.PropertyType)) { Refreshed = true };
                                                Thread.Sleep(15);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                                    
                            }
                            continue;
                        }
                    }
                }
                
                return _storProp;
            }
            set
            {
                for (int i = 0; i < value.Data.Count; i++)
                {
                    var _s = value.Data[i];
                    try
                    {
                        lock (_storProp.Data)
                        {
                            if (_storProp.Data.Exists(x => x.Key == _s.Key))
                            {

                                var _propVal = _storProp.Data.FindIndex(x => x.Key == _s.Key);
                                try
                                {
                                    this[_s.Key] = _s.Value;
                                    _storProp.Data[_propVal] = new ValueDataPair(_s.Key, _s.Value);

                                }
                                catch (Exception e) { Console.WriteLine("Error Setting Tic Value:" + e.ToString()); }
                            }
                        }
                    }
                    catch (Exception ex) { Console.WriteLine("err:" + ex.ToString()); }
                }
            }
        }
        public static bool CanChangeType(object value, Type conversionType)
        {
            if (conversionType == null)
            {
                return false;
            }

            if (value == null)
            {
                return false;
            }

            IConvertible convertible = value as IConvertible;

            if (convertible == null)
            {
                return false;
            }

            return true;
        }
        int _isEnergized = 0;
        /// <summary>
        /// Set the Energized state of the tic.
        /// </summary>
        [SerialVisible(true, false)]
        public int IsEnergized
        {
            get { return _isEnergized; }
            set
            {
                if (value > 0)
                {
                    energize();
                    Thread.Sleep(4);
                    if (SlaveTic != null)
                    {
                        //Thread.Sleep(4);
                        SlaveTic.energize();
                    }
                    _isEnergized = 1;
                }
                else if (value <= 0)
                {
                    deenergize();
                    Thread.Sleep(4);
                    if (SlaveTic != null)
                    {
                        //Thread.Sleep(4);
                        SlaveTic.deenergize();
                    }
                    _isEnergized = 0;
                }
            }
        }
        [SerialVisible(false)]
        public byte ReadAddress
        {
            get
            {
                //byte k = 0x0E;
                //byte z = 1;
                _readAddress = (byte)(_address << 1);
                _readAddress = (byte)(_readAddress | 1);
                return _readAddress;
            }
        }
        [SerialVisible(false)]
        public byte WriteAddress
        {
            get
            {
                _writeAddress = (byte)(_address << 1);
                _writeAddress = (byte)(_writeAddress | 0);
                //Console.WriteLine("Address: " + _address.ToString() + " Write Address: " + _writeAddress);
                return _writeAddress;
            }
        }
        //I2CDevice i2c;
        /// <summary>
        /// Create a new instance of a tic controller
        /// </summary>
        /// <param name="address">The I2C location of the tic</param>
        /// <param name="SlaveTic"> specify the additional tic that is setup as a slave/dir tic. (using the step/DIR pins on the tic)</param>
        public PololuTicI2C(byte address, PololuTicI2C SlaveTic = null)
        {
            Console.WriteLine("Tic Setup");
            Address = address;
            ResetErrors();
            this.SlaveTic = SlaveTic;
        }
        #region SetVariables
        public void setTargetPosition(int position)
        {
            WriteRegister((byte)TicCommand.SetTargetPosition, BitConverter.GetBytes(position), 100);
        }
        /// <summary>
        /// Toggle safe start
        /// </summary>
        /// <param name="OnOff"> set safe start value</param>
        public void SafeStart(bool OnOff)
        {
            commandQuick(OnOff == true ? TicCommand.EnterSafeStart : TicCommand.ExitSafeStart);
        }
        /// <summary>
        /// This command stops the motor abruptly without respecting the deceleration limit. Besides stopping the motor, this command also sets the “position uncertain” flag (because the abrupt stop might cause steps to be missed), sets the input state to “halt”, and clears the “input after scaling” variable.
        /// </summary>
        public void HaltAndHold()
        {
            commandQuick(TicCommand.HaltAndHold);
            if (SlaveTic != null)
            {
                Thread.Sleep(1);
                SlaveTic.HaltAndHold();
            }
        }
        /// <summary>
        /// resets tic
        /// </summary>
        public void ResetTic()
        {
            commandQuick(TicCommand.Reset);
            if (SlaveTic != null)
            {
                Thread.Sleep(1);
                SlaveTic.ResetTic();
            }
        }
        /// <summary>
        /// This command temporarily sets the Tic’s maximum allowed motor speed in units of steps per 10,000 seconds. The provided value will override the corresponding setting from the Tic’s non-volatile memory until the next “reset” (or “reinitialize”) command or full microcontroller reset.
        /// </summary>
        /// <param name="maxSpeed">steps per 10,000 seconds</param>
        public void SetMaxSpeed(uint maxSpeed)
        {
            WriteRegister((byte)TicCommand.SetSpeedMax, BitConverter.GetBytes(maxSpeed), _timeout);
            if (SlaveTic != null)
            {
                Thread.Sleep(1);
                SlaveTic.SetMaxSpeed(maxSpeed);
            }
        }
        /// <summary>
        /// This command temporarily sets the Tic’s starting speed in units of steps per 10,000 seconds. This is the maximum speed at which instant acceleration and deceleration are allowed;
        /// </summary>
        /// <param name="StartingSpeed">steps per 10,000 seconds</param>
        public void SetStartingSpeed(uint StartingSpeed)
        {
            WriteRegister((byte)TicCommand.SetStartingSpeed, BitConverter.GetBytes(StartingSpeed), _timeout);
            if (SlaveTic != null)
            {
                Thread.Sleep(1);
                SlaveTic.SetStartingSpeed(StartingSpeed);
            }
        }
        /// <summary>
        /// This command temporarily sets the Tic’s maximum allowed motor acceleration in units of steps per second per 100 seconds.
        /// </summary>
        /// <param name="MaxAccel">steps per 100 seconds</param>
        public void SetMaxAccel(uint MaxAccel)
        {
            WriteRegister((byte)TicCommand.SetAccelMax, BitConverter.GetBytes(MaxAccel), _timeout);
            if (SlaveTic != null)
            {
                Thread.Sleep(1);
                SlaveTic.SetMaxAccel(MaxAccel);
            }
        }
        /// <summary>
        /// This command temporarily sets the Tic’s maximum allowed motor deceleration in units of steps per second per 100 seconds.
        /// </summary>
        /// <param name="MaxDecel"></param>
        public void SetMaxDecel(uint MaxDecel)
        {
            WriteRegister((byte)TicCommand.SetDecelMax, BitConverter.GetBytes(MaxDecel), _timeout);
            if (SlaveTic != null)
            {
                Thread.Sleep(1);
                SlaveTic.SetMaxDecel(MaxDecel);
            }
        }
        /// <summary>
        /// If the command timeout is enabled, this command resets it and prevents the “command timeout” error from happening for some time
        /// </summary>
        public void ResetCommandTimeout()
        {
            commandQuick(TicCommand.ResetCommandTimeout);
        }
        /// <summary>
        /// This command sets the target velocity of the Tic, in microsteps per 10,000 seconds.
        /// If the control mode is set to Serial / I²C / USB, the Tic will start accelerating or decelerating the motor to reach the target velocity.
        /// </summary>
        /// <param name="velocity"></param>
        public void setTargetVelocity(uint velocity)
        {
            WriteRegister((byte)TicCommand.SetTargetVelocity, BitConverter.GetBytes(velocity), _timeout);
        }
        /// <summary>
        /// This command temporarily sets the step mode (also known as microstepping mode) of the driver on the Tic, which defines how many microsteps correspond to one full step.
        /// </summary>
        /// <param name="mode"></param>
        public void SetStepMode(TicStepMode mode)
        {
            WriteRegister((byte)TicCommand.SetStepMode, new byte[1] { (byte)mode }, _timeout);
            if (SlaveTic != null)
            {
                Thread.Sleep(1);
                SlaveTic.SetStepMode(mode);
            }
            //commandW7(TicCommand.SetStepMode, (byte)mode);
        }
        /// <summary>
        /// This command causes the Tic to de-energize the stepper motor coils by disabling its stepper motor driver. The motor will stop moving and consuming power. This command sets the “position uncertain” flag (because the Tic is no longer in control of the motor’s position); the Tic will also set the “intentionally de-energized” error bit, turn on its red LED, and drive its ERR line high.
        /// The “energize” command will undo the effect of this command (except it will leave the “position uncertain” flag set) and could make the system start up again.
        /// </summary>

        public void deenergize()
        {
            commandQuick(TicCommand.Deenergize);
        }

        /// This function sends a "Halt and set position" command to the Tic.  Besides
        /// stopping the motor and setting the current position, this command also
        /// clears the "Postion uncertain" flag, sets the "Input state" to "halt", and
        /// clears the "Input after scaling" variable.

        ///<summary>
        ////// This function sends a "Halt and set position" command to the Tic.  Besides
        /// stopping the motor and setting the current position, this command also
        /// clears the "Postion uncertain" flag, sets the "Input state" to "halt", and
        /// clears the "Input after scaling" variable.
        ///</summary>
        public void haltAndSetPosition(int position)
        {
            //commandQuick(TicCommand.HaltAndSetPosition);
            WriteRegister((byte)TicCommand.HaltAndSetPosition, BitConverter.GetBytes(position), 0);
            //commandW32(TicCommand.HaltAndSetPosition, position);
        }
        /// <summary>
        /// This command is a request for the Tic to energize the stepper motor coils by enabling its stepper motor driver. The Tic will clear the “intentionally de-energized” error bit
        /// </summary>
        public void energize()
        {
            commandQuick(TicCommand.Energize);
        }
        public void setDecayMode(TicDecayMode mode)
        {
            WriteRegister((byte)TicCommand.SetDecayMode, new byte[1] { (byte)mode }, 100);
            //commandW7(TicCommand.SetDecayMode, (byte)mode);
        }
        #endregion
        public void ResetErrors()
        {
            commandQuick(TicCommand.GetVariableAndClearErrorsOccurred);
        }
        [SerialVisible(true, false)]
        public int HaltAndSetPositionZero
        {
            get { return 0; }
            set { haltAndSetPosition(0); }
        }
        int _targetPosition = -1;
        [SerialVisible(true, true)]
        public int GetTargetPosition
        {
            get
            {
                try
                {
                    if (_targetPosition == -1)
                    {
                        Thread.Sleep(15);
                        byte[] _out = new byte[4];
                        ReadRegister((byte)VarOffset.TargetPosition, ref _out, 100, sizeof(int));
                        return _targetPosition = BitConverter.ToInt32(_out, 0);
                    }
                    else
                        return _targetPosition;
                }
                catch { return _targetPosition = -1; }
            }
            set
            {
                try
                {
                    setTargetPosition(value);
                    _targetPosition = value;
                }
                catch { _targetPosition = -1; }
                //Thread.Sleep(5);
            }
        }
        int _targetVelocity = -1;
        [SerialVisible(true, false)]
        public int GetTargetVelocity
        {
            get
            {
                try
                {
                    if (_targetVelocity == -1)
                    {
                        byte[] _out = new byte[4];
                        ReadRegister((byte)VarOffset.TargetVelocity, ref _out, 100, sizeof(int));
                        return _targetVelocity = BitConverter.ToInt32(_out, 0);
                    }
                    else
                        return _targetVelocity;
                }
                catch { return -1; }
            }
            set
            {
                setTargetVelocity((uint)value);
                //Thread.Sleep(5);
            }
            //return getVar32((byte)VarOffset.TargetVelocity);
        }
        int _maxSpeed = -1;
        [SerialVisible(true, false)]
        public int GetMaxSpeed
        {
            get
            {
                try
                {
                    if (_maxSpeed == -1)
                    {
                        byte[] _out = new byte[sizeof(uint)];
                        ReadRegister((byte)VarOffset.SpeedMax, ref _out, 100, sizeof(uint));
                        return _maxSpeed = BitConverter.ToInt32(_out, 0);
                    }
                    else
                        return _maxSpeed;
                }
                catch { return -1; }
            }
            set
            {
                SetMaxSpeed(Convert.ToUInt32(value));
            }
            //return getVar32((byte)VarOffset.SpeedMax);
        }
        int _startingSpeed = -1;
        [SerialVisible(true, false)]
        public int GetStartingSpeed
        {
            get
            {
                try
                {
                    if (_startingSpeed == -1)
                    {
                        byte[] _out = new byte[sizeof(uint)];
                        ReadRegister((byte)VarOffset.StartingSpeed, ref _out, 100, _out.Length);
                        return _startingSpeed = BitConverter.ToInt32(_out, 0);
                    }
                    else
                        return _startingSpeed;
                }
                catch { return -1; }
            }
            set
            {
                SetStartingSpeed((uint)value);
                //Thread.Sleep(10);
            }
            //return getVar32((byte)VarOffset.StartingSpeed);
        }
        int _maxAccel = -1;
        [SerialVisible(true, false)]
        public int GetMaxAccel
        {
            get
            {
                try
                {
                    if (_maxAccel == -1)
                    {
                        byte[] _out = new byte[sizeof(uint)];
                        ReadRegister((byte)VarOffset.AccelMax, ref _out, 100, sizeof(uint));
                        return _maxAccel = BitConverter.ToInt32(_out, 0);
                    }
                    else
                        return _maxAccel;
                }
                catch { return -1; }
            }
            set
            {
                SetMaxAccel((uint)value);
                //Thread.Sleep(2);
            }
            //return getVar32((byte)VarOffset.AccelMax);
        }
        int _maxDecel = -1;
        [SerialVisible(true, false)]
        public int GetMaxDecel
        {
            get
            {
                try
                {
                    if (_maxDecel == -1)
                    {
                        byte[] _out = new byte[sizeof(uint)];
                        ReadRegister((byte)VarOffset.DecelMax, ref _out, 100, sizeof(uint));
                        return _maxDecel = BitConverter.ToInt32(_out, 0);
                    }
                    else
                        return _maxDecel;
                }
                catch { return -1; }
            }
            set
            {
                SetMaxDecel((uint)value);
                //Thread.Sleep(2);
            }
            //return getVar32((byte)VarOffset.DecelMax);
        }
        [SerialVisible(true, true)]
        public int GetCurrentPosition
        {
            get
            {
                try
                {
                    Thread.Sleep(5);
                    byte[] _out = new byte[sizeof(int)];
                    ReadRegister((byte)VarOffset.CurrentPosition, ref _out, 100, sizeof(int));
                    //foreach (byte t in _intBuffer)
                    //    Console.WriteLine("CurrentposBytes:" + t);
                    return BitConverter.ToInt32(_out, 0);
                }
                catch { return -1; }
            }
        }
        [SerialVisible(false, false)]
        public int GetCurrentVelocity
        {
            get
            {
                try
                {
                    byte[] _out = new byte[4];
                    ReadRegister((byte)VarOffset.CurrentVelocity, ref _out, _timeout, sizeof(int));
                    return BitConverter.ToInt32(_out, 0); ;
                }
                catch { return -1; }
            }
            //return getVar32((byte)VarOffset.CurrentVelocity);
        }
        [SerialVisible(false, false)]
        public int GetActingTargetPosition
        {
            get
            {
                try
                {
                    byte[] _out = new byte[4];
                    ReadRegister((byte)VarOffset.ActingTargetPosition, ref _out, _timeout, sizeof(int));
                    return BitConverter.ToInt32(_out, 0); ;
                }
                catch { return -1; }
            }
            //return getVar32((byte)VarOffset.ActingTargetPosition);
        }
        [SerialVisible(false, false)]
        public int GetTimeSinceLastStep
        {
            get
            {
                try
                {
                    byte[] _out = new byte[sizeof(uint)];
                    ReadRegister((byte)VarOffset.TimeSinceLastStep, ref _out, _timeout, sizeof(uint));
                    return BitConverter.ToInt32(_out, 0); ;
                }
                catch { return -1; }
            }
            //return getVar32((byte)VarOffset.TimeSinceLastStep);
        }
        /// <summary>
        /// Returns Voltage in miliamps
        /// </summary>
        /// <returns>int value for miliamps</returns>
        int GetVinVoltage
        {
            get
            {
                try
                {
                    byte[] _out = new byte[4];
                    ReadRegister((byte)VarOffset.VinVoltage, ref _out, _timeout, sizeof(int));
                    return BitConverter.ToInt32(_out, 0); ;
                }
                catch { return -1; }
            }
            //return getVar16((byte)VarOffset.VinVoltage);
        }
        TicStepMode getStepMode()
        {
            byte[] _out = new byte[4];
            ReadRegister((byte)VarOffset.StepMode, ref _out, _timeout, sizeof(byte));
            return (TicStepMode)BitConverter.ToInt32(_out, 0); ;
            //return (TicStepMode)getVar8((byte)VarOffset.StepMode);
        }
        TicDecayMode getDecayMode()
        {
            byte[] _out = new byte[4];
            ReadRegister((byte)VarOffset.DecayMode, ref _out, _timeout, sizeof(byte));
            return (TicDecayMode)BitConverter.ToInt32(_out, 0); ;
            //return (TicDecayMode)getVar8((byte)VarOffset.DecayMode);
        }
        TicInputState getInputState()
        {
            byte[] _out = new byte[4];
            ReadRegister((byte)VarOffset.InputState, ref _out, _timeout, sizeof(byte));
            return (TicInputState)BitConverter.ToInt32(_out, 0); ;
            //
            //return (TicInputState)getVar8((byte)VarOffset.InputState);
            //
        }
        TicOperationState getOperationState()
        {

            byte[] _out = new byte[4];
            ReadRegister((byte)VarOffset.OperationState, ref _out, _timeout, sizeof(byte));
            return (TicOperationState)BitConverter.ToInt32(_out, 0); ;
        }

        private void commandQuick(byte offset)
        {
            WriteRegister(offset, new byte[0], _timeout);
        }
        //
        private void commandQuick(TicCommand cmd)
        {
            WriteRegister((byte)cmd, new byte[0], _timeout);
        }
        /// <summary>
        /// write a value to the sepecified address
        /// </summary>
        /// <param name="register">register to write to</param>
        /// <param name="writeBuffer">bytes to be written</param>
        /// <param name="timeout">timeout time in milliseconds</param>
        private int WriteRegister(byte register, byte[] writeBuffer, int timeout)
        {
            //Thread.Sleep(5);
            try
            {
                byte[] by = new byte[writeBuffer.Length + 1];
                by[0] = register;
                Array.ConstrainedCopy(writeBuffer, 0, by, 1, writeBuffer.Length);
                //works
                using (var bus = RPi.I2C.Net.I2CBus.Open("/dev/i2c-1"))
                {
                    //Thread.Sleep(1);
                    bus.WriteBytes(_address, by);
                    //Thread.Sleep(1);
                }
                //Thread.Sleep(2);
                return by.Length;
            }
            catch { Console.WriteLine("error i2c write"); return -1; }
        }
        long _tmp;
        /// <summary>
        /// Read a value from the specified address/register, puts thread to sleep for 2ms after read
        /// </summary>
        /// <param name="register"></param>
        /// <param name="readBuffer"></param>
        /// <param name="timeout"></param>
        private int ReadRegister(byte register, ref byte[] readBuffer, int timeout, int readLength)
        {
            //Thread.Sleep(5);
            using (var bus = RPi.I2C.Net.I2CBus.Open("/dev/i2c-1"))
            {
                var by = new byte[2] { (byte)TicCommand.GetVariable, register };
                try
                {
                    Thread.Sleep(5);
                    bus.WriteBytes(_address, by);
                    readBuffer = bus.ReadBytes(_address, readLength);
                    Thread.Sleep(5);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Tic Read Error: Register:" + register.ToString() + Environment.NewLine + ex.ToString()); return -1;
                }
            }
            //Thread.Sleep(2);
            return readBuffer.Length;
        }
        #region enumerators
        /// This enum defines the Tic's error bits.  See the "Error handling" section of
        /// the Tic user's guide for more information about what these errors mean.
        ///
        /// See TicBase::getErrorStatus() and TicBase::getErrorsOccurred().
        public enum TicError
        {
            IntentionallyDeenergized = 0,
            MotorDriverError = 1,
            LowVin = 2,
            KillSwitch = 3,
            RequiredInputInvalid = 4,
            SerialError = 5,
            CommandTimeout = 6,
            SafeStartViolation = 7,
            ErrLineHigh = 8,
            SerialFraming = 16,
            RxOverrun = 17,
            Format = 18,
            Crc = 19,
            EncoderSkip = 20,
        };
        public enum VarOffset
        {
            OperationState = 0x00, // uint8_t
            MiscFlags1 = 0x01, // uint8_t
            ErrorStatus = 0x02, // uint16_t
            ErrorsOccurred = 0x04, // uint32_t
            PlanningMode = 0x09, // uint8_t
            TargetPosition = 0x0A, // int32_t
            TargetVelocity = 0x0E, // int32_t
            StartingSpeed = 0x12, // uint32_t
            SpeedMax = 0x16, // uint32_t
            DecelMax = 0x1A, // uint32_t
            AccelMax = 0x1E, // uint32_t
            CurrentPosition = 0x22, // int32_t
            CurrentVelocity = 0x26, // int32_t
            ActingTargetPosition = 0x2A, // int32_t
            TimeSinceLastStep = 0x2E, // uint32_t
            DeviceReset = 0x32, // uint8_t
            VinVoltage = 0x33, // uint16_t
            UpTime = 0x35, // uint32_t
            EncoderPosition = 0x39, // int32_t
            RCPulseWidth = 0x3D, // uint16_t
            AnalogReadingSCL = 0x3F, // uint16_t
            AnalogReadingSDA = 0x41, // uint16_t
            AnalogReadingTX = 0x43, // uint16_t
            AnalogReadingRX = 0x45, // uint16_t
            DigitalReadings = 0x47, // uint8_t
            PinStates = 0x48, // uint8_t
            StepMode = 0x49, // uint8_t
            CurrentLimit = 0x4A, // uint8_t
            DecayMode = 0x4B, // uint8_t
            InputState = 0x4C, // uint8_t
            InputAfterAveraging = 0x4D, // uint16_t
            InputAfterHysteresis = 0x4F, // uint16_t
            InputAfterScaling = 0x51, // uint16_t
        };
        /// This enum defines the Tic command codes which are used for its serial, I2C,
        /// and USB interface.  These codes are used by the library and you should not
        /// need to use them.
        public enum TicCommand
        {
            SetTargetPosition = 0xE0,
            SetTargetVelocity = 0xE3,
            HaltAndSetPosition = 0xEC,
            HaltAndHold = 0x89,
            ResetCommandTimeout = 0x8C,
            Deenergize = 0x86,
            Energize = 0x85,
            ExitSafeStart = 0x83,
            EnterSafeStart = 0x8F,
            Reset = 0xB0,
            ClearDriverError = 0x8A,
            SetSpeedMax = 0xE6,
            SetStartingSpeed = 0xE5,
            SetAccelMax = 0xEA,
            SetDecelMax = 0xE9,
            SetStepMode = 0x94,
            SetCurrentLimit = 0x91,
            SetDecayMode = 0x92,
            GetVariable = 0xA1,
            GetVariableAndClearErrorsOccurred = 0xA2,
            GetSetting = 0xA8,
        };

        /// This enum defines the possible operation states for the Tic.
        ///
        /// See TicBase::getOperationState().
        public enum TicOperationState
        {
            Reset = 0,
            Deenergized = 2,
            SoftError = 4,
            WaitingForErrLine = 6,
            StartingUp = 8,
            Normal = 10,
        };

        /// This enum defines the possible planning modes for the Tic's step generation
        /// code.
        ///
        /// See TicBase::getPlanningMode().
        public enum TicPlanningMode
        {
            Off = 0,
            TargetPosition = 1,
            TargetVelocity = 2,
        };

        /// This enum defines the possible causes of a full microcontroller reset for
        /// the Tic.
        ///
        /// See TicBase::getDeviceReset().
        public enum TicReset
        {
            PowerUp = 0,
            Brownout = 1,
            ResetLine = 2,
            Watchdog = 4,
            Software = 8,
            StackOverflow = 16,
            StackUnderflow = 32,
        };

        /// This enum defines the possible decay modes.
        ///
        /// See TicBase::getDecayMode() and TicBase::setDecayMode().
        public enum TicDecayMode
        {
            /// This specifies "Mixed" decay mode on the Tic T825
            /// and "Mixed 50%" on the Tic T824.
            Mixed = 0,

            /// This specifies "Slow" decay mode.
            Slow = 1,

            /// This specifies "Fast" decay mode.
            Fast = 2,

            /// This is the same as TicDecayMode::Mixed, but better expresses your
            /// intent if you want to use "Mixed 50%' mode on a Tic T834.
            Mixed50 = 0,

            /// This specifies "Mixed 25%" decay mode on the Tic T824
            /// and is the same as TicDecayMode::Mixed on the Tic T825.
            Mixed25 = 3,

            /// This specifies "Mixed 75%" decay mode on the Tic T824
            /// and is the same as TicDecayMode::Mixed on the Tic T825.
            Mixed75 = 4,
        };

        /// This enum defines the possible step modes.
        ///
        /// See TicBase::getStepMode() and TicBase::setStepMode().
        public enum TicStepMode
        {
            Full = 0,
            Half = 1,

            Microstep1 = 0x4100000,
            Microstep2 = 0x4110000,
            Microstep4 = 0x4101000,
            Microstep8 = 0x4100100,
            Microstep16 = 0x410010,
            Microstep32 = 0x410001,
        };

        /// This enum defines the Tic's control pins.
        public enum TicPin
        {
            SCL = 0,
            SDA = 1,
            TX = 2,
            RX = 3,
            RC = 4,
        };

        /// This enum defines the Tic's pin states.
        ///
        /// See TicBase::getPinState().
        public enum TicPinState
        {
            HighImpedance = 0,
            InputPullUp = 1,
            OutputLow = 2,
            OutputHigh = 3,
        };

        /// This enum defines the possible states of the Tic's main input.
        public enum TicInputState
        {
            /// The input is not ready yet.  More samples are needed, or a command has not
            /// been received yet.
            NotReady = 0,

            /// The input is invalid.
            Invalid = 1,

            /// The input is valid and is telling the Tic to halt the motor.
            Halt = 2,

            /// The input is valid and is telling the Tic to go to a target position,
            /// which you can get with TicBase::getInputAfterScaling().
            Position = 3,

            /// The input is valid and is telling the Tic to go to a target velocity,
            /// which you can get with TicBase::getInputAfterScaling().
            Velocity = 4,
        };

        /// This enum defines the bits in the Tic's Misc Flags 1 register.  You should
        /// not need to use this directly.  See TicBase::getEnergized() and
        /// TicBase::getPositionUncertain().
        public enum TicMiscFlags1 { Energized = 0, PositionUncertain = 1 };
        #endregion
    }
    public static class Extensions
    {
        public static byte[] TrimToLength(this byte[] _aryIn, int length)
        {
            byte[] _bffr = new byte[length];
            for (int i = 0; i < length; i++)
            {
                _bffr[i] = _aryIn[i];
            } return _bffr;
        }
        public static byte[] Reverse(this byte[] _wrker)
        {
            for (int i = 0; i < _wrker.Length / 2; i++)
            {
                byte tmp = _wrker[i];
                _wrker[i] = _wrker[_wrker.Length - i - 1];
                _wrker[_wrker.Length - i - 1] = tmp;
            }
            return _wrker;
        }
    }
}