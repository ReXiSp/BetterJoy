﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;
using BetterJoy.Collections;
using BetterJoy.Controller;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using WindowsInput.Events;

namespace BetterJoy
{
    public class Joycon
    {
        public enum Button
        {
            DpadDown = 0,
            DpadRight = 1,
            DpadLeft = 2,
            DpadUp = 3,
            SL = 4,
            SR = 5,
            Minus = 6,
            Home = 7,
            Plus = 8,
            Capture = 9,
            Stick = 10,
            Shoulder1 = 11,
            Shoulder2 = 12,

            // For pro controller
            B = 13,
            A = 14,
            Y = 15,
            X = 16,
            Stick2 = 17,
            Shoulder21 = 18,
            Shoulder22 = 19
        }

        public enum ControllerType
        {
            JoyconLeft,
            JoyconRight,
            Pro,
            SNES
        }

        public enum DebugType
        {
            None,
            All,
            Comms,
            Threading,
            IMU,
            Rumble,
            Shake
        }

        public enum Status : uint
        {
            NotAttached,
            Errored,
            Dropped,
            Attached,
            IMUDataOk
        }

        private enum ReceiveError
        {
            None,
            InvalidHandle,
            ReadError,
            InvalidPacket,
            NoData
        }

        private const int ReportLen = 49;

        private static readonly int LowFreq = int.Parse(ConfigurationManager.AppSettings["LowFreqRumble"]);
        private static readonly int HighFreq = int.Parse(ConfigurationManager.AppSettings["HighFreqRumble"]);
        private static readonly bool ToRumble = bool.Parse(ConfigurationManager.AppSettings["EnableRumble"]);
        private static readonly bool ShowAsXInput = bool.Parse(ConfigurationManager.AppSettings["ShowAsXInput"]);
        private static readonly bool ShowAsDs4 = bool.Parse(ConfigurationManager.AppSettings["ShowAsDS4"]);
        private static readonly bool UseIncrementalLights = bool.Parse(ConfigurationManager.AppSettings["UseIncrementalLights"]);
        private static readonly float DefaultDeadzone = float.Parse(ConfigurationManager.AppSettings["SticksDeadzone"]);
        private static readonly float AHRSBeta = float.Parse(ConfigurationManager.AppSettings["AHRS_beta"]);
        private static readonly float ShakeDelay = float.Parse(ConfigurationManager.AppSettings["ShakeInputDelay"]);
        private static readonly bool ShakeInputEnabled = bool.Parse(ConfigurationManager.AppSettings["EnableShakeInput"]);
        private static readonly float ShakeSensitivity = float.Parse(ConfigurationManager.AppSettings["ShakeInputSensitivity"]);

        private readonly short[] _accNeutral = { 0, 0, 0 };
        private readonly short[] _accR = { 0, 0, 0 };

        private readonly short[] _accSensiti = { 0, 0, 0 };
        private readonly ushort[] _activeStick1Data;
        private readonly ushort[] _activeStick2Data;

        private readonly MadgwickAHRS _AHRS = new(0.005f, AHRSBeta); // for getting filtered Euler angles of rotation; 5ms sampling rate

        private readonly bool[] _buttons = new bool[20];
        private readonly bool[] _buttonsDown = new bool[20];
        private readonly long[] _buttonsDownTimestamp = new long[20];
        private readonly bool[] _buttonsUp = new bool[20];
        private readonly bool[] _buttonsPrev = new bool[20];
        private readonly bool[] _buttonsRemapped = new bool[20];

        private readonly bool _changeOrientationDoubleClick = bool.Parse(ConfigurationManager.AppSettings["ChangeOrientationDoubleClick"]);

        private readonly float[] _curRotation = { 0, 0, 0, 0, 0, 0 }; // Filtered IMU data

        private readonly byte[] _defaultBuf = { 0x0, 0x1, 0x40, 0x40, 0x0, 0x1, 0x40, 0x40 };

        private readonly bool _dragToggle = bool.Parse(ConfigurationManager.AppSettings["DragToggle"]);

        private readonly string _extraGyroFeature = ConfigurationManager.AppSettings["GyroToJoyOrMouse"];
        private readonly short[] _gyrNeutral = { 0, 0, 0 };

        private readonly short[] _gyrR = { 0, 0, 0 };

        private readonly short[] _gyrSensiti = { 0, 0, 0 };

        private readonly int _gyroAnalogSensitivity = int.Parse(ConfigurationManager.AppSettings["GyroAnalogSensitivity"]);
        private readonly bool _gyroAnalogSliders = bool.Parse(ConfigurationManager.AppSettings["GyroAnalogSliders"]);
        private readonly bool _gyroHoldToggle = bool.Parse(ConfigurationManager.AppSettings["GyroHoldToggle"]);
        private readonly bool _gyroMouseLeftHanded = bool.Parse(ConfigurationManager.AppSettings["GyroMouseLeftHanded"]);
        private readonly int _gyroMouseSensitivityX = int.Parse(ConfigurationManager.AppSettings["GyroMouseSensitivityX"]);
        private readonly int _gyroMouseSensitivityY = int.Parse(ConfigurationManager.AppSettings["GyroMouseSensitivityY"]);
        private readonly float _gyroStickReduction = float.Parse(ConfigurationManager.AppSettings["GyroStickReduction"]);
        private readonly float _gyroStickSensitivityX = float.Parse(ConfigurationManager.AppSettings["GyroStickSensitivityX"]);
        private readonly float _gyroStickSensitivityY = float.Parse(ConfigurationManager.AppSettings["GyroStickSensitivityY"]);
        private readonly bool _homeLongPowerOff = bool.Parse(ConfigurationManager.AppSettings["HomeLongPowerOff"]);

        private readonly bool _IMUEnabled;
        private readonly Dictionary<int, bool> _mouseToggleBtn = new();

        private readonly float[] _otherStick = { 0, 0 };

        private readonly long _powerOffInactivityMins = int.Parse(ConfigurationManager.AppSettings["PowerOffInactivity"]);

        // Values from https://github.com/dekuNukem/Nintendo_Switch_Reverse_Engineering/blob/master/spi_flash_notes.md#6-axis-horizontal-offsets
        private readonly short[] _accProHorOffset = { -688, 0, 4038 };
        private readonly short[] _accLeftHorOffset = { 350, 0, 4081 };
        private readonly short[] _accRightHorOffset = { 350, 0, -4081 };

        private readonly Stopwatch _shakeTimer = Stopwatch.StartNew(); //Setup a timer for measuring shake in milliseconds

        private readonly byte[] _sliderVal = { 0, 0 };
        private readonly ushort[] _stickCal = { 0, 0, 0, 0, 0, 0 };
        private readonly ushort[] _stickPrecal = { 0, 0 };

        private readonly byte[] _stickRaw = { 0, 0, 0 };
        private readonly ushort[] _stick2Cal = { 0, 0, 0, 0, 0, 0 };
        private readonly ushort[] _stick2Precal = { 0, 0 };

        private readonly byte[] _stick2Raw = { 0, 0, 0 };

        private readonly bool _swapAB = bool.Parse(ConfigurationManager.AppSettings["SwapAB"]);
        private readonly bool _swapXY = bool.Parse(ConfigurationManager.AppSettings["SwapXY"]);
        private readonly bool _useFilteredIMU = bool.Parse(ConfigurationManager.AppSettings["UseFilteredIMU"]);

        private Vector3 _accG = Vector3.Zero;
        public bool ActiveGyro;

        private short[] _activeIMUData;
        private ushort _activeStick1DeadZoneData;
        private ushort _activeStick2DeadZoneData;

        public int Battery = -1;
        public bool Charging = false;

        public readonly int Connection = 3;
        public readonly int Constate = 2;
        private ushort _deadzone;
        private ushort _deadzone2;
        private readonly DebugType _debugType = (DebugType)int.Parse(ConfigurationManager.AppSettings["DebugType"]);

        private bool _doLocalize;
        private float _filterweight;

        private MainForm _form;

        private byte _globalCount;
        private Vector3 _gyrG = Vector3.Zero;

        private IntPtr _handle;
        private bool _hasShaked;

        //public DebugType debug_type = DebugType.NONE; //Keep this for manual debugging during development.
        public bool IsLeft => Type != ControllerType.JoyconRight;

        public readonly bool IsThirdParty;
        public readonly bool IsUSB;
        private long _lastDoubleClick = -1;
        public readonly int Model = 2;
        public OutputControllerDualShock4 OutDs4;

        public OutputControllerXbox360 OutXbox;
        public int PacketCounter;

        // For UdpServer
        public readonly int PadId;

        public PhysicalAddress PadMacAddress = new(new byte[] { 01, 02, 03, 04, 05, 06 });
        public readonly string Path;

        private Thread _pollThreadObj;

        private Rumble _rumbleObj;

        public readonly string SerialNumber;

        public string SerialOrMac;

        private long _shakedTime;

        private Status _state;

        public Status State
        {
            get => _state;
            private set
            {
                if (_state == value)
                {
                    return;
                }

                _state = value;
                OnStateChange(new StateChangedEventArgs(value));
            }
        }

        private float[] _stick = { 0, 0 };
        private float[] _stick2 = { 0, 0 };

        private bool _stopPolling = true;
        public ulong Timestamp;

        private long _timestampActivity = Stopwatch.GetTimestamp();

        public readonly ControllerType Type;

        public EventHandler<StateChangedEventArgs> StateChanged;

        public readonly ConcurrentList<IMUData> CalibrationIMUDatas = new();
        public readonly ConcurrentList<SticksData> CalibrationStickDatas = new();
        private bool _calibrateSticks = false;
        private bool _calibrateIMU = false;

        public readonly ReaderWriterLockSlim HidapiLock = new ReaderWriterLockSlim();

        private Stopwatch _timeSinceReceive = new();
        private RollingAverage _avgReceiveDeltaMs = new(100); // delta is around 10-16ms, so rolling average over 1000-1600ms 

        public Joycon(
            MainForm form,
            IntPtr handle,
            bool imu,
            bool localize,
            float alpha,
            string path,
            string serialNum,
            bool isUSB,
            int id,
            ControllerType type,
            bool isThirdParty = false
        )
        {
            _form = form;
            SerialNumber = serialNum;
            SerialOrMac = serialNum;
            _activeIMUData = new short[6];
            _activeStick1Data = new ushort[6];
            _activeStick2Data = new ushort[6];
            _handle = handle;
            _IMUEnabled = imu;
            _doLocalize = localize;
            _rumbleObj = new Rumble(new float[] { LowFreq, HighFreq, 0 });
            for (var i = 0; i < _buttonsDownTimestamp.Length; i++)
            {
                _buttonsDownTimestamp[i] = -1;
            }

            _filterweight = alpha;

            PadId = id;
            LED = (byte)(0x1 << PadId);

            IsUSB = isUSB;
            Type = type;
            IsThirdParty = isThirdParty;
            Path = path;

            Connection = isUSB ? 0x01 : 0x02;

            if (ShowAsXInput)
            {
                OutXbox = new OutputControllerXbox360();
                if (ToRumble)
                {
                    OutXbox.FeedbackReceived += ReceiveRumble;
                }
            }

            if (ShowAsDs4)
            {
                OutDs4 = new OutputControllerDualShock4();
                if (ToRumble)
                {
                    OutDs4.FeedbackReceived += Ds4_FeedbackReceived;
                }
            }
        }

