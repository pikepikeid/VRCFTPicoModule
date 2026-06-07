using System;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using VRCFaceTracking;
using VRCFaceTracking.Core.Params.Expressions;
using VRCFTPicoModule.Data;

namespace VRCFTPicoModule.Utils;

public class ExtendedUpdater : Updater
{
    public ExtendedUpdater(
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
        var browInner = pShape[(int)BlendShape.Index.BrowInnerUp];
        var browOuterLeft = Math.Max(0f, pShape[(int)BlendShape.Index.BrowOuterUp_L] - browInner);
        var browOuterRight = Math.Max(0f, pShape[(int)BlendShape.Index.BrowOuterUp_R] - browInner);
        SetParam(browInner / 0.5f, UnifiedExpressions.BrowInnerUpLeft);
        SetParam(browInner / 0.5f, UnifiedExpressions.BrowInnerUpRight);
        SetParam(browOuterLeft / 0.5f, UnifiedExpressions.BrowOuterUpLeft);
        SetParam(browOuterRight / 0.5f, UnifiedExpressions.BrowOuterUpRight);

        SetParam(pShape[(int)BlendShape.Index.BrowDown_L] / 0.5f, UnifiedExpressions.BrowLowererLeft);
        SetParam(pShape[(int)BlendShape.Index.BrowDown_L] / 1.5f, UnifiedExpressions.BrowPinchLeft);
        SetParam(pShape[(int)BlendShape.Index.BrowDown_R] / 0.5f, UnifiedExpressions.BrowLowererRight);
        SetParam(pShape[(int)BlendShape.Index.BrowDown_R] / 1.5f, UnifiedExpressions.BrowPinchRight);
        #endregion

        #region Eye
        SetParam(pShape[(int)BlendShape.Index.EyeSquint_L] / 0.1f, UnifiedExpressions.EyeSquintLeft);
        SetParam(pShape[(int)BlendShape.Index.EyeSquint_R] / 0.1f, UnifiedExpressions.EyeSquintRight);
        SetParam(pShape[(int)BlendShape.Index.EyeWide_L] / 1.0f, UnifiedExpressions.EyeWideLeft);
        SetParam(pShape[(int)BlendShape.Index.EyeWide_R] / 1.0f, UnifiedExpressions.EyeWideRight);
        #endregion
    }

    protected override void UpdateExpression(float[] pShape)
    {
        #region Jaw
        SetParam(pShape[(int)BlendShape.Index.JawOpen] / 0.75f, UnifiedExpressions.JawOpen); // 口を開きすぎないように補正
        SetParam(pShape[(int)BlendShape.Index.JawLeft] / 0.5f, UnifiedExpressions.JawLeft); // 口を開けたまま左に大きく動かせるように補正
        SetParam(pShape[(int)BlendShape.Index.JawRight] / 0.5f, UnifiedExpressions.JawRight); // 口を開けたまま右に大きく動かせるように補正
        SetParam(pShape, BlendShape.Index.JawForward, UnifiedExpressions.JawForward);
        SetParam(pShape[(int)BlendShape.Index.MouthClose] / 0.65f, UnifiedExpressions.MouthClosed);
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
            UnifiedExpressions.NoseSneerRight);
        #endregion

        #region Mouth
        // 各種補正セクション
        // MouthUpperUpをMouthPressが0.1まで入っているうちは動作しないように補正
        var MouthUpperUpLeft =  
            pShape[(int)BlendShape.Index.MouthPress_L] < 0.1f
                ? pShape[(int)BlendShape.Index.MouthUpperUp_L] / 0.8f
                : 0f;

        var MouthUpperUpRight =  
            pShape[(int)BlendShape.Index.MouthPress_R] < 0.1f
                ? pShape[(int)BlendShape.Index.MouthUpperUp_R] / 0.8f
                : 0f;
        // MouthUpperUpシンメトリー化
        var mouthUpperUpSym = Math.Max(MouthUpperUpLeft, MouthUpperUpRight);

        // MouthLeft/Rightを0.25までは動作しないように
        var MouthLeft = (pShape[(int)BlendShape.Index.MouthLeft] / 0.5f);
        var MouthRight = (pShape[(int)BlendShape.Index.MouthRight] / 0.5f);

        if (MouthLeft < 0.25f)
        {
            MouthLeft = 0f;
        }
        if (MouthRight < 0.25f)
        {
            MouthRight = 0f;
        }

        var mouthFrownLeft = pShape[(int)BlendShape.Index.MouthFrown_L];
        var mouthFrownRight = pShape[(int)BlendShape.Index.MouthFrown_R];

        // 口を開くだけでFrownが入るため減衰
        var rawFrown = Math.Max(
            mouthFrownLeft,
            mouthFrownRight);

        var jawOpen =
            pShape[(int)BlendShape.Index.JawOpen];

        if (jawOpen > 0.1f)
        {
            rawFrown *= 0.5f;
        }

        // Shrug->Frown変換
        var mouthShrugLower =
            pShape[(int)BlendShape.Index.MouthShrugLower] > 0.2f
                ? (pShape[(int)BlendShape.Index.MouthShrugLower] - 0.2f) / 0.3f
                : 0f;

        var mouthShrugUpper =
            pShape[(int)BlendShape.Index.MouthShrugUpper] > 0.2f
                ? (pShape[(int)BlendShape.Index.MouthShrugUpper] - 0.2f) / 0.3f
                : 0f;

        // LipSuckやPucker、Funnel中はShrug->Frown変換を無効化
        var rollAmount = Math.Max(
            pShape[(int)BlendShape.Index.MouthRollUpper],
            pShape[(int)BlendShape.Index.MouthRollLower]);

        var puckerAmount =
            pShape[(int)BlendShape.Index.MouthPucker];

