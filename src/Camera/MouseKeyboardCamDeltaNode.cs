﻿using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using mp.essentials;
using mp.essentials.Camera;
using SlimDX;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.SlimDX;
using VVVV.Utils.VMath;

namespace mp.essentials.Nodes.Camera
{
    [PluginInfo(
        Name = "MouseKeyboard",
        Category = "CameraDelta",
        Version = "Trackball",
        Author = "microdee",
        Help = "Customizable mouse and keyboard camera controller, with WASD and trackball"
    )]
    public class TrackballCameraDeltaMouseKeyboardNode : IPluginEvaluate
    {
        [Input("Delta In")]
        public Pin<CameraDelta> FDeltaIn;

        [Input("Rotation Speed", DefaultValue = 0.02)]
        public ISpread<double> FRotSpeed;
        [Input("Trackball Grab Position")]
        public ISpread<Vector2D> FTrackballScreenPos;
        [Input("Translation Speed", DefaultValue = 1)]
        public ISpread<double> FTranslSpeed;
        [Input("Translation Distance Influence", DefaultValue = 0.8)]
        public ISpread<double> FTranslDistInf;
        [Input("Zoom Speed", DefaultValue = 0.07)]
        public ISpread<double> FZoomSpeed;
        [Input("Scroll Multiplier", DefaultValue = 25)]
        public ISpread<double> FScrollMul;
        [Input("WASD Speed", DefaultValue = 10)]
        public ISpread<double> FWASDSpeed;
        [Input("Invert Y")]
        public ISpread<bool> FInvY;

        [Input("Rotation Key", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "None")]
        public ISpread<Keys> FRotationKey;
        [Input("Roll Key", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "ControlKey")]
        public ISpread<Keys> FRollKey;
        [Input("TranslationXY Key", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "None")]
        public ISpread<Keys> FTranslationXYKey;
        [Input("TranslationZ Key", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "ControlKey")]
        public ISpread<Keys> FTranslationZKey;
        [Input("PivotDistance Key", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "None")]
        public ISpread<Keys> FPivotDistanceKey;
        [Input("Zoom Key", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "ControlKey")]
        public ISpread<Keys> FZoomKey;

        [Input("Forward Key", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "W")]
        public ISpread<Keys> FForwardKey;
        [Input("Backward Key", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "S")]
        public ISpread<Keys> FBackwardKey;
        [Input("StrafeLeft Key", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "A")]
        public ISpread<Keys> FStrafeLeftKey;
        [Input("StrafeRight Key", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "D")]
        public ISpread<Keys> FStrafeRightKey;
        [Input("StrafeUp Key", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "E")]
        public ISpread<Keys> FStrafeUpKey;
        [Input("StrafeDown Key", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "Q")]
        public ISpread<Keys> FStrafeDownKey;

        [Input("Rotation Button", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "Left")]
        public ISpread<MouseButtons> FRotationButton;
        [Input("Roll Button", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "Left")]
        public ISpread<MouseButtons> FRollButton;
        [Input("TranslationXY Button", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "Middle")]
        public ISpread<MouseButtons> FTranslationXYButton;
        [Input("TranslationZ Button", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "Middle")]
        public ISpread<MouseButtons> FTranslationZButton;
        [Input("PivotDistance Button", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "Right")]
        public ISpread<MouseButtons> FPivotDistanceButton;
        [Input("Zoom Button", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "Right")]
        public ISpread<MouseButtons> FZoomButton;

        [Output("Camera Delta Out")]
        public ISpread<CameraDelta> FCamDelta;
        /*
        [Output("Mouse Position")]
        public ISpread<Vector4D> FMousePos;
        [Output("Mouse Buttons")]
        public ISpread<uint> FMouseButtons;
        */

        private CameraDelta Delta = new CameraDelta();
        private AccumulatingMouseObserver MouseObserver;
        private AccumulatingKeyboardObserver KeyboardObserver;
        private bool Init = true;