        public bool IsPro => Type is ControllerType.Pro or ControllerType.SNES;
        public bool IsSNES => Type == ControllerType.SNES;
        public bool IsJoycon => Type is ControllerType.JoyconRight or ControllerType.JoyconLeft;

        public Joycon Other;

        public byte LED { get; private set; }

        public void SetLEDByPlayerNum(int id)
        {
            if (id > 3)
            {
                // No support for any higher than 3 (4 Joycons/Controllers supported in the application normally)
                id = 3;
            }

            if (UseIncrementalLights)
            {
                // Set all LEDs from 0 to the given id to lit
                var ledId = id;
                LED = 0x0;
                do
                {
                    LED |= (byte)(0x1 << ledId);
                } while (--ledId >= 0);
            }
            else
            {
                LED = (byte)(0x1 << id);
            }

            SetPlayerLED(LED);
        }

        public void SetLEDByPadID()
        {
            // If the other Joycon is itself, the Joycon is sideways
            if (Other == null || Other == this)
            {
                // Set LED to current Pad ID
                SetLEDByPlayerNum(PadId);
            }
            else
            {
                // Set LED to current Joycon Pair
                var lowestPadId = Math.Min(Other.PadId, PadId);
                SetLEDByPlayerNum(lowestPadId);
            }
        }

        public void GetActiveIMUData()
        {
            _activeIMUData = _form.ActiveCaliIMUData(SerialOrMac);
        }

        public void GetActiveSticksData()
        {
            {
                var activeSticksData = _form.ActiveCaliSticksData(SerialOrMac);
                Array.Copy(activeSticksData, _activeStick1Data, 6);
                Array.Copy(activeSticksData, 6, _activeStick2Data, 0, 6);
            }
            _activeStick1DeadZoneData = CalculateDeadzone(_activeStick1Data, DefaultDeadzone);
            _activeStick2DeadZoneData = CalculateDeadzone(_activeStick2Data, DefaultDeadzone);
        }

        public ushort CalculateDeadzone(ushort[] stickDatas, float deadzone)
        {
            var deadzone1 = (ushort)Math.Round(Math.Abs(stickDatas[0] + stickDatas[4]) * deadzone);
            var deadzone2 = (ushort)Math.Round(Math.Abs(stickDatas[1] + stickDatas[5]) * deadzone);

            return Math.Max(deadzone1, deadzone2);
        }

        public void ReceiveRumble(Xbox360FeedbackReceivedEventArgs e)
        {
            DebugPrint("Rumble data Received: XInput", DebugType.Rumble);
            SetRumble(LowFreq, HighFreq, Math.Max(e.LargeMotor, e.SmallMotor) / (float)255);

            if (Other != null && Other != this)
            {
                Other.SetRumble(LowFreq, HighFreq, Math.Max(e.LargeMotor, e.SmallMotor) / (float)255);
            }
        }

        public void Ds4_FeedbackReceived(DualShock4FeedbackReceivedEventArgs e)
        {
            DebugPrint("Rumble data Received: DS4", DebugType.Rumble);
            SetRumble(LowFreq, HighFreq, Math.Max(e.LargeMotor, e.SmallMotor) / (float)255);

            if (Other != null && Other != this)
            {
                Other.SetRumble(LowFreq, HighFreq, Math.Max(e.LargeMotor, e.SmallMotor) / (float)255);
            }
        }

        private void OnStateChange(StateChangedEventArgs e)
        {
            StateChanged?.Invoke(this, e);
        }

        public void DebugPrint(string s, DebugType d)
        {
            if (_debugType == DebugType.None)
            {
                return;
            }

            if (d == DebugType.All || d == _debugType || _debugType == DebugType.All)
            {
                _form.AppendTextBox("[J" + (PadId + 1) + "] " + s);
            }
        }

        public Vector3 GetGyro()
        {
            return _gyrG;
        }

        public Vector3 GetAccel()
        {
            return _accG;
        }

        public void Reset()
        {
            _form.AppendTextBox("Resetting connection.");
            SetHCIState(0x01);
        }

        public void Attach()
        {
            if (State > Status.Dropped)
            {
                return;
            }

            if (_handle == IntPtr.Zero)
            {
                throw new Exception("reset hidapi");
            }

            // set report mode to simple HID mode (fix SPI read not working when controller is already initialized)
            // do not always send a response so we don't check if there is one
            Subcommand(0x3, new byte[] { 0x3F }, 1);

            // Connect
            if (IsUSB)
            {
                _form.AppendTextBox("Using USB.");

                var buf = new byte[ReportLen];

                // Get MAC
                buf[0] = 0x80;
                buf[1] = 0x1;
                HIDApi.hid_write(_handle, buf, new UIntPtr(2));
                if (ReadUSBCheck(buf, 0x1) == 0)
                {
                    // can occur when USB connection isn't closed properly
                    Reset();
                    throw new Exception("reset mac");
                }

                PadMacAddress = new PhysicalAddress(new[] { buf[9], buf[8], buf[7], buf[6], buf[5], buf[4] });
                SerialOrMac = PadMacAddress.ToString().ToLower();

                // USB Pairing
                buf[0] = 0x80;
                buf[1] = 0x2; // Handshake
                HIDApi.hid_write(_handle, buf, new UIntPtr(2));
                if (ReadUSBCheck(buf, 0x2) == 0)
                {
                    // can occur when another software sends commands to the device, disable PurgeAffectedDevice in the config to avoid this
                    Reset();
                    throw new Exception("reset handshake");
                }

                buf[0] = 0x80;
                buf[1] = 0x3; // 3Mbit baud rate
                HIDApi.hid_write(_handle, buf, new UIntPtr(2));
                if (ReadUSBCheck(buf, 0x3) == 0)
                {
                    Reset();
                    throw new Exception("reset baud rate");
                }

                buf[0] = 0x80;
                buf[1] = 0x2; // Handshake at new baud rate
                HIDApi.hid_write(_handle, buf, new UIntPtr(2));
                if (ReadUSBCheck(buf, 0x2) == 0)
                {
                    Reset();
                    throw new Exception("reset new handshake");
                }

                buf[0] = 0x80;
                buf[1] = 0x4; // Prevent HID timeout
                HIDApi.hid_write(_handle, buf, new UIntPtr(2)); // does not send a response
            }
            else
            {
                _form.AppendTextBox("Using Bluetooth.");

                // Serial = MAC address of the controller in bluetooth
                var mac = new byte[6];
                try
                {
                    for (var n = 0; n < 6 && n < SerialNumber.Length; n++)
                    {
                        mac[n] = byte.Parse(SerialNumber.AsSpan(n * 2, 2), NumberStyles.HexNumber);
                    }
                }
                catch (Exception)
                {
                    // could not parse mac address
                }

                PadMacAddress = new PhysicalAddress(mac);
            }

            var ok = DumpCalibrationData();
            if (!ok)
            {
                Reset();
                throw new Exception("reset calibration");
            }

            // Bluetooth manual pairing
            //byte[] btmac_host = Program.btMAC.GetAddressBytes();
            // send host MAC and acquire Joycon MAC
            //byte[] reply = Subcommand(0x01, new byte[] { 0x01, btmac_host[5], btmac_host[4], btmac_host[3], btmac_host[2], btmac_host[1], btmac_host[0] }, 7, true);
            //byte[] LTKhash = Subcommand(0x01, new byte[] { 0x02 }, 1, true);
            // save pairing info
            //Subcommand(0x01, new byte[] { 0x03 }, 1, true);

            BlinkHomeLight();
            SetLEDByPlayerNum(PadId);

            Subcommand(0x40, new[] { _IMUEnabled ? (byte)0x1 : (byte)0x0 }, 1); // enable IMU
            Subcommand(0x48, new byte[] { 0x01 }, 1); // enable vibrations
            Subcommand(0x3, new byte[] { 0x30 }, 1); // set report mode to NPad standard mode

            State = Status.Attached;

            DebugPrint("Done with init.", DebugType.Comms);
        }

        public void SetPlayerLED(byte leds = 0x0)
        {
            Subcommand(0x30, new[] { leds }, 1);
        }

        public void BlinkHomeLight()
        {
            // do not call after initial setup
            if (IsThirdParty)
            {
                return;
            }

            var buf = Enumerable.Repeat((byte)0xFF, 25).ToArray();
            buf[0] = 0x18;
            buf[1] = 0x01;
            Subcommand(0x38, buf, 25);
        }

        public void SetHomeLight(bool on)
        {
            if (IsThirdParty)
            {
                return;
            }

            var buf = Enumerable.Repeat((byte)0xFF, 25).ToArray();
            if (on)
            {
                buf[0] = 0x1F;
                buf[1] = 0xF0;
            }
            else
            {
                buf[0] = 0x10;
                buf[1] = 0x01;
            }

            Subcommand(0x38, buf, 25);
        }

        private void SetHCIState(byte state)
        {
            Subcommand(0x06, new[] { state }, 1);
        }

        public void PowerOff()
        {
            if (State > Status.Dropped)
            {
                SetHCIState(0x00);
                State = Status.Dropped;
            }
        }

        private void BatteryChanged()
        {
            // battery changed level
            _form.SetBatteryColor(this, Battery);

            if (!IsUSB && !Charging && Battery <= 1)
            {
                var msg = $"Controller {PadId} ({GetControllerName()}) - low battery notification!";
                _form.Tooltip(msg);
            }
        }

        private void ChargingChanged()
        {
            _form.SetCharging(this, Charging);
        }

        public void SetFilterCoeff(float a)
        {
            _filterweight = a;
        }

        public void Detach(bool close = true)
        {
            if (State == Status.NotAttached)
            {
                return;
            }

            _stopPolling = true;
            _pollThreadObj?.Join();

            DisconnectViGEm();

            if (_handle != IntPtr.Zero)
            {
                if (State > Status.Dropped)
                {
                    //Subcommand(0x40, new byte[] { 0x0 }, 1); // disable IMU sensor
                    //Subcommand(0x48, new byte[] { 0x0 }, 1); // Would turn off rumble?

                    Subcommand(0x3, new byte[] { 0x3F }, 1); // set report mode to simple HID mode

                    // Commented because you need to restart the controller to reconnect in usb again with the following
                    //if (IsUSB)
                    //{
                        //var buf = new byte[ReportLen];
                        //buf[0] = 0x80; buf[1] = 0x5; // Allow device to talk to BT again
                        //HIDApi.hid_write(_handle, buf, new UIntPtr(2));
                        //ReadUSBCheck(buf, 0x5);
                        //buf[0] = 0x80; buf[1] = 0x6; // Allow device to talk to BT again
                        //HIDApi.hid_write(_handle, buf, new UIntPtr(2));
                        //ReadUSBCheck(buf, 0x6);
                    //}
                }

                if (close)
                {
                    HidapiLock.EnterWriteLock();
                    try
                    {
                        HIDApi.hid_close(_handle);
                        _handle = IntPtr.Zero;
                    }
                    finally
                    {
                        HidapiLock.ExitWriteLock();
                    }
                }
            }

            State = Status.NotAttached;
        }

