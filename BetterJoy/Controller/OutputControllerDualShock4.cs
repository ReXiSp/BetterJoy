using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;

namespace BetterJoy.Controller
{
    public enum DpadDirection
    {
        None,
        Northwest,
        West,
        Southwest,
        South,
        Southeast,
        East,
        Northeast,
        North
    }

    public struct OutputControllerDualShock4InputState
    {
        public bool Triangle;
        public bool Circle;
        public bool Cross;
        public bool Square;

        public bool TriggerLeft;
        public bool TriggerRight;

        public bool ShoulderLeft;
        public bool ShoulderRight;

        public bool Options;
        public bool Share;
        public bool Ps;
        public bool Touchpad;

        public bool ThumbLeft;
        public bool ThumbRight;

        public DpadDirection DPad;

        public byte ThumbLeftX;
        public byte ThumbLeftY;
        public byte ThumbRightX;
        public byte ThumbRightY;

        public byte TriggerLeftValue;
        public byte TriggerRightValue;

        public Vector3 accel;
        public Vector3 gyro;

        public bool IsEqual(OutputControllerDualShock4InputState other)
        {
            var buttons = Triangle == other.Triangle
                          && Circle == other.Circle
                          && Cross == other.Cross
                          && Square == other.Square
                          && TriggerLeft == other.TriggerLeft
                          && TriggerRight == other.TriggerRight
                          && ShoulderLeft == other.ShoulderLeft
                          && ShoulderRight == other.ShoulderRight
                          && Options == other.Options
                          && Share == other.Share
                          && Ps == other.Ps
                          && Touchpad == other.Touchpad
                          && ThumbLeft == other.ThumbLeft
                          && ThumbRight == other.ThumbRight
                          && DPad == other.DPad;

            var axis = ThumbLeftX == other.ThumbLeftX
                       && ThumbLeftY == other.ThumbLeftY
                       && ThumbRightX == other.ThumbRightX
                       && ThumbRightY == other.ThumbRightY;

            var triggers = TriggerLeftValue == other.TriggerLeftValue
                           && TriggerRightValue == other.TriggerRightValue;

            var gyros = gyro == other.gyro;
            var accels = accel == other.accel;

            //Force Because Gyro | Accel
            //return false;
            return buttons && axis && triggers && gyros && accels;
        }
    }