        var funnelAmount =
            pShape[(int)BlendShape.Index.MouthFunnel];

        if (rollAmount > 0.3f ||
            puckerAmount > 0.4f ||
            funnelAmount > 0.1f)
        {
            mouthShrugLower = 0f;
            mouthShrugUpper = 0f;
        }

        // 生FrownとShrug変換の強い方を採用
        var mouthFrownOutput = Math.Max(
            rawFrown,
            Math.Max(
                mouthShrugLower,
                mouthShrugUpper));

        if (jawOpen < 0.1f) // JawOpenが小さいときだけFrownを減衰
        {
            float t = Math.Clamp(mouthFrownOutput / 0.5f, 0f, 1f);
            mouthFrownOutput *= t;
        }

        // Stretchは強めの補正
        var stretchLeftRaw = pShape[(int)BlendShape.Index.MouthStretch_L] / 0.3f;
        var stretchRightRaw = pShape[(int)BlendShape.Index.MouthStretch_R] / 0.3f;
        // 0.5までしか来ない前提でSmileを線形補正
        var smileLeftRaw = pShape[(int)BlendShape.Index.MouthSmile_L] / 0.5f;
        var smileRightRaw = pShape[(int)BlendShape.Index.MouthSmile_R] / 0.5f;
        // 妙に偏るので強い方を優先してシンメトリー化
        var mouthSmileSym = Math.Max(smileLeftRaw, smileRightRaw);
        var mouthStretchSym = Math.Max(stretchLeftRaw, stretchRightRaw);
        // Stretchを入れたいときにSmileが入ると不自然なので、Stretchが強いときはSmileとUpperUpを減衰
        if (mouthSmileSym <= 0.4f &&
            mouthStretchSym > 0.05f)
        {
            var smileScale = Math.Max(0.2f, 1f - mouthSmileSym);
            mouthSmileSym *= smileScale; // Stretchが入るときはSmileを減衰

            var upperUpScale = Math.Max(0.3f, 1f - mouthUpperUpSym);
            mouthUpperUpSym *= upperUpScale;    // Stretchが入るときはUpperUpも減衰
        }
        // 最終的に送る値
        var mouthSmileLeft = mouthSmileSym;
        var mouthSmileRight = mouthSmileSym;

        SetParam(mouthUpperUpSym, UnifiedExpressions.MouthUpperUpLeft);
        SetParam(mouthUpperUpSym, UnifiedExpressions.MouthUpperUpRight);

        SetParam(mouthFrownOutput, UnifiedExpressions.MouthFrownLeft);
        SetParam(mouthFrownOutput, UnifiedExpressions.MouthFrownRight);

        SetParam(mouthStretchSym, UnifiedExpressions.MouthStretchLeft);
        SetParam(mouthStretchSym, UnifiedExpressions.MouthStretchRight);
        SetParam(pShape[(int)BlendShape.Index.MouthRollLower] < 0.2f
                ? mouthSmileLeft
                : 0f,
            UnifiedExpressions.MouthCornerPullLeft);
        SetParam(pShape[(int)BlendShape.Index.MouthRollLower] < 0.2f
                ? Math.Max(0f, mouthSmileLeft - pShape[(int)BlendShape.Index.MouthRollLower]) // 念の為にClamp
                : 0f,
            UnifiedExpressions.MouthCornerSlantLeft);
        SetParam(pShape[(int)BlendShape.Index.MouthRollLower] < 0.2f
                ? mouthSmileRight
                : 0f,
            UnifiedExpressions.MouthCornerPullRight);
        SetParam(pShape[(int)BlendShape.Index.MouthRollLower] < 0.2f
                ? Math.Max(0f, mouthSmileRight - pShape[(int)BlendShape.Index.MouthRollLower]) // 念の為にClamp
                : 0f,
            UnifiedExpressions.MouthCornerSlantRight);

        SetParam(pShape, BlendShape.Index.MouthLowerDown_L, UnifiedExpressions.MouthLowerDownLeft);
        SetParam(pShape, BlendShape.Index.MouthLowerDown_R, UnifiedExpressions.MouthLowerDownRight);
        SetParam(pShape, BlendShape.Index.MouthDimple_L, UnifiedExpressions.MouthDimpleLeft);
        SetParam(pShape, BlendShape.Index.MouthDimple_R, UnifiedExpressions.MouthDimpleRight);
        SetParam(MouthLeft, UnifiedExpressions.MouthUpperLeft);
        SetParam(MouthLeft, UnifiedExpressions.MouthLowerLeft);
        SetParam(MouthRight, UnifiedExpressions.MouthUpperRight);
        SetParam(MouthRight, UnifiedExpressions.MouthLowerRight);
        SetParam(pShape, BlendShape.Index.MouthPress_L, UnifiedExpressions.MouthPressLeft);
        SetParam(pShape, BlendShape.Index.MouthPress_R, UnifiedExpressions.MouthPressRight);

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
        var tongueOut = pShape[(int)BlendShape.Index.TongueOut];

        if (jawOpen < 0.5f)
        {
            tongueOut = 0f;
        }

        SetParam(tongueOut, UnifiedExpressions.TongueOut);
        #endregion
    }

    private static float SmoothValue(float newValue, ref float lastValue)
    {
        lastValue += (newValue - lastValue) * SmoothingFactor;
        return lastValue;
    }

    protected override void SetParam(float[] pShape, BlendShape.Index index, UnifiedExpressions outputType)
    {
        UnifiedTracking.Data.Shapes[(int)outputType].Weight = pShape[(int)index];
    }

    protected override void SetParam(float param, UnifiedExpressions outputType)
    {
        UnifiedTracking.Data.Shapes[(int)outputType].Weight = param;
    }
}