        public void Drop(bool error = false)
        {
            _stopPolling = true;
            _pollThreadObj?.Join();

            State = error ? Status.Errored : Status.Dropped;
        }

        public void ConnectViGEm()
        {
            OutXbox?.Connect();
            OutDs4?.Connect();
        }

        public void DisconnectViGEm()
        {
            try
            {
                OutXbox?.Disconnect();
                OutDs4?.Disconnect();
            }
            catch { } // nothing we can do, might not be connected in the first place
        }

        // Run from poll thread
        private ReceiveError ReceiveRaw(byte[] buf)
        {
            if (_handle == IntPtr.Zero)
            {
                return ReceiveError.InvalidHandle;
            }

            // The controller should report back at 60hz or between 60-120hz for the Pro Controller in USB
            var length = HIDApi.hid_read_timeout(_handle, buf, new UIntPtr(ReportLen), 100);

            if (length < 0)
            {
                return ReceiveError.ReadError;
            }

            if (length == 0)
            {
                return ReceiveError.NoData;
            }

            if (buf[0] != 0x30)
            {
                // 0x30 = standard full mode report
                return ReceiveError.InvalidPacket;
            }

            // Determine the IMU timestamp with a rolling average instead of relying on the unreliable packet's timestamp
            // more detailed explanations on why : https://github.com/torvalds/linux/blob/52b1853b080a082ec3749c3a9577f6c71b1d4a90/drivers/hid/hid-nintendo.c#L1115
            long deltaReceiveMs = 0;
            if (_timeSinceReceive.IsRunning)
            {
                deltaReceiveMs = _timeSinceReceive.ElapsedMilliseconds;
                _avgReceiveDeltaMs.AddValue((int)deltaReceiveMs);
            }
            _timeSinceReceive.Restart();

            // clear remaining of buffer just to be safe
            if (length < ReportLen)
            {
                Array.Clear(buf, length, ReportLen - length);
            }

            // Process packets as soon as they come
            const int nbPackets = 3;
            var deltaPacketsMicroseconds = (ulong)(_avgReceiveDeltaMs.GetAverage() / nbPackets * 1000);

            for (var n = 0; n < nbPackets; n++)
            {
                ExtractIMUValues(buf, n);

                if (n == 0)
                {
                    ProcessButtonsAndStick(buf);
                    DoThingsWithButtons();
                    GetBatteryInfos(buf);
                }

                Timestamp += deltaPacketsMicroseconds;

                PacketCounter++;
                Program.Server?.NewReportIncoming(this);
            }

            try
            {
                OutDs4?.UpdateInput(MapToDualShock4Input(this));
                OutXbox?.UpdateInput(MapToXbox360Input(this));
            }
            catch { } // ignore

            //DebugPrint($"Bytes read: {length:D}. Elapsed: {deltaReceiveMs}ms AVG: {_avgReceiveDeltaMs.GetAverage()}ms", DebugType.Threading);

            return ReceiveError.None;
        }

        private void DetectShake()
        {
            if (ShakeInputEnabled)
            {
                var currentShakeTime = _shakeTimer.ElapsedMilliseconds;

                // Shake detection logic
                var isShaking = GetAccel().LengthSquared() >= ShakeSensitivity;
                if ((isShaking && currentShakeTime >= _shakedTime + ShakeDelay) || (isShaking && _shakedTime == 0))
                {
                    _shakedTime = currentShakeTime;
                    _hasShaked = true;

                    // Mapped shake key down
                    Simulate(Config.Value("shake"), false);
                    DebugPrint("Shaked at time: " + _shakedTime, DebugType.Shake);
                }

                // If controller was shaked then release mapped key after a small delay to simulate a button press, then reset hasShaked
                if (_hasShaked && currentShakeTime >= _shakedTime + 10)
                {
                    // Mapped shake key up
                    Simulate(Config.Value("shake"), false, true);
                    DebugPrint("Shake completed", DebugType.Shake);
                    _hasShaked = false;
                }
            }
            else
            {
                _shakeTimer.Stop();
            }
        }

        private void Simulate(string s, bool click = true, bool up = false)
        {
            if (s.StartsWith("key_"))
            {
                var key = (KeyCode)int.Parse(s.AsSpan(4));
                if (click)
                {
                    WindowsInput.Simulate.Events().Click(key).Invoke();
                }
                else
                {
                    if (up)
                    {
                        WindowsInput.Simulate.Events().Release(key).Invoke();
                    }
                    else
                    {
                        WindowsInput.Simulate.Events().Hold(key).Invoke();
                    }
                }
            }
            else if (s.StartsWith("mse_"))
            {
                var button = (ButtonCode)int.Parse(s.AsSpan(4));
                if (click)
                {
                    WindowsInput.Simulate.Events().Click(button).Invoke();
                }
                else
                {
                    if (_dragToggle)
                    {
                        if (!up)
                        {
                            bool release;
                            _mouseToggleBtn.TryGetValue((int)button, out release);
                            if (release)
                            {
                                WindowsInput.Simulate.Events().Release(button).Invoke();
                            }
                            else
                            {
                                WindowsInput.Simulate.Events().Hold(button).Invoke();
                            }

                            _mouseToggleBtn[(int)button] = !release;
                        }
                    }
                    else
                    {
                        if (up)
                        {
                            WindowsInput.Simulate.Events().Release(button).Invoke();
                        }
                        else
                        {
                            WindowsInput.Simulate.Events().Hold(button).Invoke();
                        }
                    }
                }
            }
        }

        // For Joystick->Joystick inputs
        private void SimulateContinous(int origin, string s)
        {
            if (s.StartsWith("joy_"))
            {
                var button = int.Parse(s.AsSpan(4));
                _buttonsRemapped[button] |= _buttons[origin];
            }
        }

        private void ReleaseRemappedButtons()
        {
            // overwrite custom-mapped buttons
            if (Config.Value("capture") != "0")
            {
                _buttonsRemapped[(int)Button.Capture] = false;
            }

            if (Config.Value("home") != "0")
            {
                _buttonsRemapped[(int)Button.Home] = false;
            }

            // single joycon mode
            if (IsLeft)
            {
                if (Config.Value("sl_l") != "0")
                {
                    _buttonsRemapped[(int)Button.SL] = false;
                }

                if (Config.Value("sr_l") != "0")
                {
                    _buttonsRemapped[(int)Button.SR] = false;
                }
            }
            else
            {
                if (Config.Value("sl_r") != "0")
                {
                    _buttonsRemapped[(int)Button.SL] = false;
                }

                if (Config.Value("sr_r") != "0")
                {
                    _buttonsRemapped[(int)Button.SR] = false;
                }
            }
        }

        private void SimulateRemappedButtons()
        {
            if (_buttonsDown[(int)Button.Capture])
            {
                Simulate(Config.Value("capture"), false);
            }

            if (_buttonsUp[(int)Button.Capture])
            {
                Simulate(Config.Value("capture"), false, true);
            }

            if (_buttonsDown[(int)Button.Home])
            {
                Simulate(Config.Value("home"), false);
            }

            if (_buttonsUp[(int)Button.Home])
            {
                Simulate(Config.Value("home"), false, true);
            }

            SimulateContinous((int)Button.Capture, Config.Value("capture"));
            SimulateContinous((int)Button.Home, Config.Value("home"));

            if (IsLeft)
            {
                if (_buttonsDown[(int)Button.SL])
                {
                    Simulate(Config.Value("sl_l"), false);
                }

                if (_buttonsUp[(int)Button.SL])
                {
                    Simulate(Config.Value("sl_l"), false, true);
                }

                if (_buttonsDown[(int)Button.SR])
                {
                    Simulate(Config.Value("sr_l"), false);
                }

                if (_buttonsUp[(int)Button.SR])
                {
                    Simulate(Config.Value("sr_l"), false, true);
                }

                SimulateContinous((int)Button.SL, Config.Value("sl_l"));
                SimulateContinous((int)Button.SR, Config.Value("sr_l"));
            }
            else
            {
                if (_buttonsDown[(int)Button.SL])
                {
                    Simulate(Config.Value("sl_r"), false);
                }

                if (_buttonsUp[(int)Button.SL])
                {
                    Simulate(Config.Value("sl_r"), false, true);
                }

                if (_buttonsDown[(int)Button.SR])
                {
                    Simulate(Config.Value("sr_r"), false);
                }

                if (_buttonsUp[(int)Button.SR])
                {
                    Simulate(Config.Value("sr_r"), false, true);
                }

                SimulateContinous((int)Button.SL, Config.Value("sl_r"));
                SimulateContinous((int)Button.SR, Config.Value("sr_r"));
            }
        }

        private void RemapButtons()
        {
            lock (_buttonsRemapped)
            {
                lock (_buttons)
                {
                    Array.Copy(_buttons, _buttonsRemapped, _buttons.Length);

                    ReleaseRemappedButtons();
                    SimulateRemappedButtons();
                }
            }
        }