    /// <summary>
    /// Used to set data for DS4 Extended output report. StructLayout
    /// will be used to align data for a raw byte array of 63 bytes.
    /// ViGEmBus will place report ID byte into the output so this data
    /// will technically start with byte 1 of the final output report
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 63)]
    unsafe struct DS4_REPORT_EX
    {
        [FieldOffset(0)]
        public byte bThumbLX;
        [FieldOffset(1)]
        public byte bThumbLY;
        [FieldOffset(2)]
        public byte bThumbRX;
        [FieldOffset(3)]
        public byte bThumbRY;
        [FieldOffset(4)]
        public ushort wButtons;
        [FieldOffset(6)]
        public byte bSpecial;
        [FieldOffset(7)]
        public byte bTriggerL;
        [FieldOffset(8)]
        public byte bTriggerR;
        [FieldOffset(9)]
        public ushort wTimestamp;
        [FieldOffset(11)]
        public byte bBatteryLvl;
        [FieldOffset(12)]
        public short wGyroX;
        [FieldOffset(14)]
        public short wGyroY;
        [FieldOffset(16)]
        public short wGyroZ;
        [FieldOffset(18)]
        public short wAccelX;
        [FieldOffset(20)]
        public short wAccelY;
        [FieldOffset(22)]
        public short wAccelZ;
        [FieldOffset(24)]
        public fixed byte _bUnknown1[5];
        [FieldOffset(29)]
        public byte bBatteryLvlSpecial;
        [FieldOffset(30)]
        public fixed byte _bUnknown2[2];
        [FieldOffset(32)]
        public byte bTouchPacketsN;
    }

    public class OutputControllerDualShock4
    {
        public delegate void DualShock4FeedbackReceivedEventHandler(DualShock4FeedbackReceivedEventArgs e);

        private readonly IDualShock4Controller _controller;

        private byte[] rawOutReportEx = new byte[63];
        private DS4_REPORT_EX outDS4Report;

        private OutputControllerDualShock4InputState _currentState;

        public OutputControllerDualShock4()
        {
            _controller = Program.EmClient.CreateDualShock4Controller();
            Init();
        }

        public OutputControllerDualShock4(ushort vendorId, ushort productId)
        {
            _controller = Program.EmClient.CreateDualShock4Controller(vendorId, productId);
            Init();
        }

        public event DualShock4FeedbackReceivedEventHandler FeedbackReceived;

        private void Init()
        {
            outDS4Report = default(DS4_REPORT_EX);
            outDS4Report.wButtons &= unchecked((ushort)~0X0F);
            outDS4Report.wButtons |= 0x08;
            outDS4Report.bThumbLX = 0x80;
            outDS4Report.bThumbLY = 0x80;
            outDS4Report.bThumbRX = 0x80;
            outDS4Report.bThumbRY = 0x80;

            GCHandle h = GCHandle.Alloc(outDS4Report, GCHandleType.Pinned);
            Marshal.Copy(h.AddrOfPinnedObject(), rawOutReportEx, 0, 63);
            h.Free();

            //_controller.SubmitRawReport(rawOutReportEx);

            _controller.AutoSubmitReport = false;
            _controller.FeedbackReceived += FeedbackReceivedRcv;
        }

        private void FeedbackReceivedRcv(object sender, DualShock4FeedbackReceivedEventArgs e)
        {
            FeedbackReceived?.Invoke(e);
        }

        public void Connect()
        {
            _controller.Connect();
        }

        public void Disconnect()
        {
            _controller.Disconnect();
        }

        public bool UpdateInput(OutputControllerDualShock4InputState newState)
        {
            if (_currentState.IsEqual(newState))
            {
                return false;
            }

            DoUpdateInput(newState);

            return true;
        }

        private void DoUpdateInput(OutputControllerDualShock4InputState newState)
        {

            /*_controller.SetButtonState(DualShock4Button.Triangle, newState.Triangle);
            _controller.SetButtonState(DualShock4Button.Circle, newState.Circle);
            _controller.SetButtonState(DualShock4Button.Cross, newState.Cross);
            _controller.SetButtonState(DualShock4Button.Square, newState.Square);

            _controller.SetButtonState(DualShock4Button.ShoulderLeft, newState.ShoulderLeft);
            _controller.SetButtonState(DualShock4Button.ShoulderRight, newState.ShoulderRight);

            _controller.SetButtonState(DualShock4Button.TriggerLeft, newState.TriggerLeft);
            _controller.SetButtonState(DualShock4Button.TriggerRight, newState.TriggerRight);

            _controller.SetButtonState(DualShock4Button.ThumbLeft, newState.ThumbLeft);
            _controller.SetButtonState(DualShock4Button.ThumbRight, newState.ThumbRight);

            _controller.SetButtonState(DualShock4Button.Share, newState.Share);
            _controller.SetButtonState(DualShock4Button.Options, newState.Options);
            _controller.SetButtonState(DualShock4SpecialButton.Ps, newState.Ps);
            _controller.SetButtonState(DualShock4SpecialButton.Touchpad, newState.Touchpad);

            _controller.SetDPadDirection(MapDPadDirection(newState.DPad));

            _controller.SetAxisValue(DualShock4Axis.LeftThumbX, newState.ThumbLeftX);
            _controller.SetAxisValue(DualShock4Axis.LeftThumbY, newState.ThumbLeftY);
            _controller.SetAxisValue(DualShock4Axis.RightThumbX, newState.ThumbRightX);
            _controller.SetAxisValue(DualShock4Axis.RightThumbY, newState.ThumbRightY);

            _controller.SetSliderValue(DualShock4Slider.LeftTrigger, newState.TriggerLeftValue);
            _controller.SetSliderValue(DualShock4Slider.RightTrigger, newState.TriggerRightValue);

            _controller.SubmitReport();*/

            ushort tempButtons = 0;
            DualShock4DPadDirection tempDPad = DualShock4DPadDirection.None;
            ushort tempSpecial = 0;

            unchecked
            {
                if (newState.Share) tempButtons |= DualShock4Button.Share.Value;
                if (newState.ThumbLeft) tempButtons |= DualShock4Button.ThumbLeft.Value;
                if (newState.ThumbRight) tempButtons |= DualShock4Button.ThumbRight.Value;
                if (newState.Options) tempButtons |= DualShock4Button.Options.Value;

                tempDPad = MapDPadDirection(newState.DPad);

                /*if (state.DpadUp) tempDPad = (state.DpadRight) ? DualShock4DPadValues.Northeast : DualShock4DPadValues.North;
                if (state.DpadRight) tempDPad = (state.DpadDown) ? DualShock4DPadValues.Southeast : DualShock4DPadValues.East;
                if (state.DpadDown) tempDPad = (state.DpadLeft) ? DualShock4DPadValues.Southwest : DualShock4DPadValues.South;
                if (state.DpadLeft) tempDPad = (state.DpadUp) ? DualShock4DPadValues.Northwest : DualShock4DPadValues.West;
                */

                if (newState.ShoulderLeft) tempButtons |= DualShock4Button.ShoulderLeft.Value;
                if (newState.ShoulderRight) tempButtons |= DualShock4Button.ShoulderRight.Value;
                if (newState.TriggerLeft) tempButtons |= DualShock4Button.TriggerLeft.Value;
                if (newState.TriggerRight) tempButtons |= DualShock4Button.TriggerRight.Value;
                //if (state.L2 > 0) tempButtons |= DualShock4Button.TriggerLeft.Value;
                //if (state.R2 > 0) tempButtons |= DualShock4Button.TriggerRight.Value;

                if (newState.Triangle) tempButtons |= DualShock4Button.Triangle.Value;
                if (newState.Circle) tempButtons |= DualShock4Button.Circle.Value;
                if (newState.Cross) tempButtons |= DualShock4Button.Cross.Value;
                if (newState.Square) tempButtons |= DualShock4Button.Square.Value;
                if (newState.Touchpad) tempSpecial |= DualShock4SpecialButton.Touchpad.Value;
                if (newState.Ps) tempSpecial |= DualShock4SpecialButton.Ps.Value;

                outDS4Report.wButtons = tempButtons;
                // Frame counter is high 6 bits. Low 2 bits is for extra buttons (PS, TP Click)
                outDS4Report.bSpecial = (byte)(tempSpecial);
                outDS4Report.wButtons |= tempDPad.Value;
            }

            outDS4Report.bThumbLX = newState.ThumbLeftX;
            outDS4Report.bThumbLY = newState.ThumbLeftY;
            outDS4Report.bThumbRX = newState.ThumbRightX;
            outDS4Report.bThumbRY = newState.ThumbRightY;

            outDS4Report.wAccelX = (short)(newState.accel.Y * 15);
            outDS4Report.wAccelY = (short)(newState.accel.X * 15);
            outDS4Report.wAccelZ = (short)-(newState.accel.Z * 15);
            outDS4Report.wGyroX = (short)(newState.gyro.Y * 15);
            outDS4Report.wGyroY = (short)(newState.gyro.X * 15);
            outDS4Report.wGyroZ = (short)-(newState.gyro.Z * 15);

            GCHandle h = GCHandle.Alloc(outDS4Report, GCHandleType.Pinned);
            Marshal.Copy(h.AddrOfPinnedObject(), rawOutReportEx, 0, 63);
            h.Free();   

            _controller.SubmitRawReport(rawOutReportEx);

            //_controller.SubmitReport();

            _currentState = newState;
        }

        private static DualShock4DPadDirection MapDPadDirection(DpadDirection dPad)
        {
            switch (dPad)
            {
                case DpadDirection.None:      return DualShock4DPadDirection.None;
                case DpadDirection.North:     return DualShock4DPadDirection.North;
                case DpadDirection.Northeast: return DualShock4DPadDirection.Northeast;
                case DpadDirection.East:      return DualShock4DPadDirection.East;
                case DpadDirection.Southeast: return DualShock4DPadDirection.Southeast;
                case DpadDirection.South:     return DualShock4DPadDirection.South;
                case DpadDirection.Southwest: return DualShock4DPadDirection.Southwest;
                case DpadDirection.West:      return DualShock4DPadDirection.West;
                case DpadDirection.Northwest: return DualShock4DPadDirection.Northwest;
                default:                      throw new NotImplementedException();
            }
        }
    }
}
