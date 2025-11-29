using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using VRCFaceTracking;
using VRCFaceTracking.Core.Library;
using VRCFaceTracking.Core.Params.Expressions;
using VRCFTPicoModule.Data;
using static VRCFTPicoModule.Utils.Localization;

namespace VRCFTPicoModule.Utils
{
    public class Updater()
    {
        private readonly UdpClient? _udpClient;
        private readonly ILogger? _logger;
        private readonly bool _isLegacy;
        private readonly (bool, bool) _trackingAvailable;

        public Updater(UdpClient udpClient, ILogger logger, bool isLegacy, (bool, bool) trackingAvailable) : this()
        {
            _udpClient = udpClient;
            _logger = logger;
            _isLegacy = isLegacy;
            _trackingAvailable = trackingAvailable;
        }
        
        private int _timeOut;
        private float _lastMouthLeft;
        private float _lastMouthRight;
        private const float SmoothingFactor = 0.5f;
        private ModuleState _moduleState;

        public void Update(ModuleState state)
        {
            if (_udpClient == null)
                return;
            
            if (_logger == null)
                return;
            
            _udpClient.Client.ReceiveTimeout = 100;
            _moduleState = state;
            
            if (_moduleState != ModuleState.Active) return;

            try
            {
                var endPoint = new IPEndPoint(IPAddress.Any, 0);
                var data = _udpClient.Receive(ref endPoint);
                var pShape = ParseData(data, _isLegacy);
                
                if (_trackingAvailable.Item1)
                    UpdateEye(pShape);
                
                if (_trackingAvailable.Item2)
                    UpdateExpression(pShape);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                if (++_timeOut > 600)
                {
                    _logger.LogWarning(T("update-timeout"));
                    _timeOut = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(T("update-failed"), ex);
            }
        }

        private static float[] ParseData(byte[] data, bool isLegacy)
        {
            if (isLegacy && data.Length >= Marshal.SizeOf<LegacyDataPacket.DataPackBody>())
                return DataPacketHelpers.ByteArrayToStructure<LegacyDataPacket.DataPackBody>(data).blendShapeWeight;

            if (data.Length <
                Marshal.SizeOf<DataPacket.DataPackHeader>() + Marshal.SizeOf<DataPacket.DataPackBody>()) return [];
            var header = DataPacketHelpers.ByteArrayToStructure<DataPacket.DataPackHeader>(data);
            return header.trackingType == 2 ? DataPacketHelpers.ByteArrayToStructure<DataPacket.DataPackBody>(data, Marshal.SizeOf<DataPacket.DataPackHeader>()).blendShapeWeight : [];
        }

        private static void UpdateEye(float[] pShape)
        {
            var eye = UnifiedTracking.Data.Eye;

            #region LeftEye
            eye.Left.Openness = 1f - pShape[(int)BlendShape.Index.EyeBlink_L];
            eye.Left.Gaze.x = pShape[(int)BlendShape.Index.EyeLookIn_L] - pShape[(int)BlendShape.Index.EyeLookOut_L];
            eye.Left.Gaze.y = pShape[(int)BlendShape.Index.EyeLookUp_L] - pShape[(int)BlendShape.Index.EyeLookDown_L];
            #endregion

            #region RightEye
            eye.Right.Openness = 1f - pShape[(int)BlendShape.Index.EyeBlink_R];
            eye.Right.Gaze.x = pShape[(int)BlendShape.Index.EyeLookOut_R] - pShape[(int)BlendShape.Index.EyeLookIn_R];
            eye.Right.Gaze.y = pShape[(int)BlendShape.Index.EyeLookUp_R] - pShape[(int)BlendShape.Index.EyeLookDown_R];
            #endregion
            
            #region Brow
            SetParam(pShape, BlendShape.Index.BrowInnerUp, UnifiedExpressions.BrowInnerUpLeft);
            SetParam(pShape, BlendShape.Index.BrowInnerUp, UnifiedExpressions.BrowInnerUpRight);
            SetParam(pShape, BlendShape.Index.BrowOuterUp_L, UnifiedExpressions.BrowOuterUpLeft);
            SetParam(pShape, BlendShape.Index.BrowOuterUp_R, UnifiedExpressions.BrowOuterUpRight);
            SetParam(pShape, BlendShape.Index.BrowDown_L, UnifiedExpressions.BrowLowererLeft);
            SetParam(pShape, BlendShape.Index.BrowDown_L, UnifiedExpressions.BrowPinchLeft);
            SetParam(pShape, BlendShape.Index.BrowDown_R, UnifiedExpressions.BrowLowererRight);
            SetParam(pShape, BlendShape.Index.BrowDown_R, UnifiedExpressions.BrowPinchRight);
            #endregion

            #region Eye
            SetParam(pShape, BlendShape.Index.EyeSquint_L, UnifiedExpressions.EyeSquintLeft);
            SetParam(pShape, BlendShape.Index.EyeSquint_R, UnifiedExpressions.EyeSquintRight);
            SetParam(pShape[(int)BlendShape.Index.EyeWide_L] / 0.5f, UnifiedExpressions.EyeWideLeft); // 目を見開けるように補正
            SetParam(pShape[(int)BlendShape.Index.EyeWide_R] / 0.5f, UnifiedExpressions.EyeWideRight); // 目を見開けるように補正
            #endregion
        }

        private void UpdateExpression(float[] pShape)
        {
            #region Jaw
            SetParam(pShape[(int)BlendShape.Index.JawOpen] / 0.8f, UnifiedExpressions.JawOpen); // 口を大きく開けるように補正
            SetParam(pShape[(int)BlendShape.Index.JawLeft] / 0.5f, UnifiedExpressions.JawLeft); // 口を開けたまま左に大きく動かせるように補正
            SetParam(pShape[(int)BlendShape.Index.JawRight] / 0.5f, UnifiedExpressions.JawRight); // 口を開けたまま右に大きく動かせるように補正
            SetParam(pShape, BlendShape.Index.JawForward, UnifiedExpressions.JawForward);
            SetParam(pShape, BlendShape.Index.MouthClose, UnifiedExpressions.MouthClosed);
            #endregion

            #region Cheek
            SetParam(pShape, BlendShape.Index.CheekSquint_L, UnifiedExpressions.CheekSquintLeft);
            SetParam(pShape, BlendShape.Index.CheekSquint_R, UnifiedExpressions.CheekSquintRight);

            var mouthLeft = SmoothValue(pShape[(int)BlendShape.Index.MouthLeft], ref _lastMouthLeft);
            var mouthRight = SmoothValue(pShape[(int)BlendShape.Index.MouthRight], ref _lastMouthRight);

            var cheekPuff = pShape[(int)BlendShape.Index.CheekPuff];
            const float diffThreshold = 0.1f;

            if (cheekPuff > 0.1f)
            {
                if (mouthLeft > mouthRight + diffThreshold)
                {
                    SetParam(cheekPuff, UnifiedExpressions.CheekPuffLeft);
                    SetParam(cheekPuff + mouthLeft, UnifiedExpressions.CheekPuffRight);
                }
                else if (mouthRight > mouthLeft + diffThreshold)
                {
                    SetParam(cheekPuff + mouthRight, UnifiedExpressions.CheekPuffLeft);
                    SetParam(cheekPuff, UnifiedExpressions.CheekPuffRight);
                }
                else
                {
                    SetParam(pShape, BlendShape.Index.CheekPuff, UnifiedExpressions.CheekPuffLeft);
                    SetParam(pShape, BlendShape.Index.CheekPuff, UnifiedExpressions.CheekPuffRight);
                }
            }
            else
            {
                SetParam(pShape, BlendShape.Index.CheekPuff, UnifiedExpressions.CheekPuffLeft);
                SetParam(pShape, BlendShape.Index.CheekPuff, UnifiedExpressions.CheekPuffRight);
            }
            #endregion

            #region Nose
            var NoseSneerLeft = pShape[(int)BlendShape.Index.NoseSneer_L]; // 口が半開きになるのでNoseSneerを0.6まで動作しないように補正
            SetParam(pShape[(int)BlendShape.Index.NoseSneer_L] > 0.6f
                ? NoseSneerLeft
                : 0f,
                UnifiedExpressions.NoseSneerLeft);
            var NoseSneerRight = pShape[(int)BlendShape.Index.NoseSneer_R]; // 口が半開きになるのでNoseSneerを0.6まで動作しないように補正
            SetParam(pShape[(int)BlendShape.Index.NoseSneer_R] > 0.6f
                ? NoseSneerRight
                : 0f,
                UnifiedExpressions.NoseSneerLeft);
            #endregion

            #region Mouth
            var MouthUpperUpLeft = pShape[(int)BlendShape.Index.MouthUpperUp_L]; // MouthUpperUpをMouthPressが0.1まで入っているうちは動作しないように補正
            SetParam(pShape[(int)BlendShape.Index.MouthPress_L] < 0.1f
                ? MouthUpperUpLeft / 0.8f
                : 0f,
                UnifiedExpressions.MouthUpperUpLeft);
            var MouthUpperUpRight = pShape[(int)BlendShape.Index.MouthUpperUp_R]; // MouthUpperUpをMouthPressが0.1まで入っているうちは動作しないように補正
            SetParam(pShape[(int)BlendShape.Index.MouthPress_R] < 0.1f
                ? MouthUpperUpRight / 0.8f
                : 0f,
                UnifiedExpressions.MouthUpperUpRight);
            SetParam(pShape, BlendShape.Index.MouthLowerDown_L, UnifiedExpressions.MouthLowerDownLeft);
            SetParam(pShape, BlendShape.Index.MouthLowerDown_R, UnifiedExpressions.MouthLowerDownRight);

            var mouthFrownLeft = pShape[(int)BlendShape.Index.MouthFrown_L];
            SetParam(pShape[(int)BlendShape.Index.JawOpen] > 0.1f
                    ? mouthFrownLeft / 2f
                    : pShape[(int)BlendShape.Index.MouthRollLower] > 0.2f 
                        ? mouthFrownLeft * 2.5f + pShape[(int)BlendShape.Index.MouthRollLower]
                        : mouthFrownLeft,
                UnifiedExpressions.MouthFrownLeft);

            var mouthFrownRight = pShape[(int)BlendShape.Index.MouthFrown_R];
            SetParam(pShape[(int)BlendShape.Index.JawOpen] > 0.1f
                    ? mouthFrownRight / 2f
                    : pShape[(int)BlendShape.Index.MouthRollLower] > 0.2f
                        ? mouthFrownRight * 2.5f + pShape[(int)BlendShape.Index.MouthRollLower]
                        : mouthFrownRight,
                UnifiedExpressions.MouthFrownRight);
            
            SetParam(pShape, BlendShape.Index.MouthDimple_L, UnifiedExpressions.MouthDimpleLeft);
            SetParam(pShape, BlendShape.Index.MouthDimple_R, UnifiedExpressions.MouthDimpleRight);
            SetParam(pShape[(int)BlendShape.Index.MouthLeft] / 0.5f, UnifiedExpressions.MouthUpperLeft); // 口を閉じたまま左に大きく動かせるように補正
            SetParam(pShape[(int)BlendShape.Index.MouthLeft] / 0.5f, UnifiedExpressions.MouthLowerLeft); // 口を閉じたまま左に大きく動かせるように補正
            SetParam(pShape[(int)BlendShape.Index.MouthRight] / 0.5f, UnifiedExpressions.MouthUpperRight); // 口を閉じたまま右に大きく動かせるように補正
            SetParam(pShape[(int)BlendShape.Index.MouthRight] / 0.5f, UnifiedExpressions.MouthLowerRight); // 口を閉じたまま右に大きく動かせるように補正
            SetParam(pShape, BlendShape.Index.MouthPress_L, UnifiedExpressions.MouthPressLeft);
            SetParam(pShape, BlendShape.Index.MouthPress_R, UnifiedExpressions.MouthPressRight);
            SetParam(pShape, BlendShape.Index.MouthShrugLower, UnifiedExpressions.MouthRaiserLower);
            SetParam(pShape, BlendShape.Index.MouthShrugUpper, UnifiedExpressions.MouthRaiserUpper);

            // 0.5までしか来ない前提で線形補正
            var smileLeftRaw = pShape[(int)BlendShape.Index.MouthSmile_L] / 0.5f;
            var smileRightRaw = pShape[(int)BlendShape.Index.MouthSmile_R] / 0.5f;
            // 妙に偏るので強い方を優先してシンメトリー化
            var mouthSmileSym = Math.Max(smileLeftRaw, smileRightRaw);
            // 最終的に送る値
            var mouthSmileLeft = mouthSmileSym;
            var mouthSmileRight = mouthSmileSym;
            SetParam(pShape[(int)BlendShape.Index.MouthRollLower] < 0.2f
                    ? mouthSmileLeft
                    : 0f,
                UnifiedExpressions.MouthCornerPullLeft);
            SetParam(pShape[(int)BlendShape.Index.MouthRollLower] < 0.2f
                    ? mouthSmileLeft - pShape[(int)BlendShape.Index.MouthRollLower]
                    : 0f,
                UnifiedExpressions.MouthCornerSlantLeft);
            SetParam(pShape[(int)BlendShape.Index.MouthRollLower] < 0.2f
                    ? mouthSmileRight
                    : 0f,
                UnifiedExpressions.MouthCornerPullRight);
            SetParam(pShape[(int)BlendShape.Index.MouthRollLower] < 0.2f
                    ? mouthSmileRight - pShape[(int)BlendShape.Index.MouthRollLower]
                    : 0f,
                UnifiedExpressions.MouthCornerSlantRight);

            SetParam(pShape, BlendShape.Index.MouthStretch_L, UnifiedExpressions.MouthStretchLeft);
            SetParam(pShape, BlendShape.Index.MouthStretch_R, UnifiedExpressions.MouthStretchRight);
            #endregion

            #region Lip
            var isFunnelLeft = pShape[(int)BlendShape.Index.MouthPucker] > 0.3f &&
                               pShape[(int)BlendShape.Index.MouthPress_L] < 0.2f;
            var isFunnelRight = pShape[(int)BlendShape.Index.MouthPucker] > 0.3f &&
                               pShape[(int)BlendShape.Index.MouthPress_R] < 0.2f;
            var mouthFunnelFixed = pShape[(int)BlendShape.Index.MouthPucker];
            SetParam(isFunnelLeft
                    ? mouthFunnelFixed
                    : pShape[(int)BlendShape.Index.MouthFunnel],
                UnifiedExpressions.LipFunnelUpperLeft);
            SetParam(isFunnelRight
                    ? mouthFunnelFixed
                    : pShape[(int)BlendShape.Index.MouthFunnel],
                UnifiedExpressions.LipFunnelUpperRight);
            SetParam(isFunnelLeft
                    ? mouthFunnelFixed
                    : pShape[(int)BlendShape.Index.MouthFunnel],
                UnifiedExpressions.LipFunnelLowerLeft);
            SetParam(isFunnelRight
                    ? mouthFunnelFixed
                    : pShape[(int)BlendShape.Index.MouthFunnel],
                UnifiedExpressions.LipFunnelLowerRight);
            
            SetParam(pShape, BlendShape.Index.MouthPucker, UnifiedExpressions.LipPuckerUpperLeft);
            SetParam(pShape, BlendShape.Index.MouthPucker, UnifiedExpressions.LipPuckerUpperRight);
            SetParam(pShape, BlendShape.Index.MouthPucker, UnifiedExpressions.LipPuckerLowerLeft);
            SetParam(pShape, BlendShape.Index.MouthPucker, UnifiedExpressions.LipPuckerLowerRight);
            SetParam(pShape, BlendShape.Index.MouthRollUpper, UnifiedExpressions.LipSuckUpperLeft);
            SetParam(pShape, BlendShape.Index.MouthRollUpper, UnifiedExpressions.LipSuckUpperRight);
            SetParam(pShape, BlendShape.Index.MouthRollLower, UnifiedExpressions.LipSuckLowerLeft);
            SetParam(pShape, BlendShape.Index.MouthRollLower, UnifiedExpressions.LipSuckLowerRight);
            #endregion

            #region Tongue
            // 気休め程度の誤爆防止
            SetParam(pShape[(int)BlendShape.Index.TongueOut] > 0.95f
                ? pShape[(int)BlendShape.Index.TongueOut]
                : 0f,
            UnifiedExpressions.TongueOut);
            #endregion
        }

        private static float SmoothValue(float newValue, ref float lastValue)
        {
            lastValue += (newValue - lastValue) * SmoothingFactor;
            return lastValue;
        }

        private static void SetParam(float[] pShape, BlendShape.Index index, UnifiedExpressions outputType)
        {
            UnifiedTracking.Data.Shapes[(int)outputType].Weight = pShape[(int)index];
        }

        private static void SetParam(float param, UnifiedExpressions outputType)
        {
            UnifiedTracking.Data.Shapes[(int)outputType].Weight = param;
        }
    }
}