        private void DoThingsWithButtons()
        {
            var powerOffButton = (int)(IsPro || !IsLeft || (Other != null && Other != this) ? Button.Home : Button.Capture);

            var timestampNow = Stopwatch.GetTimestamp();
            if (_homeLongPowerOff && _buttons[powerOffButton] && !IsUSB)
            {
                var powerOffPressedDurationMs = (timestampNow - _buttonsDownTimestamp[powerOffButton]) / 10000;
                if (powerOffPressedDurationMs > 2000)
                {
                    if (Other != null)
                    {
                        Program.Mgr.PowerOff(Other);
                    }
                    PowerOff();
                    return;
                }
            }

            if (IsJoycon && !_calibrateSticks && !_calibrateIMU)
            {
                if (_changeOrientationDoubleClick && _buttonsDown[(int)Button.Stick] && _lastDoubleClick != -1)
                {
                    if (_buttonsDownTimestamp[(int)Button.Stick] - _lastDoubleClick < 3000000)
                    {
                        Program.Mgr.JoinOrSplitJoycon(this);

                        _lastDoubleClick = _buttonsDownTimestamp[(int)Button.Stick];
                        return;
                    }

                    _lastDoubleClick = _buttonsDownTimestamp[(int)Button.Stick];
                }
                else if (_changeOrientationDoubleClick && _buttonsDown[(int)Button.Stick])
                {
                    _lastDoubleClick = _buttonsDownTimestamp[(int)Button.Stick];
                }
            }

            if (_powerOffInactivityMins > 0 && !IsUSB)
            {
                var timeSinceActivityMs = (timestampNow - _timestampActivity) / 10000;
                if (timeSinceActivityMs > _powerOffInactivityMins * 60 * 1000)
                {
                    if (Other != null)
                    {
                        Program.Mgr.PowerOff(Other);
                    }
                    PowerOff();
                    return;
                }
            }

            DetectShake();

            RemapButtons();

            // Filtered IMU data
            _AHRS.GetEulerAngles(_curRotation);
            const float dt = 0.015f; // 15ms

            if (_gyroAnalogSliders && (Other != null || IsPro))
            {
                var leftT = IsLeft ? Button.Shoulder2 : Button.Shoulder22;
                var rightT = IsLeft ? Button.Shoulder22 : Button.Shoulder2;
                var left = IsLeft || IsPro ? this : Other;
                var right = !IsLeft || IsPro ? this : Other;

                int ldy, rdy;
                if (_useFilteredIMU)
                {
                    ldy = (int)(_gyroAnalogSensitivity * (left._curRotation[0] - left._curRotation[3]));
                    rdy = (int)(_gyroAnalogSensitivity * (right._curRotation[0] - right._curRotation[3]));
                }
                else
                {
                    ldy = (int)(_gyroAnalogSensitivity * (left._gyrG.Y * dt));
                    rdy = (int)(_gyroAnalogSensitivity * (right._gyrG.Y * dt));
                }

                if (_buttons[(int)leftT])
                {
                    _sliderVal[0] = (byte)Math.Min(byte.MaxValue, Math.Max(0, _sliderVal[0] + ldy));
                }
                else
                {
                    _sliderVal[0] = 0;
                }

                if (_buttons[(int)rightT])
                {
                    _sliderVal[1] = (byte)Math.Min(byte.MaxValue, Math.Max(0, _sliderVal[1] + rdy));
                }
                else
                {
                    _sliderVal[1] = 0;
                }
            }

            var resVal = Config.Value("active_gyro");
            if (resVal.StartsWith("joy_"))
            {
                var i = int.Parse(resVal.AsSpan(4));
                if (_gyroHoldToggle)
                {
                    if (_buttonsDown[i] || (Other != null && Other._buttonsDown[i]))
                    {
                        ActiveGyro = true;
                    }
                    else if (_buttonsUp[i] || (Other != null && Other._buttonsUp[i]))
                    {
                        ActiveGyro = false;
                    }
                }
                else
                {
                    if (_buttonsDown[i] || (Other != null && Other._buttonsDown[i]))
                    {
                        ActiveGyro = !ActiveGyro;
                    }
                }
            }

            if (_extraGyroFeature.StartsWith("joy"))
            {
                if (Config.Value("active_gyro") == "0" || ActiveGyro)
                {
                    var controlStick = _extraGyroFeature == "joy_left" ? _stick : _stick2;

                    float dx, dy;
                    if (_useFilteredIMU)
                    {
                        dx = _gyroStickSensitivityX * (_curRotation[1] - _curRotation[4]); // yaw
                        dy = -(_gyroStickSensitivityY * (_curRotation[0] - _curRotation[3])); // pitch
                    }
                    else
                    {
                        dx = _gyroStickSensitivityX * (_gyrG.Z * dt); // yaw
                        dy = -(_gyroStickSensitivityY * (_gyrG.Y * dt)); // pitch
                    }

                    controlStick[0] = Math.Max(-1.0f, Math.Min(1.0f, controlStick[0] / _gyroStickReduction + dx));
                    controlStick[1] = Math.Max(-1.0f, Math.Min(1.0f, controlStick[1] / _gyroStickReduction + dy));
                }
            }
            else if (_extraGyroFeature == "mouse" &&
                     (IsPro || Other == null || (Other != null && (_gyroMouseLeftHanded ? IsLeft : !IsLeft))))
            {
                // gyro data is in degrees/s
                if (Config.Value("active_gyro") == "0" || ActiveGyro)
                {
                    int dx, dy;

                    if (_useFilteredIMU)
                    {
                        dx = (int)(_gyroMouseSensitivityX * (_curRotation[1] - _curRotation[4])); // yaw
                        dy = (int)-(_gyroMouseSensitivityY * (_curRotation[0] - _curRotation[3])); // pitch
                    }
                    else
                    {
                        dx = (int)(_gyroMouseSensitivityX * (_gyrG.Z * dt));
                        dy = (int)-(_gyroMouseSensitivityY * (_gyrG.Y * dt));
                    }

                    WindowsInput.Simulate.Events().MoveBy(dx, dy).Invoke();
                }

                // reset mouse position to centre of primary monitor
                resVal = Config.Value("reset_mouse");
                if (resVal.StartsWith("joy_"))
                {
                    var i = int.Parse(resVal.AsSpan(4));
                    if (_buttonsDown[i] || (Other != null && Other._buttonsDown[i]))
                    {
                        WindowsInput.Simulate.Events()
                                    .MoveTo(
                                        Screen.PrimaryScreen.Bounds.Width / 2,
                                        Screen.PrimaryScreen.Bounds.Height / 2
                                    )
                                    .Invoke();
                    }
                }
            }
        }

        private void GetBatteryInfos(byte[] reportBuf)
        {
            var prevBattery = Battery;
            var prevCharging = Charging;

            byte highNibble = (byte)(reportBuf[2] >> 4);
            Battery = highNibble / 2;
            Charging = (highNibble & 0x1) == 1;

            if (prevBattery != Battery)
            {
                BatteryChanged();
            }

            if (prevCharging != Charging)
            {
                ChargingChanged();
            }
        }

        private void Poll()
        {
            var buf = new byte[ReportLen];
            _stopPolling = false;
            Stopwatch timeSinceError = new();
            int dropAfterMs = IsUSB ? 1500 : 3000;

            // For IMU timestamp calculation
            _avgReceiveDeltaMs.Clear();
            _avgReceiveDeltaMs.AddValue(15); // default value of 15ms between packets
            _timeSinceReceive.Reset();
            Timestamp = 0;

            while (!_stopPolling && State > Status.Dropped)
            {
                {
                    var data = _rumbleObj.GetData();
                    if (data != null)
                    {
                        SendRumble(buf, data);
                    }
                }
                var error = ReceiveRaw(buf);

                if (error == ReceiveError.None && State > Status.Dropped)
                {
                    State = Status.IMUDataOk;
                    timeSinceError.Reset();
                }
                else if (timeSinceError.ElapsedMilliseconds > dropAfterMs)
                {
                    State = Status.Errored;
                    _form.AppendTextBox("Dropped.");
                    DebugPrint("Connection lost. Is the controller connected?", DebugType.All);
                }
                else if (error == ReceiveError.InvalidHandle)
                {
                    // should not happen
                    State = Status.Errored;
                    _form.AppendTextBox("Dropped (invalid handle).");
                }
                else
                {
                    timeSinceError.Start();

                    // No data read, read error or invalid packet
                    if (error == ReceiveError.ReadError)
                    {
                        Thread.Sleep(5); // to avoid spin
                    }
                }
            }
        }

        private int ProcessButtonsAndStick(byte[] reportBuf)
        {
            if (!IsSNES)
            {
                var reportOffset = IsLeft ? 0 : 3;
                _stickRaw[0] = reportBuf[6 + reportOffset];
                _stickRaw[1] = reportBuf[7 + reportOffset];
                _stickRaw[2] = reportBuf[8 + reportOffset];

                if (IsPro)
                {
                    reportOffset = !IsLeft ? 0 : 3;
                    _stick2Raw[0] = reportBuf[6 + reportOffset];
                    _stick2Raw[1] = reportBuf[7 + reportOffset];
                    _stick2Raw[2] = reportBuf[8 + reportOffset];
                }

                _stickPrecal[0] = (ushort)(_stickRaw[0] | ((_stickRaw[1] & 0xf) << 8));
                _stickPrecal[1] = (ushort)((_stickRaw[1] >> 4) | (_stickRaw[2] << 4));

                var cal = _stickCal;
                var dz = _deadzone;

                if (_form.AllowCalibration)
                {
                    cal = _activeStick1Data;
                    dz = _activeStick1DeadZoneData;
                }

                CalculateStickCenter(_stickPrecal, cal, dz, _stick);

                if (IsPro)
                {
                    _stick2Precal[0] = (ushort)(_stick2Raw[0] | ((_stick2Raw[1] & 0xf) << 8));
                    _stick2Precal[1] = (ushort)((_stick2Raw[1] >> 4) | (_stick2Raw[2] << 4));

                    cal = _stick2Cal;
                    dz = _deadzone2;

                    if (_form.AllowCalibration)
                    {
                        cal = _activeStick2Data;
                        dz = _activeStick2DeadZoneData;
                    }

                    CalculateStickCenter(_stick2Precal, cal, dz, _stick2);
                }

                if (_calibrateSticks)
                {
                    var sticks = new SticksData(
                        _stickPrecal[0],
                        _stickPrecal[1],
                        _stick2Precal[0],
                        _stick2Precal[1]
                    );
                    CalibrationStickDatas.Add(sticks);
                }
                else
                {
                    //DebugPrint($"Stick1: X={stick[0]} Y={stick[1]}. Stick2: X={stick2[0]} Y={stick2[1]}", DebugType.THREADING);
                }

                // Read other Joycon's sticks
                if (Other != null && Other != this)
                {
                    lock (_otherStick)
                    {
                        // Read other stick sent by other joycon
                        if (IsLeft)
                        {
                            Array.Copy(_otherStick, _stick2, 2);
                        }
                        else
                        {
                            _stick = Interlocked.Exchange(ref _stick2, _stick);
                            Array.Copy(_otherStick, _stick, 2);
                        }
                    }

                    lock (Other._otherStick)
                    {
                        // Write stick to linked joycon
                        Array.Copy(IsLeft ? _stick : _stick2, Other._otherStick, 2);
                    }
                }
            }

            // Set button states both for ViGEm
            lock (_buttons)
            {
                lock (_buttonsPrev)
                {
                    Array.Copy(_buttons, _buttonsPrev, _buttons.Length);
                }

                Array.Clear(_buttons);

                var reportOffset = IsLeft ? 2 : 0;

                _buttons[(int)Button.DpadDown] = (reportBuf[3 + reportOffset] & (IsLeft ? 0x01 : 0x04)) != 0;
                _buttons[(int)Button.DpadRight] = (reportBuf[3 + reportOffset] & (IsLeft ? 0x04 : 0x08)) != 0;
                _buttons[(int)Button.DpadUp] = (reportBuf[3 + reportOffset] & 0x02) != 0;
                _buttons[(int)Button.DpadLeft] = (reportBuf[3 + reportOffset] & (IsLeft ? 0x08 : 0x01)) != 0;
                _buttons[(int)Button.Home] = (reportBuf[4] & 0x10) != 0;
                _buttons[(int)Button.Capture] = (reportBuf[4] & 0x20) != 0;
                _buttons[(int)Button.Minus] = (reportBuf[4] & 0x01) != 0;
                _buttons[(int)Button.Plus] = (reportBuf[4] & 0x02) != 0;
                _buttons[(int)Button.Stick] = (reportBuf[4] & (IsLeft ? 0x08 : 0x04)) != 0;
                _buttons[(int)Button.Shoulder1] = (reportBuf[3 + reportOffset] & 0x40) != 0;
                _buttons[(int)Button.Shoulder2] = (reportBuf[3 + reportOffset] & 0x80) != 0;
                _buttons[(int)Button.SR] = (reportBuf[3 + reportOffset] & 0x10) != 0;
                _buttons[(int)Button.SL] = (reportBuf[3 + reportOffset] & 0x20) != 0;

                if (IsPro)
                {
                    reportOffset = !IsLeft ? 2 : 0;

                    _buttons[(int)Button.B] = (reportBuf[3 + reportOffset] & (!IsLeft ? 0x01 : 0x04)) != 0;
                    _buttons[(int)Button.A] = (reportBuf[3 + reportOffset] & (!IsLeft ? 0x04 : 0x08)) != 0;
                    _buttons[(int)Button.X] = (reportBuf[3 + reportOffset] & 0x02) != 0;
                    _buttons[(int)Button.Y] = (reportBuf[3 + reportOffset] & (!IsLeft ? 0x08 : 0x01)) != 0;

                    _buttons[(int)Button.Stick2] = (reportBuf[4] & (!IsLeft ? 0x08 : 0x04)) != 0;
                    _buttons[(int)Button.Shoulder21] = (reportBuf[3 + reportOffset] & 0x40) != 0;
                    _buttons[(int)Button.Shoulder22] = (reportBuf[3 + reportOffset] & 0x80) != 0;
                }

                if (Other != null && Other != this)
                {
                    _buttons[(int)Button.B] = Other._buttons[(int)Button.DpadDown];
                    _buttons[(int)Button.A] = Other._buttons[(int)Button.DpadRight];
                    _buttons[(int)Button.X] = Other._buttons[(int)Button.DpadUp];
                    _buttons[(int)Button.Y] = Other._buttons[(int)Button.DpadLeft];

                    _buttons[(int)Button.Stick2] = Other._buttons[(int)Button.Stick];
                    _buttons[(int)Button.Shoulder21] = Other._buttons[(int)Button.Shoulder1];
                    _buttons[(int)Button.Shoulder22] = Other._buttons[(int)Button.Shoulder2];

                    if (IsLeft)
                    {
                        _buttons[(int)Button.Home] = Other._buttons[(int)Button.Home];
                        _buttons[(int)Button.Plus] = Other._buttons[(int)Button.Plus];
                    }
                    else
                    {
                        _buttons[(int)Button.Capture] = Other._buttons[(int)Button.Capture];
                        _buttons[(int)Button.Minus] = Other._buttons[(int)Button.Minus];
                    }
                }

                var timestamp = Stopwatch.GetTimestamp();

                lock (_buttonsUp)
                {
                    lock (_buttonsDown)
                    {
                        var changed = false;
                        for (var i = 0; i < _buttons.Length; ++i)
                        {
                            _buttonsUp[i] = _buttonsPrev[i] & !_buttons[i];
                            _buttonsDown[i] = !_buttonsPrev[i] & _buttons[i];
                            if (_buttonsPrev[i] != _buttons[i])
                            {
                                _buttonsDownTimestamp[i] = _buttons[i] ? timestamp : -1;
                            }

                            if (_buttonsUp[i] || _buttonsDown[i])
                            {
                                changed = true;
                            }
                        }

                        if (changed)
                        {
                            _timestampActivity = timestamp;
                        }
                    }
                }
            }

            return 0;
        }