        public void Evaluate(int SpreadMax)
        {
            if (FDeltaIn.IsConnected)
            {
                FCamDelta.SliceCount = FDeltaIn.SliceCount + 1;
                for (int i = 0; i < FDeltaIn.SliceCount; i++)
                {
                    FCamDelta[i] = FDeltaIn[i];
                }
                FCamDelta[-1] = Delta;
            }
            else FCamDelta[0] = Delta;
            Delta.LockCursor = false;
            Delta.ResetSignals();
            if (Delta.InputMouse != null)
            {
                if (Init)
                {
                    Init = false;
                    MouseObserver = new AccumulatingMouseObserver();
                    KeyboardObserver = new AccumulatingKeyboardObserver();
                    MouseObserver.SubscribeTo(Delta.InputMouse.MouseNotifications);
                    KeyboardObserver.SubscribeTo(Delta.InputKeyboard.KeyNotifications);
                }

                var carea = MouseObserver?.LastNotification?.ClientArea ?? new Size(1, 1);
                var aspx = Math.Min(1, (double)carea.Height / (double)carea.Width);
                var aspy = Math.Min(1, (double)carea.Width / (double)carea.Height);
                var mousepos = new Vector2D(
                    MouseObserver.AccumulatedXDelta * aspx,
                    MouseObserver.AccumulatedYDelta * aspy);
                var mousewheel = new Vector2D(
                    (double)MouseObserver.AccumulatedWheelDelta / 120,
                    (double)MouseObserver.AccumulatedHorizontalWheelDelta / 120);
                try
                {
                    if (Delta.InteractUpstream)
                    {
                        if (MouseObserver.MouseClicks[FRotationButton[0]].DoubleClick) Delta.ResetRotation = true;
                        if (MouseObserver.MouseClicks[FTranslationXYButton[0]].DoubleClick) Delta.ResetTranslation = true;
                        if (MouseObserver.MouseClicks[FPivotDistanceButton[0]].DoubleClick) Delta.ResetPivotDistance = true;
                        if (MouseObserver.MouseClicks[FZoomButton[0]].DoubleClick) Delta.ResetFov = true;
                    }
                }
                catch { }
                MouseObserver.ResetAccumulation();
                //FMousePos[0] = new Vector4D(mousepos, mousewheel);
                //FMouseButtons[0] = (uint) Delta.InputMouse.PressedButtons;

                var translxykey = FTranslationXYKey[0] == Keys.None;
                var translzkey = FTranslationZKey[0] == Keys.None;

                var forw = FForwardKey[0] == Keys.None;
                var left = FStrafeLeftKey[0] == Keys.None;
                var backw = FBackwardKey[0] == Keys.None;
                var right = FStrafeRightKey[0] == Keys.None;
                var up = FStrafeUpKey[0] == Keys.None;
                var down = FStrafeDownKey[0] == Keys.None;

                var rotkey = FRotationKey[0] == Keys.None;
                var rollkey = FRollKey[0] == Keys.None;

                var pdkey = FPivotDistanceKey[0] == Keys.None;
                var zoomkey = FZoomKey[0] == Keys.None;

                var keyboard = from kp in KeyboardObserver.Keypresses.Values select kp.KeyCode;
                KeyboardObserver.ResetAccumulation();

                foreach (var key in keyboard)
                {
                    translxykey = translxykey || key == FTranslationXYKey[0];
                    translzkey = translzkey || key == FTranslationZKey[0];

                    forw = forw || key == FForwardKey[0];
                    left = left || key == FStrafeLeftKey[0];
                    backw = backw || key == FBackwardKey[0];
                    right = right || key == FStrafeRightKey[0];
                    up = up || key == FStrafeUpKey[0];
                    down = down || key == FStrafeDownKey[0];

                    rotkey = rotkey || key == FRotationKey[0];
                    rollkey = rollkey || key == FRollKey[0];

                    pdkey = pdkey || key == FPivotDistanceKey[0];
                    zoomkey = zoomkey || key == FZoomKey[0];
                }

                if (Delta.InteractUpstream)
                {

                    //// Translation
                    var transl = new Vector3D(0, 0, 0);
                    var crot = new Vector3D(0,0,0);
                    var translinf = VMath.Lerp(1.0, Math.Max(Delta.ConnectedCamera.PivotDistance * 0.25, 0.1), FTranslDistInf[0]);
                    if ((FTranslationXYButton[0] & Delta.InputMouse.PressedButtons) > 0 && translxykey)
                    {
                        if (!translzkey || FTranslationZKey[0] == Keys.None)
                        {
                            transl.x += mousepos.x;
                            transl.y -= mousepos.y;
                            Delta.LockCursor = true;
                        }
                    }
                    if ((FTranslationZButton[0] & Delta.InputMouse.PressedButtons) > 0 && translzkey)
                    {
                        if (!translxykey || FTranslationXYKey[0] == Keys.None)
                        {
                            transl.z += mousepos.y;
                            Delta.LockCursor = true;
                        }
                    }
                    transl *= FTranslSpeed[0] * translinf;

                    // WASD Transl
                    if (forw) transl.z -= FWASDSpeed[0];
                    if (backw) transl.z += FWASDSpeed[0];
                    if (left) transl.x += FWASDSpeed[0];
                    if (right) transl.x -= FWASDSpeed[0];
                    if (up) transl.y -= FWASDSpeed[0];
                    if (down) transl.y += FWASDSpeed[0];

                    if (transl.Length > 0.00001)
                        Delta.Translation = new Vector3D(transl);

                    //// Rotation
                    bool rotate = false;
                    var mouserot = new Vector2D(mousepos * FRotSpeed[0]);
                    if (FInvY[0]) mouserot.y *= -1;
                    if (Delta.ConnectedCamera.PivotDistance < 0.1) mouserot *= -1;
                    if (((FRotationButton[0] & Delta.InputMouse.PressedButtons) > 0 && rotkey) || forw || backw || left || right || up || down)
                    {
                        if (!rollkey || FRollKey[0] == Keys.None)
                        {
                            Delta.LockCursor = true;
                            // Prepare
                            mouserot.x = Math.Min(Math.Abs(mouserot.x) * 0.1, 0.499) * Math.Sign(mouserot.x);
                            mouserot.y = Math.Min(Math.Abs(mouserot.y) * 0.1, 0.499) * Math.Sign(mouserot.y);

                            // Trackball
                            
                            var trackbpos = new Vector2D(
                                Math.Pow(Math.Abs(FTrackballScreenPos[0].x), 2) * Math.Sign(FTrackballScreenPos[0].x),
                                Math.Pow(Math.Abs(FTrackballScreenPos[0].y), 2) * Math.Sign(FTrackballScreenPos[0].y));
                            trackbpos *= 0.25;
                            var trackbtr = VMath.Rotate(trackbpos.y * Math.PI * 2, trackbpos.x * Math.PI * -2, 0);
                            var trackxv = trackbtr * new Vector3D(-1, 0, 0);
                            var trackyv = trackbtr * new Vector3D(0, -1, 0);
                            crot = VMath.QuaternionToEulerYawPitchRoll(
                                (Quaternion.RotationAxis(trackxv.ToSlimDXVector(), (float)(mouserot.y * Math.PI * -2)) *
                                Quaternion.RotationAxis(trackyv.ToSlimDXVector(), (float)(mouserot.x * Math.PI * -2))
                                ).ToVector4D());
                            //crot = new Vector3D(mouserot.y, mouserot.x, 0);
                            crot *= 10;

                            rotate = true;
                        }
                    }
                    if ((FRollButton[0] & Delta.InputMouse.PressedButtons) > 0 && rollkey)
                    {
                        if (!rotkey || FRotationKey[0] == Keys.None)
                        {
                            Delta.LockCursor = true;
                            crot += new Vector3D(0, 0, mouserot.x * Math.PI * 2);
                            rotate = true;
                        }
                    }
                    if (rotate)
                    {
                        Delta.LockCursor = true;
                        Delta.PitchYawRoll = crot;
                    }
                    double pivotd = 0;
                    double zoom = mousewheel.y * FScrollMul[0] * FZoomSpeed[0];
                    if (zoomkey && (!pdkey || FPivotDistanceKey[0] == Keys.None))
                    {
                        zoom -= mousewheel.x * FScrollMul[0] * FZoomSpeed[0];
                        if ((FPivotDistanceButton[0] & Delta.InputMouse.PressedButtons) > 0)
                        {
                            Delta.LockCursor = true;
                            zoom -= mousepos.y * FZoomSpeed[0];
                        }
                    }
                    if (pdkey && (!zoomkey || FZoomKey[0] == Keys.None))
                    {
                        pivotd -= mousewheel.x * FScrollMul[0] * FTranslSpeed[0];
                        if ((FPivotDistanceButton[0] & Delta.InputMouse.PressedButtons) > 0)
                        {
                            Delta.LockCursor = true;
                            pivotd -= mousepos.y * FTranslSpeed[0];
                        }
                    }
                    if (Math.Abs(pivotd) > 0.00001) Delta.PivotDistance = pivotd;
                    if (Math.Abs(zoom) > 0.00001) Delta.Fov = zoom;

                }
            }
            else
            {
                MouseObserver?.Unsubscribe();
                KeyboardObserver?.Unsubscribe();
                Init = true;
            }
        }
    }
}
