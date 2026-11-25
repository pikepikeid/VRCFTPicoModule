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
    private bool _tongueDetected;

    protected override void UpdateEye(float[] pShape)
    {
        var eye = UnifiedTracking.Data.Eye;
        var leftBlink = pShape[(int)BlendShape.Index.EyeBlink_L];
        var rightBlink = pShape[(int)BlendShape.Index.EyeBlink_R];

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
        var browL = pShape[(int)BlendShape.Index.BrowDown_L];
        var browR = pShape[(int)BlendShape.Index.BrowDown_R];
        var leftFactor = 1f;
        var rightFactor = 1f;
        if (leftBlink < rightBlink)
        {
            leftFactor = 1f - (rightBlink - leftBlink) * 0.5f;
        }
        if (rightBlink < leftBlink)
        {
            rightFactor = 1f - (leftBlink - rightBlink) * 0.5f;
        }
        var leftBase = 1f / (1f + MathF.Exp(-12f * (browL - 0.25f)));
        var rightBase = 1f / (1f + MathF.Exp(-12f * (browR - 0.25f)));
        leftBase *= leftFactor;
        rightBase *= rightFactor;
        SetParam(leftBase / 0.8f, UnifiedExpressions.BrowLowererLeft);
        SetParam(leftBase / 0.8f, UnifiedExpressions.BrowPinchLeft);
        SetParam(rightBase / 0.8f, UnifiedExpressions.BrowLowererRight);
        SetParam(rightBase / 0.8f, UnifiedExpressions.BrowPinchRight);

        var browInner = pShape[(int)BlendShape.Index.BrowInnerUp];
        var browOuterLeft = MathF.Max(0f, pShape[(int)BlendShape.Index.BrowOuterUp_L] - browInner);
        var browOuterRight = MathF.Max(0f, pShape[(int)BlendShape.Index.BrowOuterUp_R] - browInner);
        SetParam(browInner / 0.5f, UnifiedExpressions.BrowInnerUpLeft);
        SetParam(browInner / 0.5f, UnifiedExpressions.BrowInnerUpRight);
        SetParam(browOuterLeft / 0.5f, UnifiedExpressions.BrowOuterUpLeft);
        SetParam(browOuterRight / 0.5f, UnifiedExpressions.BrowOuterUpRight);
        #endregion

        #region Eye
        var leftSquint = pShape[(int)BlendShape.Index.EyeSquint_L];
        var rightSquint = pShape[(int)BlendShape.Index.EyeSquint_R];
        var blinkSquintLeft = MathF.Min(1f, leftBlink / 0.5f);
        var blinkSquintRight = MathF.Min(1f, rightBlink / 0.5f);

        leftSquint = MathF.Max(0f, leftSquint - 0.015f);
        rightSquint = MathF.Max(0f, rightSquint - 0.015f);
        leftSquint *= 10.0f;
        rightSquint *= 10.0f;

        leftSquint *= blinkSquintLeft;
        rightSquint *= blinkSquintRight;

        SetParam(leftSquint, UnifiedExpressions.EyeSquintLeft);
        SetParam(rightSquint, UnifiedExpressions.EyeSquintRight);
        SetParam(pShape[(int)BlendShape.Index.EyeWide_L] / 1.0f, UnifiedExpressions.EyeWideLeft);
        SetParam(pShape[(int)BlendShape.Index.EyeWide_R] / 1.0f, UnifiedExpressions.EyeWideRight);
        #endregion
    }

    protected override void UpdateExpression(float[] pShape)
    {
        #region Values
        var cheekPuff = pShape[(int)BlendShape.Index.CheekPuff];

        var noseSneerLeft = pShape[(int)BlendShape.Index.NoseSneer_L];
        var noseSneerRight = pShape[(int)BlendShape.Index.NoseSneer_R];

        var mouthLowerDownLeft = pShape[(int)BlendShape.Index.MouthLowerDown_L];
        var mouthLowerDownRight = pShape[(int)BlendShape.Index.MouthLowerDown_R];

        var mouthStretchLeft = pShape[(int)BlendShape.Index.MouthStretch_L];
        var mouthStretchRight = pShape[(int)BlendShape.Index.MouthStretch_R];

        var mouthSmileLeft = pShape[(int)BlendShape.Index.MouthSmile_L];
        var mouthSmileRight = pShape[(int)BlendShape.Index.MouthSmile_R];

        var mouthUpperUpLeft = pShape[(int)BlendShape.Index.MouthUpperUp_L];
        var mouthUpperUpRight = pShape[(int)BlendShape.Index.MouthUpperUp_R];

        var mouthPressLeft = pShape[(int)BlendShape.Index.MouthPress_L];
        var mouthPressRight = pShape[(int)BlendShape.Index.MouthPress_R];

        var mouthLeft = pShape[(int)BlendShape.Index.MouthLeft];
        var mouthRight = pShape[(int)BlendShape.Index.MouthRight];
        var mouthLR = Math.Max(mouthLeft, mouthRight);
        var mouthLeftRaw = pShape[(int)BlendShape.Index.MouthLeft];
        var mouthRightRaw = pShape[(int)BlendShape.Index.MouthRight];

        var mouthFrownLeft = pShape[(int)BlendShape.Index.MouthFrown_L];
        var mouthFrownRight = pShape[(int)BlendShape.Index.MouthFrown_R];

        var jawOpen = pShape[(int)BlendShape.Index.JawOpen];
        var jawLeft = pShape[(int)BlendShape.Index.JawLeft];
        var jawRight = pShape[(int)BlendShape.Index.JawRight];
        var jawLeftRaw = pShape[(int)BlendShape.Index.JawLeft];
        var jawRightRaw = pShape[(int)BlendShape.Index.JawRight];

        var mouthShrugLower = pShape[(int)BlendShape.Index.MouthShrugLower];
        var mouthShrugUpper = pShape[(int)BlendShape.Index.MouthShrugUpper];

        var mouthFunnel = pShape[(int)BlendShape.Index.MouthFunnel];
        var mouthPucker = pShape[(int)BlendShape.Index.MouthPucker];
        var mouthFunnelRaw = pShape[(int)BlendShape.Index.MouthFunnel];
        var mouthPuckerRaw = pShape[(int)BlendShape.Index.MouthPucker];

        var mouthRollLower = pShape[(int)BlendShape.Index.MouthRollLower];
        var mouthRollUpper = pShape[(int)BlendShape.Index.MouthRollUpper];

        var tongueOut = pShape[(int)BlendShape.Index.TongueOut];
        var tongueLeft = MathF.Max(mouthLeftRaw, jawLeftRaw);
        var tongueRight = MathF.Max(mouthRightRaw, jawRightRaw);
        #endregion
        #region Fix
        // 各種補正セクション
        // noseSneerは0.6までは動作しないように
        noseSneerLeft = MathF.Max(0f, (noseSneerLeft - 0.6f) / 0.4f);
        noseSneerRight = MathF.Max(0f, (noseSneerRight - 0.6f) / 0.4f);
        // LowerDown補正
        mouthLowerDownLeft = MathF.Pow(Math.Clamp(mouthLowerDownLeft / 0.8f, 0f, 1f), 0.7f);
        mouthLowerDownRight = MathF.Pow(Math.Clamp(mouthLowerDownRight / 0.8f, 0f, 1f), 0.7f);
        // Stretch補正
        mouthStretchLeft = MathF.Pow(Math.Clamp(mouthStretchLeft / 0.6f, 0f, 1f), 1.25f);
        mouthStretchRight = MathF.Pow(Math.Clamp(mouthStretchRight / 0.6f, 0f, 1f), 1.25f);
        // Smileを補正
        mouthSmileLeft /= 0.65f;
        mouthSmileRight /= 0.65f;
        // 妙に偏るので強い方を優先してシンメトリー化
        var mouthSmileSym = MathF.Max(mouthSmileLeft, mouthSmileRight);
        var mouthStretchSym = MathF.Max(mouthStretchLeft, mouthStretchRight);
        // MouthUpperUpシンメトリー化
        var mouthUpperUpSym = MathF.Max(mouthUpperUpLeft, mouthUpperUpRight);
        // MouthPressシンメトリー化
        var mouthPressSym = MathF.Max(mouthPressLeft, mouthPressRight);
        var mouthPressSymRaw = mouthPressSym;
        // MouthLeft/Rightを0.25までは動作しないように
        mouthLeft = mouthLeft / 0.65f;
        mouthRight = mouthRight / 0.65f;
        mouthLeft = MathF.Max(0f, (mouthLeft - 0.25f) / 0.75f);
        mouthRight = MathF.Max(0f, (mouthRight - 0.25f) / 0.75f);
        // Frownシンメトリー化
        var mouthFrownSym = MathF.Max(mouthFrownLeft, mouthFrownRight);
        // Frownを0.333fまでは動作しないようにして正規化、中央辺りから動作し出すように補正
        mouthFrownSym = MathF.Pow(Math.Clamp((Math.Clamp((mouthFrownSym - 0.12f) / 0.08f, 0f, 1f)) / 0.6f, 0f, 1f), 1.35f) / 0.65f;
        // jawOpenは0.05までは動作しないようにしつつ、補正
        jawOpen = MathF.Max(0f, (jawOpen - 0.05f) / 0.95f) / 0.75f;
        // Shrugを0.5まで無効化
        mouthShrugLower = MathF.Pow(Math.Clamp((mouthShrugLower - 0.5f) / 0.2f, 0f, 1f), 2.0f);
        mouthShrugUpper = MathF.Pow(Math.Clamp((mouthShrugUpper - 0.5f) / 0.2f, 0f, 1f), 2.0f);
        // Pucker、Funnel中はShrugやSmile、Press、NoseSneerを無効化
        var rollAmount = MathF.Max(mouthRollUpper,mouthRollLower);
        if (rollAmount > 0.3f ||
            mouthPucker > 0.4f ||
            mouthFunnel > 0.1f)
        {
            mouthShrugLower = 0f;
            mouthShrugUpper = 0f;
            mouthSmileSym = 0f;
            mouthPressSym = 0f;
            noseSneerLeft = 0f;
            noseSneerRight = 0f;
        }

        // Smileがある程度弱くStretchが強いときはSmileとUpperUpを減衰
        if (mouthSmileSym <= 0.4f &&
            mouthStretchSym > 0.05f)
        {
            var smileScale = MathF.Max(0.2f, 1f - mouthSmileSym);
            mouthSmileSym *= smileScale; // Smileを減衰

            var upperUpScale = MathF.Max(0.3f, 1f - mouthUpperUpSym);
            mouthUpperUpSym *= upperUpScale;    // UpperUpも減衰
        }
        // UpperUpがある程度強い場合はStretchを減衰
        if (mouthUpperUpSym > 0.3f)
        {
            var stretchScale = MathF.Max(0.2f, 1f - mouthUpperUpSym);
            mouthStretchSym *= stretchScale;
        }
        // Shrugが入るときもSmileを無効
        if (mouthShrugLower > 0.3f)
        {
            mouthSmileSym = 0f;
        }
        // Pressが入るときに僅かに入っているUpperUpを無効化
        if (mouthPressSym > 0.1f)
        {
            mouthUpperUpSym = 0f;
        }

        // 口を開くだけでFrownが入るため、Stretchが入っていない場合は減衰
        if (jawOpen > 0.05f && mouthStretchSym < 0.5f)
        {
            mouthFrownSym *= 0.05f;
        }
        // Smile時にFrownを減衰
        if (mouthSmileSym > 0.5f)
        {
            var frownScale = MathF.Max(0.2f, 1f - mouthFrownSym);
            mouthFrownSym *= frownScale;
        }
        
        // MouthLeft/Rightが十分入るときにFrownを減衰
        if (mouthLR > 0.025f)
        {
            mouthFrownSym *= 1f - Math.Clamp((mouthLR - 0.025f) / 0.175f, 0f, 1f);
        }
        // Stretchが0.5fを超えたらFrownに加算してSmileを減衰
        else if (mouthStretchSym > 0.5f)
        {
            mouthSmileSym *= 0.5f;
            mouthFrownSym += mouthStretchSym;
        }
        // Funnelが検出されにくいため、Puckerが強くPressが弱いときにFunnelへ加算
        var funnelBlend = Math.Clamp((0.09f - mouthPressSymRaw) / 0.09f, 0f, 1f);
        mouthFunnel = Math.Clamp(mouthFunnelRaw + mouthPuckerRaw * funnelBlend, 0f, 1f);
        mouthFunnel = Math.Clamp(mouthFunnel / 0.8f, 0f, 1f);
        mouthPucker *= 1f - funnelBlend;

        // Shrug検出時はPuckerを無効化
        if (mouthShrugLower > 0f)
        {
            mouthPucker = 0f;
        }
        // Pucker補正
        mouthPucker /= 0.8f;
        // LipSuckが入るときにSmileを無効化
        if (mouthRollLower >= 0.2f)
        {
            mouthSmileSym = 0f;
        }
        // SmileとLipSuckでSlantを計算（なんだっけこの処理？）
        var mouthSlantOutput = MathF.Max(0f, mouthSmileSym - mouthRollLower);

        // LipPuckerをMouthTightenerに変換
        var mouthTightener = mouthPucker / 4.0f;

        // TongueOut関連
        if (tongueOut > 0.75f && mouthStretchSym < 0.25f)
        {
            _tongueDetected = true;
        }
        if (_tongueDetected)
        {
            tongueOut = 1.0f;
            mouthLeft = mouthLeft / 4.0f;
            mouthRight = mouthRight / 4.0f;
            jawLeft = jawLeft / 4.0f;
            jawRight = jawRight / 4.0f;
            mouthStretchLeft = 0f;
            mouthStretchRight = 0f;
            if (tongueLeft < 0.05f && tongueRight < 0.05f || jawOpen < 0.2f)
            {
                _tongueDetected = false;
            }
        }
        if (!_tongueDetected)
        {
            tongueOut = 0f;
            tongueLeft = 0f;
            tongueRight = 0f;
        }
        #endregion

        #region Jaw
        SetParam(jawOpen, UnifiedExpressions.JawOpen);
        SetParam(jawLeft / 0.5f, UnifiedExpressions.JawLeft); // 口を開けたまま左に大きく動かせるように補正
        SetParam(jawRight / 0.5f, UnifiedExpressions.JawRight); // 口を開けたまま右に大きく動かせるように補正
        SetParam(pShape, BlendShape.Index.JawForward, UnifiedExpressions.JawForward);
        SetParam(pShape[(int)BlendShape.Index.MouthClose] / 0.65f, UnifiedExpressions.MouthClosed);
        #endregion

        #region Cheek
        SetParam(pShape, BlendShape.Index.CheekSquint_L, UnifiedExpressions.CheekSquintLeft);
        SetParam(pShape, BlendShape.Index.CheekSquint_R, UnifiedExpressions.CheekSquintRight);

        var cheekMouthLeft = SmoothValue(pShape[(int)BlendShape.Index.MouthLeft], ref _lastMouthLeft);
        var cheekMouthRight = SmoothValue(pShape[(int)BlendShape.Index.MouthRight], ref _lastMouthRight);

        const float diffThreshold = 0.1f;

        if (cheekPuff > 0.1f)
        {
            if (cheekMouthLeft > cheekMouthRight + diffThreshold)
            {
                SetParam(cheekPuff, UnifiedExpressions.CheekPuffLeft);
                SetParam(cheekPuff + cheekMouthLeft, UnifiedExpressions.CheekPuffRight);
            }
            else if (cheekMouthRight > cheekMouthLeft + diffThreshold)
            {
                SetParam(cheekPuff + cheekMouthRight, UnifiedExpressions.CheekPuffLeft);
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
        SetParam(noseSneerLeft, UnifiedExpressions.NoseSneerLeft);
        SetParam(noseSneerRight, UnifiedExpressions.NoseSneerRight);
        #endregion

        #region Mouth
        SetParam(mouthUpperUpSym, UnifiedExpressions.MouthUpperUpLeft);
        SetParam(mouthUpperUpSym, UnifiedExpressions.MouthUpperUpRight);

        SetParam(mouthFrownSym, UnifiedExpressions.MouthFrownLeft);
        SetParam(mouthFrownSym, UnifiedExpressions.MouthFrownRight);

        SetParam(mouthShrugLower, UnifiedExpressions.MouthRaiserLower);
        SetParam(mouthShrugUpper, UnifiedExpressions.MouthRaiserUpper);
        
        SetParam(mouthStretchLeft, UnifiedExpressions.MouthStretchLeft);
        SetParam(mouthStretchRight, UnifiedExpressions.MouthStretchRight);
        SetParam(mouthSmileSym, UnifiedExpressions.MouthCornerPullLeft);
        SetParam(mouthSlantOutput, UnifiedExpressions.MouthCornerSlantLeft);
        SetParam(mouthSmileSym, UnifiedExpressions.MouthCornerPullRight);
        SetParam(mouthSlantOutput, UnifiedExpressions.MouthCornerSlantRight);
        SetParam(mouthLowerDownLeft, UnifiedExpressions.MouthLowerDownLeft);
        SetParam(mouthLowerDownRight, UnifiedExpressions.MouthLowerDownRight);
        SetParam(pShape, BlendShape.Index.MouthDimple_L, UnifiedExpressions.MouthDimpleLeft);
        SetParam(pShape, BlendShape.Index.MouthDimple_R, UnifiedExpressions.MouthDimpleRight);
        SetParam(mouthLeft, UnifiedExpressions.MouthUpperLeft);
        SetParam(mouthLeft, UnifiedExpressions.MouthLowerLeft);
        SetParam(mouthRight, UnifiedExpressions.MouthUpperRight);
        SetParam(mouthRight, UnifiedExpressions.MouthLowerRight);
        SetParam(mouthPressSym, UnifiedExpressions.MouthPressLeft);
        SetParam(mouthPressSym, UnifiedExpressions.MouthPressRight);
        SetParam(mouthTightener, UnifiedExpressions.MouthTightenerLeft);
        SetParam(mouthTightener, UnifiedExpressions.MouthTightenerRight);
        #endregion

        #region Lip
        SetParam(mouthFunnel, UnifiedExpressions.LipFunnelUpperLeft);
        SetParam(mouthFunnel, UnifiedExpressions.LipFunnelUpperRight);
        SetParam(mouthFunnel, UnifiedExpressions.LipFunnelLowerLeft);
        SetParam(mouthFunnel, UnifiedExpressions.LipFunnelLowerRight);
        SetParam(mouthPucker, UnifiedExpressions.LipPuckerUpperLeft);
        SetParam(mouthPucker, UnifiedExpressions.LipPuckerUpperRight);
        SetParam(mouthPucker, UnifiedExpressions.LipPuckerLowerLeft);
        SetParam(mouthPucker, UnifiedExpressions.LipPuckerLowerRight);
        SetParam(pShape, BlendShape.Index.MouthRollUpper, UnifiedExpressions.LipSuckUpperLeft);
        SetParam(pShape, BlendShape.Index.MouthRollUpper, UnifiedExpressions.LipSuckUpperRight);
        SetParam(pShape, BlendShape.Index.MouthRollLower, UnifiedExpressions.LipSuckLowerLeft);
        SetParam(pShape, BlendShape.Index.MouthRollLower, UnifiedExpressions.LipSuckLowerRight);
        #endregion

        #region Tongue
        SetParam(tongueOut, UnifiedExpressions.TongueOut);
        SetParam(tongueLeft / 0.5f, UnifiedExpressions.TongueLeft);
        SetParam(tongueRight / 0.5f, UnifiedExpressions.TongueRight);
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