        // Get Gyro/Accel data
        private void ExtractIMUValues(byte[] reportBuf, int n = 0)
        {
            if (IsSNES)
            {
                return;
            }

            _gyrR[0] = (short)(reportBuf[19 + n * 12] | (reportBuf[20 + n * 12] << 8));
            _gyrR[1] = (short)(reportBuf[21 + n * 12] | (reportBuf[22 + n * 12] << 8));
            _gyrR[2] = (short)(reportBuf[23 + n * 12] | (reportBuf[24 + n * 12] << 8));
            _accR[0] = (short)(reportBuf[13 + n * 12] | (reportBuf[14 + n * 12] << 8));
            _accR[1] = (short)(reportBuf[15 + n * 12] | (reportBuf[16 + n * 12] << 8));
            _accR[2] = (short)(reportBuf[17 + n * 12] | (reportBuf[18 + n * 12] << 8));

            var direction = IsLeft ? 1 : -1;

            if (_form.AllowCalibration)
            {
                _accG.X = (_accR[0] - _activeIMUData[3]) * (1.0f / (_accSensiti[0] - _accNeutral[0])) * 4.0f;
                _gyrG.X = (_gyrR[0] - _activeIMUData[0]) * (816.0f / (_gyrSensiti[0] - _activeIMUData[0]));

                _accG.Y = direction * (_accR[1] -_activeIMUData[4]) * (1.0f / (_accSensiti[1] - _accNeutral[1])) * 4.0f;
                _gyrG.Y = -direction * (_gyrR[1] - _activeIMUData[1]) * (816.0f / (_gyrSensiti[1] - _activeIMUData[1]));

                _accG.Z = direction * (_accR[2] - _activeIMUData[5]) * (1.0f / (_accSensiti[2] - _accNeutral[2])) * 4.0f;
                _gyrG.Z = -direction * (_gyrR[2] - _activeIMUData[2]) * (816.0f / (_gyrSensiti[2] - _activeIMUData[2]));

                if (_calibrateIMU)
                {
                    // We need to add the accelerometer offset from the origin position when it's on a flat surface
                    short[] accOffset;
                    if (IsPro)
                    {
                        accOffset = _accProHorOffset;
                    }
                    else if (IsLeft)
                    {
                        accOffset = _accLeftHorOffset;
                    }
                    else
                    {
                        accOffset = _accRightHorOffset;
                    }

                    var imuData = new IMUData(
                        _gyrR[0],
                        _gyrR[1],
                        _gyrR[2],
                        (short)(_accR[0] - accOffset[0]),
                        (short)(_accR[1] - accOffset[1]),
                        (short)(_accR[2] - accOffset[2])
                    );
                    CalibrationIMUDatas.Add(imuData);
                }
            }
            else
            {
                _accG.X = _accR[0] * (1.0f / (_accSensiti[0] - _accNeutral[0])) * 4.0f;
                _gyrG.X = (_gyrR[0] - _gyrNeutral[0]) * (816.0f / (_gyrSensiti[0] - _gyrNeutral[0]));

                _accG.Y = direction * _accR[1] * (1.0f / (_accSensiti[1] - _accNeutral[1])) * 4.0f;
                _gyrG.Y = -direction * (_gyrR[1] - _gyrNeutral[1]) * (816.0f / (_gyrSensiti[1] - _gyrNeutral[1]));

                _accG.Z = direction * _accR[2] * (1.0f / (_accSensiti[2] - _accNeutral[2])) * 4.0f;
                _gyrG.Z = -direction * (_gyrR[2] - _gyrNeutral[2]) * (816.0f / (_gyrSensiti[2] - _gyrNeutral[2]));
            }

            if (IsJoycon && Other == null)
            {
                // single joycon mode; Z do not swap, rest do
                if (IsLeft)
                {
                    _accG.X = -_accG.X;
                    _accG.Y = -_accG.Y;
                    _gyrG.X = -_gyrG.X;
                }
                else
                {
                    _gyrG.Y = -_gyrG.Y;
                }

                var temp = _accG.X;
                _accG.X = _accG.Y;
                _accG.Y = -temp;

                temp = _gyrG.X;
                _gyrG.X = _gyrG.Y;
                _gyrG.Y = temp;
            }

            // Update rotation Quaternion
            var degToRad = 0.0174533f;
            _AHRS.Update(
                _gyrG.X * degToRad,
                _gyrG.Y * degToRad,
                _gyrG.Z * degToRad,
                _accG.X,
                _accG.Y,
                _accG.Z
            );
        }

        public void Begin()
        {
            if (_pollThreadObj == null)
            {
                _pollThreadObj = new Thread(Poll)
                {
                    IsBackground = true
                };
                _pollThreadObj.Start();

                _form.AppendTextBox("Starting poll thread.");
            }
            else
            {
                _form.AppendTextBox("Poll cannot start.");
            }
        }

        private void CalculateStickCenter(ushort[] vals, ushort[] cal, ushort dz, float[] stick)
        {
            float dx = vals[0] - cal[2];
            float dy = vals[1] - cal[3];

            if (Math.Abs(dx * dx + dy * dy) < dz * dz)
            {
                stick[0] = 0;
                stick[1] = 0;
            }
            else
            {
                stick[0] = dx / (dx > 0 ? cal[0] : cal[4]);
                stick[1] = dy / (dy > 0 ? cal[1] : cal[5]);
            }
        }

        private static short CastStickValue(float stickValue)
        {
            return (short)Math.Max(
                short.MinValue,
                Math.Min(short.MaxValue, stickValue * (stickValue > 0 ? short.MaxValue : -short.MinValue))
            );
        }

        private static byte CastStickValueByte(float stickValue)
        {
            return (byte)Math.Max(byte.MinValue, Math.Min(byte.MaxValue, 127 - stickValue * byte.MaxValue));
        }

        public void SetRumble(float lowFreq, float highFreq, float amp)
        {
            if (State <= Status.Attached)
            {
                return;
            }

            _rumbleObj.Enqueue(lowFreq, highFreq, amp);
        }

        // Run from poll thread
        private void SendRumble(byte[] buf, byte[] data)
        {
            Array.Clear(buf);

            buf[0] = 0x10;
            buf[1] = _globalCount;
            if (_globalCount == 0xf)
            {
                _globalCount = 0;
            }
            else
            {
                ++_globalCount;
            }

            Array.Copy(data, 0, buf, 2, 8);
            PrintArray(buf, DebugType.Rumble, 10, format: "Rumble data sent: {0:S}");
            HIDApi.hid_write(_handle, buf, new UIntPtr(ReportLen));
        }

        private byte[] Subcommand(byte sc, byte[] bufParameters, uint len, bool print = true)
        {
            if (_handle == IntPtr.Zero)
            {
                return null;
            }

            var buf = new byte[ReportLen];

            Array.Copy(_defaultBuf, 0, buf, 2, 8);
            Array.Copy(bufParameters, 0, buf, 11, len);
            buf[10] = sc;
            buf[1] = _globalCount;
            buf[0] = 0x1;
            if (_globalCount == 0xf)
            {
                _globalCount = 0;
            }
            else
            {
                ++_globalCount;
            }

            if (print)
            {
                PrintArray(buf, DebugType.Comms, len, 11, $"Subcommand 0x{sc:X2} sent." + " Data: 0x{0:S}");
            }

            HIDApi.hid_write(_handle, buf, new UIntPtr(len + 11));

            ref var response = ref buf;
            var tries = 0;
            var length = 0;
            var responseFound = false;
            do
            {
                length = HIDApi.hid_read_timeout(
                    _handle,
                    response,
                    new UIntPtr(ReportLen),
                    100
                ); // don't set the timeout lower than 100 or might not always work
                responseFound = length >= 20 && response[0] == 0x21 && response[14] == sc;
                tries++;
            } while (tries < 10 && !responseFound);

            if (!responseFound)
            {
                DebugPrint("No response.", DebugType.Comms);
                return null;
            }

            if (print)
            {
                PrintArray(
                    response,
                    DebugType.Comms,
                    (uint)length - 1,
                    1,
                    $"Response ID 0x{response[0]:X2}." + " Data: 0x{0:S}"
                );
            }

            return response;
        }

