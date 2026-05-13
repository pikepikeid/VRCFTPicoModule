using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using VRCFaceTracking;
using VRCFaceTracking.Core.Params.Expressions;
using VRCFTPicoModule.Data;

namespace VRCFTPicoModule.Utils;

public class TestModeUpdater : Updater
{
    public TestModeUpdater(
        UdpClient udpClient,
        ILogger logger,
        bool isLegacy,
        ModuleConfig config)
        : base(udpClient, logger, isLegacy, config)
    {
    }

    protected override void UpdateEye(float[] pShape)
    {
        var eye = UnifiedTracking.Data.Eye;

        #region LeftEye
        eye.Left.Openness = 1f - pShape[(int)BlendShape.Index.EyeBlink_L];
        eye.Left.Gaze.x = (pShape[(int)BlendShape.Index.EyeLookIn_L] - pShape[(int)BlendShape.Index.EyeLookOut_L]) * _config.EyeGainX;
        eye.Left.Gaze.y = (pShape[(int)BlendShape.Index.EyeLookUp_L] - pShape[(int)BlendShape.Index.EyeLookDown_L]) * _config.EyeGainY;
        #endregion

        #region RightEye
        eye.Right.Openness = 1f - pShape[(int)BlendShape.Index.EyeBlink_R];
        eye.Right.Gaze.x = (pShape[(int)BlendShape.Index.EyeLookOut_R] - pShape[(int)BlendShape.Index.EyeLookIn_R]) * _config.EyeGainX;
        eye.Right.Gaze.y = (pShape[(int)BlendShape.Index.EyeLookUp_R] - pShape[(int)BlendShape.Index.EyeLookDown_R]) * _config.EyeGainY;
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
        SetParam(pShape, BlendShape.Index.EyeWide_L, UnifiedExpressions.EyeWideLeft);
        SetParam(pShape, BlendShape.Index.EyeWide_R, UnifiedExpressions.EyeWideRight);
        #endregion
    }

    protected override void UpdateExpression(float[] pShape)
    {
        #region Jaw
        SetParam(pShape, BlendShape.Index.JawOpen, UnifiedExpressions.JawOpen);
        SetParam(pShape, BlendShape.Index.JawLeft, UnifiedExpressions.JawLeft);
        SetParam(pShape, BlendShape.Index.JawRight, UnifiedExpressions.JawRight);
        SetParam(pShape, BlendShape.Index.JawForward, UnifiedExpressions.JawForward);
        SetParam(pShape, BlendShape.Index.MouthClose, UnifiedExpressions.MouthClosed);
        #endregion

        #region Cheek
        SetParam(pShape, BlendShape.Index.CheekSquint_L, UnifiedExpressions.CheekSquintLeft);
        SetParam(pShape, BlendShape.Index.CheekSquint_R, UnifiedExpressions.CheekSquintRight);

        SetParam(pShape, BlendShape.Index.CheekPuff, UnifiedExpressions.CheekPuffLeft);
        SetParam(pShape, BlendShape.Index.CheekPuff, UnifiedExpressions.CheekPuffRight);
        #endregion

        #region Nose
        SetParam(pShape, BlendShape.Index.NoseSneer_L, UnifiedExpressions.NoseSneerLeft);
        SetParam(pShape, BlendShape.Index.NoseSneer_R, UnifiedExpressions.NoseSneerRight);
        #endregion

        #region Mouth
        SetParam(pShape, BlendShape.Index.MouthUpperUp_L, UnifiedExpressions.MouthUpperUpLeft);
        SetParam(pShape, BlendShape.Index.MouthUpperUp_R, UnifiedExpressions.MouthUpperUpRight);
        SetParam(pShape, BlendShape.Index.MouthLowerDown_L, UnifiedExpressions.MouthLowerDownLeft);
        SetParam(pShape, BlendShape.Index.MouthLowerDown_R, UnifiedExpressions.MouthLowerDownRight);

        SetParam(pShape, BlendShape.Index.MouthFrown_L, UnifiedExpressions.MouthFrownLeft);
        SetParam(pShape, BlendShape.Index.MouthFrown_R, UnifiedExpressions.MouthFrownRight);

        SetParam(pShape, BlendShape.Index.MouthDimple_L, UnifiedExpressions.MouthDimpleLeft);
        SetParam(pShape, BlendShape.Index.MouthDimple_R, UnifiedExpressions.MouthDimpleRight);
        SetParam(pShape, BlendShape.Index.MouthLeft, UnifiedExpressions.MouthUpperLeft);
        SetParam(pShape, BlendShape.Index.MouthLeft, UnifiedExpressions.MouthLowerLeft);
        SetParam(pShape, BlendShape.Index.MouthRight, UnifiedExpressions.MouthUpperRight);
        SetParam(pShape, BlendShape.Index.MouthRight, UnifiedExpressions.MouthLowerRight);
        SetParam(pShape, BlendShape.Index.MouthPress_L, UnifiedExpressions.MouthPressLeft);
        SetParam(pShape, BlendShape.Index.MouthPress_R, UnifiedExpressions.MouthPressRight);
        SetParam(pShape, BlendShape.Index.MouthShrugLower, UnifiedExpressions.MouthRaiserLower);
        SetParam(pShape, BlendShape.Index.MouthShrugUpper, UnifiedExpressions.MouthRaiserUpper);

        SetParam(pShape, BlendShape.Index.MouthSmile_L, UnifiedExpressions.MouthCornerPullLeft);
        SetParam(pShape, BlendShape.Index.MouthSmile_L, UnifiedExpressions.MouthCornerSlantLeft);
        SetParam(pShape, BlendShape.Index.MouthSmile_R, UnifiedExpressions.MouthCornerPullRight);
        SetParam(pShape, BlendShape.Index.MouthSmile_R, UnifiedExpressions.MouthCornerSlantRight);

        SetParam(pShape, BlendShape.Index.MouthStretch_L, UnifiedExpressions.MouthStretchLeft);
        SetParam(pShape, BlendShape.Index.MouthStretch_R, UnifiedExpressions.MouthStretchRight);
        #endregion

        #region Lip
        SetParam(pShape, BlendShape.Index.MouthFunnel, UnifiedExpressions.LipFunnelUpperLeft);
        SetParam(pShape, BlendShape.Index.MouthFunnel, UnifiedExpressions.LipFunnelUpperRight);
        SetParam(pShape, BlendShape.Index.MouthFunnel, UnifiedExpressions.LipFunnelLowerLeft);
        SetParam(pShape, BlendShape.Index.MouthFunnel, UnifiedExpressions.LipFunnelLowerRight);
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
        SetParam(pShape, BlendShape.Index.TongueOut, UnifiedExpressions.TongueOut);
        #endregion
    }
}