        private bool DumpCalibrationData()
        {
            if (IsSNES || IsThirdParty)
            {
                // Use default joycon values for sensors
                Array.Fill(_accSensiti, (short)16384);
                Array.Fill(_accNeutral, (short)0);
                Array.Fill(_gyrSensiti, (short)13371);
                Array.Fill(_gyrNeutral, (short)0);

                // Default stick calibration
                Array.Fill(_stickCal, (ushort)2048);
                Array.Fill(_stick2Cal, (ushort)2048);

                _deadzone = CalculateDeadzone(_stickCal, DefaultDeadzone);
                _deadzone2 = CalculateDeadzone(_stick2Cal, DefaultDeadzone);

                return true;
            }

            var ok = true;

            // get user calibration data if possible

            // Sticks axis
            {
                var userStickData = ReadSPICheck(0x80, 0x10, 0x16, ref ok);
                var factoryStickData = ReadSPICheck(0x60, 0x3D, 0x12, ref ok);

                var stick1Data = new ReadOnlySpan<byte>(userStickData, IsLeft ? 2 : 13, 9);
                var stick1Name = IsLeft ? "left" : "right";

                if (ok)
                {
                    if (userStickData[IsLeft ? 0 : 11] == 0xB2 && userStickData[IsLeft ? 1 : 12] == 0xA1)
                    {
                        _form.AppendTextBox($"Using user {stick1Name} stick calibration data.");
                    }
                    else
                    {
                        stick1Data = new ReadOnlySpan<byte>(factoryStickData, IsLeft ? 0 : 9, 9);

                        _form.AppendTextBox($"Using factory {stick1Name} stick calibration data.");
                    }
                }

                _stickCal[IsLeft ? 0 : 2] = (ushort)(((stick1Data[1] << 8) & 0xF00) | stick1Data[0]); // X Axis Max above center
                _stickCal[IsLeft ? 1 : 3] = (ushort)((stick1Data[2] << 4) | (stick1Data[1] >> 4)); // Y Axis Max above center
                _stickCal[IsLeft ? 2 : 4] = (ushort)(((stick1Data[4] << 8) & 0xF00) | stick1Data[3]); // X Axis Center
                _stickCal[IsLeft ? 3 : 5] = (ushort)((stick1Data[5] << 4) | (stick1Data[4] >> 4)); // Y Axis Center
                _stickCal[IsLeft ? 4 : 0] = (ushort)(((stick1Data[7] << 8) & 0xF00) | stick1Data[6]); // X Axis Min below center
                _stickCal[IsLeft ? 5 : 1] = (ushort)((stick1Data[8] << 4) | (stick1Data[7] >> 4)); // Y Axis Min below center

                PrintArray(_stickCal, len: 6, start: 0, format: $"{stick1Name} stick 1 calibration data: {{0:S}}");

                if (IsPro)
                {
                    var stick2Data = new ReadOnlySpan<byte>(userStickData, !IsLeft ? 2 : 13, 9);
                    var stick2Name = !IsLeft ? "left" : "right";

                    if (ok)
                    {
                        if (userStickData[!IsLeft ? 0 : 11] == 0xB2 && userStickData[!IsLeft ? 1 : 12] == 0xA1)
                        {
                            _form.AppendTextBox($"Using user {stick2Name} stick calibration data.");
                        }
                        else
                        {
                            stick2Data = new ReadOnlySpan<byte>(factoryStickData, !IsLeft ? 0 : 9, 9);

                            _form.AppendTextBox($"Using factory {stick2Name} stick calibration data.");
                        }
                    }

                    _stick2Cal[!IsLeft ? 0 : 2] = (ushort)(((stick2Data[1] << 8) & 0xF00) | stick2Data[0]); // X Axis Max above center
                    _stick2Cal[!IsLeft ? 1 : 3] = (ushort)((stick2Data[2] << 4) | (stick2Data[1] >> 4)); // Y Axis Max above center
                    _stick2Cal[!IsLeft ? 2 : 4] = (ushort)(((stick2Data[4] << 8) & 0xF00) | stick2Data[3]); // X Axis Center
                    _stick2Cal[!IsLeft ? 3 : 5] = (ushort)((stick2Data[5] << 4) | (stick2Data[4] >> 4)); // Y Axis Center
                    _stick2Cal[!IsLeft ? 4 : 0] = (ushort)(((stick2Data[7] << 8) & 0xF00) | stick2Data[6]); // X Axis Min below center
                    _stick2Cal[!IsLeft ? 5 : 1] = (ushort)((stick2Data[8] << 4) | (stick2Data[7] >> 4)); // Y Axis Min below center

                    PrintArray(_stick2Cal, len: 6, start: 0, format: $"{stick2Name} stick calibration data: {{0:S}}");
                }
            }

            // Sticks deadzones
            {
                var factoryDeadzoneData = ReadSPICheck(0x60, IsLeft ? (byte)0x86 : (byte)0x98, 5, ref ok);
                _deadzone = (ushort)(((factoryDeadzoneData[4] << 8) & 0xF00) | factoryDeadzoneData[3]);

                if (IsPro)
                {
                    var factoryDeadzone2Data = ReadSPICheck(0x60, !IsLeft ? (byte)0x86 : (byte)0x98, 5, ref ok);
                    _deadzone2 = (ushort)(((factoryDeadzone2Data[4] << 8) & 0xF00) | factoryDeadzone2Data[3]);
                }
            }

            // Gyro and accelerometer
            {
                var userSensorData = ReadSPICheck(0x80, 0x26, 0x1A, ref ok);
                ReadOnlySpan<byte> sensorData = new ReadOnlySpan<byte>(userSensorData, 2, 24);

                if (ok)
                {
                    if (userSensorData[0] == 0xB2 && userSensorData[1] == 0xA1)
                    {
                        _form.AppendTextBox($"Using user sensors calibration data.");
                    }
                    else
                    {
                        var factorySensorData = ReadSPICheck(0x60, 0x20, 0x18, ref ok);
                        sensorData = new ReadOnlySpan<byte>(factorySensorData, 0, 24);

                        _form.AppendTextBox($"Using factory sensors calibration data.");
                    }
                }

                _accNeutral[0] = (short)(sensorData[0] | (sensorData[1] << 8));
                _accNeutral[1] = (short)(sensorData[2] | (sensorData[3] << 8));
                _accNeutral[2] = (short)(sensorData[4] | (sensorData[5] << 8));

                _accSensiti[0] = (short)(sensorData[6] | (sensorData[7] << 8));
                _accSensiti[1] = (short)(sensorData[8] | (sensorData[9] << 8));
                _accSensiti[2] = (short)(sensorData[10] | (sensorData[11] << 8));

                _gyrNeutral[0] = (short)(sensorData[12] | (sensorData[13] << 8));
                _gyrNeutral[1] = (short)(sensorData[14] | (sensorData[15] << 8));
                _gyrNeutral[2] = (short)(sensorData[16] | (sensorData[17] << 8));

                _gyrSensiti[0] = (short)(sensorData[18] | (sensorData[19] << 8));
                _gyrSensiti[1] = (short)(sensorData[20] | (sensorData[21] << 8));
                _gyrSensiti[2] = (short)(sensorData[22] | (sensorData[23] << 8));

                bool noCalibration = false;

                if (_accNeutral[0] == -1 || _accNeutral[1] == -1 || _accNeutral[2] == -1)
                {
                    Array.Fill(_accNeutral, (short) 0);
                    noCalibration = true;
                }

                if (_accSensiti[0] == -1 || _accSensiti[1] == -1 || _accSensiti[2] == -1)
                {
                    // Default accelerometer sensitivity for joycons
                    Array.Fill(_accSensiti, (short)16384);
                    noCalibration = true;
                }

                if (_gyrNeutral[0] == -1 || _gyrNeutral[1] == -1 || _gyrNeutral[2] == -1)
                {
                    Array.Fill(_gyrNeutral, (short)0);
                    noCalibration = true;
                }

                if (_gyrSensiti[0] == -1 || _gyrSensiti[1] == -1 || _gyrSensiti[2] == -1)
                {
                    // Default gyroscope sensitivity for joycons
                    Array.Fill(_gyrSensiti, (short)13371);
                    noCalibration = true;
                }

                if (noCalibration)
                {
                    _form.AppendTextBox($"Some sensor calibrations datas are missing, fallback to default ones.");
                }

                PrintArray(_gyrNeutral, len: 3, d: DebugType.IMU, format: "Gyro neutral position: {0:S}");
            }

            if (!ok)
            {
                _form.AppendTextBox("Error while reading calibration data.");
            }

            return ok;
        }

        private int ReadUSBCheck(byte[] data, byte command)
        {
            int length;
            bool responseFound;
            var tries = 0;
            do
            {
                length = HIDApi.hid_read_timeout(_handle, data, new UIntPtr(ReportLen), 100);
                responseFound = length > 1 && data[0] == 0x81 && data[1] == command;
                ++tries;
            } while (tries < 10 && !responseFound);

            if (!responseFound)
            {
                length = 0;
            }

            return length;
        }

        private byte[] ReadSPICheck(byte addr1, byte addr2, uint len, ref bool ok, bool print = false)
        {
            var readBuf = new byte[len];
            if (!ok)
            {
                return readBuf;
            }

            byte[] bufSubcommand = { addr2, addr1, 0x00, 0x00, (byte)len };
            byte[] buf = null;

            ok = false;
            for (var i = 0; i < 5; ++i)
            {
                buf = Subcommand(0x10, bufSubcommand, 5, false);
                if (buf != null && buf[15] == addr2 && buf[16] == addr1)
                {
                    ok = true;
                    break;
                }
            }

            if (ok)
            {
                Array.Copy(buf, 20, readBuf, 0, len);
                if (print)
                {
                    PrintArray(readBuf, DebugType.Comms, len);
                }
            }
            else
            {
                _form.AppendTextBox("ReadSPI error");
            }

            return readBuf;
        }

        private void PrintArray<T>(
            T[] arr,
            DebugType d = DebugType.None,
            uint len = 0,
            uint start = 0,
            string format = "{0:S}"
        )
        {
            if (d != _debugType && _debugType != DebugType.All)
            {
                return;
            }

            if (len == 0)
            {
                len = (uint)arr.Length;
            }

            var tostr = "";
            for (var i = 0; i < len; ++i)
            {
                tostr += string.Format(
                    arr[0] is byte ? "{0:X2} " : arr[0] is float ? "{0:F} " : "{0:D} ",
                    arr[i + start]
                );
            }

            DebugPrint(string.Format(format, tostr), d);
        }

        public class StateChangedEventArgs : EventArgs
        {
            public Status State { get; }

            public StateChangedEventArgs(Status state)
            {
                State = state;
            }
        }

        private static OutputControllerXbox360InputState MapToXbox360Input(Joycon input)
        {
            var output = new OutputControllerXbox360InputState();

            var swapAB = input._swapAB;
            var swapXY = input._swapXY;

            var isPro = input.IsPro;
            var isLeft = input.IsLeft;
            var isSNES = input.IsSNES;
            var other = input.Other;
            var gyroAnalogSliders = input._gyroAnalogSliders;

            var buttons = input._buttonsRemapped;
            var stick = input._stick;
            var stick2 = input._stick2;
            var sliderVal = input._sliderVal;

            if (isPro)
            {
                output.A = buttons[(int)(!swapAB ? Button.B : Button.A)];
                output.B = buttons[(int)(!swapAB ? Button.A : Button.B)];
                output.Y = buttons[(int)(!swapXY ? Button.X : Button.Y)];
                output.X = buttons[(int)(!swapXY ? Button.Y : Button.X)];

                output.DpadUp = buttons[(int)Button.DpadUp];
                output.DpadDown = buttons[(int)Button.DpadDown];
                output.DpadLeft = buttons[(int)Button.DpadLeft];
                output.DpadRight = buttons[(int)Button.DpadRight];

                output.Back = buttons[(int)Button.Minus];
                output.Start = buttons[(int)Button.Plus];
                output.Guide = buttons[(int)Button.Home];

                output.ShoulderLeft = buttons[(int)Button.Shoulder1];
                output.ShoulderRight = buttons[(int)Button.Shoulder21];

                output.ThumbStickLeft = buttons[(int)Button.Stick];
                output.ThumbStickRight = buttons[(int)Button.Stick2];
            }
            else
            {
                // no need for && other != this
                if (other != null)
                {
                    output.A = !swapAB
                            ? buttons[(int)(isLeft ? Button.B : Button.DpadDown)]
                            : buttons[(int)(isLeft ? Button.A : Button.DpadRight)];
                    output.B = !swapAB
                            ? buttons[(int)(isLeft ? Button.A : Button.DpadRight)]
                            : buttons[(int)(isLeft ? Button.B : Button.DpadDown)];
                    output.X = !swapXY
                            ? buttons[(int)(isLeft ? Button.Y : Button.DpadLeft)]
                            : buttons[(int)(isLeft ? Button.X : Button.DpadUp)];
                    output.Y = !swapXY
                            ? buttons[(int)(isLeft ? Button.X : Button.DpadUp)]
                            : buttons[(int)(isLeft ? Button.Y : Button.DpadLeft)];

                    output.DpadUp = buttons[(int)(isLeft ? Button.DpadUp : Button.X)];
                    output.DpadDown = buttons[(int)(isLeft ? Button.DpadDown : Button.B)];
                    output.DpadLeft = buttons[(int)(isLeft ? Button.DpadLeft : Button.Y)];
                    output.DpadRight = buttons[(int)(isLeft ? Button.DpadRight : Button.A)];

                    output.Back = buttons[(int)Button.Minus];
                    output.Start = buttons[(int)Button.Plus];
                    output.Guide = buttons[(int)Button.Home];

                    output.ShoulderLeft = buttons[(int)(isLeft ? Button.Shoulder1 : Button.Shoulder21)];
                    output.ShoulderRight = buttons[(int)(isLeft ? Button.Shoulder21 : Button.Shoulder1)];

                    output.ThumbStickLeft = buttons[(int)(isLeft ? Button.Stick : Button.Stick2)];
                    output.ThumbStickRight = buttons[(int)(isLeft ? Button.Stick2 : Button.Stick)];
                }
                else
                {
                    // single joycon mode
                    output.A = !swapAB
                            ? buttons[(int)(isLeft ? Button.DpadLeft : Button.DpadRight)]
                            : buttons[(int)(isLeft ? Button.DpadDown : Button.DpadUp)];
                    output.B = !swapAB
                            ? buttons[(int)(isLeft ? Button.DpadDown : Button.DpadUp)]
                            : buttons[(int)(isLeft ? Button.DpadLeft : Button.DpadRight)];
                    output.X = !swapXY
                            ? buttons[(int)(isLeft ? Button.DpadUp : Button.DpadDown)]
                            : buttons[(int)(isLeft ? Button.DpadRight : Button.DpadLeft)];
                    output.Y = !swapXY
                            ? buttons[(int)(isLeft ? Button.DpadRight : Button.DpadLeft)]
                            : buttons[(int)(isLeft ? Button.DpadUp : Button.DpadDown)];

                    output.Back = buttons[(int)Button.Minus] | buttons[(int)Button.Home];
                    output.Start = buttons[(int)Button.Plus] | buttons[(int)Button.Capture];

                    output.ShoulderLeft = buttons[(int)Button.SL];
                    output.ShoulderRight = buttons[(int)Button.SR];

                    output.ThumbStickLeft = buttons[(int)Button.Stick];
                }
            }

            if (!isSNES)
            {
                if (other != null || isPro)
                {
                    // no need for && other != this
                    output.AxisLeftX = CastStickValue(other == input && !isLeft ? stick2[0] : stick[0]);
                    output.AxisLeftY = CastStickValue(other == input && !isLeft ? stick2[1] : stick[1]);

                    output.AxisRightX = CastStickValue(other == input && !isLeft ? stick[0] : stick2[0]);
                    output.AxisRightY = CastStickValue(other == input && !isLeft ? stick[1] : stick2[1]);
                }
                else
                {
                    // single joycon mode
                    output.AxisLeftY = CastStickValue((isLeft ? 1 : -1) * stick[0]);
                    output.AxisLeftX = CastStickValue((isLeft ? -1 : 1) * stick[1]);
                }
            }

            if (isPro || other != null)
            {
                var lval = gyroAnalogSliders ? sliderVal[0] : byte.MaxValue;
                var rval = gyroAnalogSliders ? sliderVal[1] : byte.MaxValue;
                output.TriggerLeft = (byte)(buttons[(int)(isLeft ? Button.Shoulder2 : Button.Shoulder22)] ? lval : 0);
                output.TriggerRight = (byte)(buttons[(int)(isLeft ? Button.Shoulder22 : Button.Shoulder2)] ? rval : 0);
            }
            else
            {
                output.TriggerLeft = (byte)(buttons[(int)(isLeft ? Button.Shoulder2 : Button.Shoulder1)] ? byte.MaxValue : 0);
                output.TriggerRight = (byte)(buttons[(int)(isLeft ? Button.Shoulder1 : Button.Shoulder2)] ? byte.MaxValue : 0);
            }

            return output;
        }

        public static OutputControllerDualShock4InputState MapToDualShock4Input(Joycon input)
        {
            var output = new OutputControllerDualShock4InputState();

            var swapAB = input._swapAB;
            var swapXY = input._swapXY;

            var isPro = input.IsPro;
            var isLeft = input.IsLeft;
            var isSNES = input.IsSNES;
            var other = input.Other;
            var gyroAnalogSliders = input._gyroAnalogSliders;

            var buttons = input._buttonsRemapped;
            var stick = input._stick;
            var stick2 = input._stick2;
            var sliderVal = input._sliderVal;

            if (isPro)
            {
                output.Cross = buttons[(int)(!swapAB ? Button.B : Button.A)];
                output.Circle = buttons[(int)(!swapAB ? Button.A : Button.B)];
                output.Triangle = buttons[(int)(!swapXY ? Button.X : Button.Y)];
                output.Square = buttons[(int)(!swapXY ? Button.Y : Button.X)];


                if (buttons[(int)Button.DpadUp])
                {
                    if (buttons[(int)Button.DpadLeft])
                    {
                        output.DPad = DpadDirection.Northwest;
                    }
                    else if (buttons[(int)Button.DpadRight])
                    {
                        output.DPad = DpadDirection.Northeast;
                    }
                    else
                    {
                        output.DPad = DpadDirection.North;
                    }
                }
                else if (buttons[(int)Button.DpadDown])
                {
                    if (buttons[(int)Button.DpadLeft])
                    {
                        output.DPad = DpadDirection.Southwest;
                    }
                    else if (buttons[(int)Button.DpadRight])
                    {
                        output.DPad = DpadDirection.Southeast;
                    }
                    else
                    {
                        output.DPad = DpadDirection.South;
                    }
                }
                else if (buttons[(int)Button.DpadLeft])
                {
                    output.DPad = DpadDirection.West;
                }
                else if (buttons[(int)Button.DpadRight])
                {
                    output.DPad = DpadDirection.East;
                }

                output.Share = buttons[(int)Button.Capture];
                output.Options = buttons[(int)Button.Plus];
                output.Ps = buttons[(int)Button.Home];
                output.Touchpad = buttons[(int)Button.Minus];
                output.ShoulderLeft = buttons[(int)Button.Shoulder1];
                output.ShoulderRight = buttons[(int)Button.Shoulder21];
                output.ThumbLeft = buttons[(int)Button.Stick];
                output.ThumbRight = buttons[(int)Button.Stick2];
            }
            else
            {
                if (other != null)
                {
                    // no need for && other != this
                    output.Cross = !swapAB
                            ? buttons[(int)(isLeft ? Button.B : Button.DpadDown)]
                            : buttons[(int)(isLeft ? Button.A : Button.DpadRight)];
                    output.Circle = swapAB
                            ? buttons[(int)(isLeft ? Button.B : Button.DpadDown)]
                            : buttons[(int)(isLeft ? Button.A : Button.DpadRight)];
                    output.Triangle = !swapXY
                            ? buttons[(int)(isLeft ? Button.X : Button.DpadUp)]
                            : buttons[(int)(isLeft ? Button.Y : Button.DpadLeft)];
                    output.Square = swapXY
                            ? buttons[(int)(isLeft ? Button.X : Button.DpadUp)]
                            : buttons[(int)(isLeft ? Button.Y : Button.DpadLeft)];

                    if (buttons[(int)(isLeft ? Button.DpadUp : Button.X)])
                    {
                        if (buttons[(int)(isLeft ? Button.DpadLeft : Button.Y)])
                        {
                            output.DPad = DpadDirection.Northwest;
                        }
                        else if (buttons[(int)(isLeft ? Button.DpadRight : Button.A)])
                        {
                            output.DPad = DpadDirection.Northeast;
                        }
                        else
                        {
                            output.DPad = DpadDirection.North;
                        }
                    }
                    else if (buttons[(int)(isLeft ? Button.DpadDown : Button.B)])
                    {
                        if (buttons[(int)(isLeft ? Button.DpadLeft : Button.Y)])
                        {
                            output.DPad = DpadDirection.Southwest;
                        }
                        else if (buttons[(int)(isLeft ? Button.DpadRight : Button.A)])
                        {
                            output.DPad = DpadDirection.Southeast;
                        }
                        else
                        {
                            output.DPad = DpadDirection.South;
                        }
                    }
                    else if (buttons[(int)(isLeft ? Button.DpadLeft : Button.Y)])
                    {
                        output.DPad = DpadDirection.West;
                    }
                    else if (buttons[(int)(isLeft ? Button.DpadRight : Button.A)])
                    {
                        output.DPad = DpadDirection.East;
                    }

                    output.Share = buttons[(int)Button.Capture];
                    output.Options = buttons[(int)Button.Plus];
                    output.Ps = buttons[(int)Button.Home];
                    output.Touchpad = buttons[(int)Button.Minus];
                    output.ShoulderLeft = buttons[(int)(isLeft ? Button.Shoulder1 : Button.Shoulder21)];
                    output.ShoulderRight = buttons[(int)(isLeft ? Button.Shoulder21 : Button.Shoulder1)];
                    output.ThumbLeft = buttons[(int)(isLeft ? Button.Stick : Button.Stick2)];
                    output.ThumbRight = buttons[(int)(isLeft ? Button.Stick2 : Button.Stick)];
                }
                else
                {
                    // single joycon mode
                    output.Cross = !swapAB
                            ? buttons[(int)(isLeft ? Button.DpadLeft : Button.DpadRight)]
                            : buttons[(int)(isLeft ? Button.DpadDown : Button.DpadUp)];
                    output.Circle = swapAB
                            ? buttons[(int)(isLeft ? Button.DpadLeft : Button.DpadRight)]
                            : buttons[(int)(isLeft ? Button.DpadDown : Button.DpadUp)];
                    output.Triangle = !swapXY
                            ? buttons[(int)(isLeft ? Button.DpadRight : Button.DpadLeft)]
                            : buttons[(int)(isLeft ? Button.DpadUp : Button.DpadDown)];
                    output.Square = swapXY
                            ? buttons[(int)(isLeft ? Button.DpadRight : Button.DpadLeft)]
                            : buttons[(int)(isLeft ? Button.DpadUp : Button.DpadDown)];

                    output.Ps = buttons[(int)Button.Minus] | buttons[(int)Button.Home];
                    output.Options = buttons[(int)Button.Plus] | buttons[(int)Button.Capture];

                    output.ShoulderLeft = buttons[(int)Button.SL];
                    output.ShoulderRight = buttons[(int)Button.SR];

                    output.ThumbLeft = buttons[(int)Button.Stick];
                }
            }

            if (!isSNES)
            {
                if (other != null || isPro)
                {
                    // no need for && other != this
                    output.ThumbLeftX = CastStickValueByte(other == input && !isLeft ? -stick2[0] : -stick[0]);
                    output.ThumbLeftY = CastStickValueByte(other == input && !isLeft ? stick2[1] : stick[1]);
                    output.ThumbRightX = CastStickValueByte(other == input && !isLeft ? -stick[0] : -stick2[0]);
                    output.ThumbRightY = CastStickValueByte(other == input && !isLeft ? stick[1] : stick2[1]);
                }
                else
                {
                    // single joycon mode
                    output.ThumbLeftY = CastStickValueByte((isLeft ? 1 : -1) * stick[0]);
                    output.ThumbLeftX = CastStickValueByte((isLeft ? 1 : -1) * stick[1]);
                }
            }

            if (isPro || other != null)
            {
                var lval = gyroAnalogSliders ? sliderVal[0] : byte.MaxValue;
                var rval = gyroAnalogSliders ? sliderVal[1] : byte.MaxValue;
                output.TriggerLeftValue = (byte)(buttons[(int)(isLeft ? Button.Shoulder2 : Button.Shoulder22)] ? lval : 0);
                output.TriggerRightValue = (byte)(buttons[(int)(isLeft ? Button.Shoulder22 : Button.Shoulder2)] ? rval : 0);
            }
            else
            {
                output.TriggerLeftValue = (byte)(buttons[(int)(isLeft ? Button.Shoulder2 : Button.Shoulder1)] ? byte.MaxValue : 0);
                output.TriggerRightValue = (byte)(buttons[(int)(isLeft ? Button.Shoulder1 : Button.Shoulder2)] ? byte.MaxValue : 0);
            }

            // Output digital L2 / R2 in addition to analog L2 / R2
            output.TriggerLeft = output.TriggerLeftValue > 0;
            output.TriggerRight = output.TriggerRightValue > 0;

            return output;
        }

        public static string GetControllerName(ControllerType type)
        {
            return type switch
            {
                ControllerType.JoyconLeft  => "Left joycon",
                ControllerType.JoyconRight => "Right joycon",
                ControllerType.Pro         => "Pro controller",
                ControllerType.SNES        => "SNES controller",
                _                          => "Controller"
            };
        }

        public string GetControllerName()
        {
            return GetControllerName(Type);
        }

        public void StartSticksCalibration()
        {
            CalibrationStickDatas.Clear();
            _calibrateSticks = true;
        }

        public void StopSticksCalibration(bool clean = false)
        {
            _calibrateSticks = false;

            if (clean)
            {
                CalibrationStickDatas.Clear();
            }
        }

        public void StartIMUCalibration()
        {
            CalibrationIMUDatas.Clear();
            _calibrateIMU = true;
        }

        public void StopIMUCalibration(bool clean = false)
        {
            _calibrateIMU = false;

            if (clean)
            {
                CalibrationIMUDatas.Clear();
            }
        }

        private struct Rumble
        {
            private readonly Queue<float[]> _queue;
            private SpinLock _queueLock;

            public void Enqueue(float lowFreq, float highFreq, float amplitude)
            {
                float[] rumbleQueue = { lowFreq, highFreq, amplitude };
                // Keep a queue of 15 items, discard oldest item if queue is full.
                var lockTaken = false;
                try
                {
                    _queueLock.Enter(ref lockTaken);
                    if (_queue.Count > 15)
                    {
                        _queue.Dequeue();
                    }

                    _queue.Enqueue(rumbleQueue);
                }
                finally
                {
                    if (lockTaken)
                    {
                        _queueLock.Exit();
                    }
                }
            }

            public Rumble(float[] rumbleInfo)
            {
                _queue = new Queue<float[]>();
                _queueLock = new SpinLock();
                _queue.Enqueue(rumbleInfo);
            }

            private float Clamp(float x, float min, float max)
            {
                if (x < min)
                {
                    return min;
                }

                if (x > max)
                {
                    return max;
                }

                return x;
            }

            private byte EncodeAmp(float amp)
            {
                byte enAmp;

                if (amp == 0)
                {
                    enAmp = 0;
                }
                else if (amp < 0.117)
                {
                    enAmp = (byte)((Math.Log(amp * 1000, 2) * 32 - 0x60) / (5 - Math.Pow(amp, 2)) - 1);
                }
                else if (amp < 0.23)
                {
                    enAmp = (byte)(Math.Log(amp * 1000, 2) * 32 - 0x60 - 0x5c);
                }
                else
                {
                    enAmp = (byte)((Math.Log(amp * 1000, 2) * 32 - 0x60) * 2 - 0xf6);
                }

                return enAmp;
            }

            public byte[] GetData()
            {
                float[] queuedData = null;
                var lockTaken = false;
                try
                {
                    _queueLock.Enter(ref lockTaken);
                    if (_queue.Count > 0)
                    {
                        queuedData = _queue.Dequeue();
                    }
                }
                finally
                {
                    if (lockTaken)
                    {
                        _queueLock.Exit();
                    }
                }

                if (queuedData == null)
                {
                    return null;
                }

                var rumbleData = new byte[8];

                if (queuedData[2] == 0.0f)
                {
                    rumbleData[0] = 0x0;
                    rumbleData[1] = 0x1;
                    rumbleData[2] = 0x40;
                    rumbleData[3] = 0x40;
                }
                else
                {
                    queuedData[0] = Clamp(queuedData[0], 40.875885f, 626.286133f);
                    queuedData[1] = Clamp(queuedData[1], 81.75177f, 1252.572266f);

                    queuedData[2] = Clamp(queuedData[2], 0.0f, 1.0f);

                    var hf = (ushort)((Math.Round(32f * Math.Log(queuedData[1] * 0.1f, 2)) - 0x60) * 4);
                    var lf = (byte)(Math.Round(32f * Math.Log(queuedData[0] * 0.1f, 2)) - 0x40);
                    var hfAmp = EncodeAmp(queuedData[2]);

                    var lfAmp = (ushort)(Math.Round((double)hfAmp) * .5);
                    var parity = (byte)(lfAmp % 2);
                    if (parity > 0)
                    {
                        --lfAmp;
                    }

                    lfAmp = (ushort)(lfAmp >> 1);
                    lfAmp += 0x40;
                    if (parity > 0)
                    {
                        lfAmp |= 0x8000;
                    }

                    hfAmp = (byte)(hfAmp - hfAmp % 2); // make even at all times to prevent weird hum
                    rumbleData[0] = (byte)(hf & 0xff);
                    rumbleData[1] = (byte)(((hf >> 8) & 0xff) + hfAmp);
                    rumbleData[2] = (byte)(((lfAmp >> 8) & 0xff) + lf);
                    rumbleData[3] = (byte)(lfAmp & 0xff);
                }

                for (var i = 0; i < 4; ++i)
                {
                    rumbleData[4 + i] = rumbleData[i];
                }

                return rumbleData;
            }
        }

        public struct IMUData
        {
            public short Xg;
            public short Yg;
            public short Zg;
            public short Xa;
            public short Ya;
            public short Za;

            public IMUData(short xg, short yg, short zg, short xa, short ya, short za)
            {
                Xg = xg;
                Yg = yg;
                Zg = zg;
                Xa = xa;
                Ya = ya;
                Za = za;
            }
        }

        public struct SticksData
        {
            public ushort Xs1;
            public ushort Ys1;
            public ushort Xs2;
            public ushort Ys2;

            public SticksData(ushort x1, ushort y1, ushort x2, ushort y2)
            {
                Xs1 = x1;
                Ys1 = y1;
                Xs2 = x2;
                Ys2 = y2;
            }
        }

        class RollingAverage
        {
            private Queue<int> _samples;
            private int _size;
            private long _sum;

            public RollingAverage(int size)
            {
                _size = size;
                _samples = new Queue<int>(size);
                _sum = 0;
            }

            public void AddValue(int value)
            {
                if (_samples.Count >= _size)
                {
                    int sample = _samples.Dequeue();
                    _sum -= sample;
                }

                _samples.Enqueue(value);
                _sum += value;
            }

            public void Clear()
            {
                _samples.Clear();
                _sum = 0;
            }

            public bool Empty()
            {
                return _samples.Count == 0;
            }

            public float GetAverage()
            {
                return Empty() ? 0 : _sum / _samples.Count;
            }
        }
    }
}
