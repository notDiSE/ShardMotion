using System;
using OpenCvSharp.Aruco;
using UnityEngine;

namespace ShardMotion
{
    /// <summary>
    /// Used to hold serializable data and for conversion to <see cref="DetectorParameters"/> 
    /// </summary>
    [Serializable]
    public class DetectorParamsData
    {
        public int AdaptiveThreshWinSizeMin = 3;
        public int AdaptiveThreshWinSizeMax = 23;
        public int AdaptiveThreshWinSizeStep = 10;
        public double AdaptiveThreshConstant = 7;
        public double MinMarkerPerimeterRate = 0.03;
        public double MaxMarkerPerimeterRate = 4.0;
        public double PolygonalApproxAccuracyRate = 0.03;
        public double MinCornerDistanceRate = 0.05;
        public int MinDistanceToBorder = 3;
        public double MinMarkerDistanceRate = 0.05;
        public CornerRefineMethod CornerRefinementMethod = CornerRefineMethod.None;
        public int CornerRefinementWinSize = 5;
        public int CornerRefinementMaxIterations = 30;
        public double CornerRefinementMinAccuracy = 0.1;
        public int MarkerBorderBits = 1;
        public int PerspectiveRemovePixelPerCell = 4;
        public double PerspectiveRemoveIgnoredMarginPerCell = 0.13;
        public double MaxErroneousBitsInBorderRate = 0.35;
        public double MinOtsuStdDev = 5.0;
        public double ErrorCorrectionRate = 0.6;
        public bool DetectInvertedMarker = false;
        public bool UseAruco3Detection = false;
        public int MinSideLengthCanonicalImg = 32;
        public float MinMarkerLengthRatioOriginalImg = 0;

        /// <summary>
        /// Converstion
        /// </summary>
        /// <returns><see cref="DetectorParameters"/></returns>
        public DetectorParameters ToDetectorParameters()
        {
            var p = new DetectorParameters();
            p.AdaptiveThreshWinSizeMin = AdaptiveThreshWinSizeMin;
            p.AdaptiveThreshWinSizeMax = AdaptiveThreshWinSizeMax;
            p.AdaptiveThreshWinSizeStep = AdaptiveThreshWinSizeStep;
            p.AdaptiveThreshConstant = AdaptiveThreshConstant;
            p.MinMarkerPerimeterRate = MinMarkerPerimeterRate;
            p.MaxMarkerPerimeterRate = MaxMarkerPerimeterRate;
            p.PolygonalApproxAccuracyRate = PolygonalApproxAccuracyRate;
            p.MinCornerDistanceRate = MinCornerDistanceRate;
            p.MinDistanceToBorder = MinDistanceToBorder;
            p.MinMarkerDistanceRate = MinMarkerDistanceRate;
            p.CornerRefinementMethod = CornerRefinementMethod;
            p.CornerRefinementWinSize = CornerRefinementWinSize;
            p.CornerRefinementMaxIterations = CornerRefinementMaxIterations;
            p.CornerRefinementMinAccuracy = CornerRefinementMinAccuracy;
            p.MarkerBorderBits = MarkerBorderBits;
            p.PerspectiveRemovePixelPerCell = PerspectiveRemovePixelPerCell;
            p.PerspectiveRemoveIgnoredMarginPerCell = PerspectiveRemoveIgnoredMarginPerCell;
            p.MaxErroneousBitsInBorderRate = MaxErroneousBitsInBorderRate;
            p.MinOtsuStdDev = MinOtsuStdDev;
            p.ErrorCorrectionRate = ErrorCorrectionRate;
            p.DetectInvertedMarker = DetectInvertedMarker;
            p.UseAruco3Detection = UseAruco3Detection;
            p.MinSideLengthCanonicalImg = MinSideLengthCanonicalImg;
            p.MinMarkerLengthRatioOriginalImg = MinMarkerLengthRatioOriginalImg;
            return p;
        }

        /// <summary>
        /// Copies detector parameters
        /// </summary>
        /// <param name="p"> copy from</param>
        public void CopyFrom(DetectorParameters p)
        {
            AdaptiveThreshWinSizeMin = p.AdaptiveThreshWinSizeMin;
            AdaptiveThreshWinSizeMax = p.AdaptiveThreshWinSizeMax;
            AdaptiveThreshWinSizeStep = p.AdaptiveThreshWinSizeStep;
            AdaptiveThreshConstant = p.AdaptiveThreshConstant;
            MinMarkerPerimeterRate = p.MinMarkerPerimeterRate;
            MaxMarkerPerimeterRate = p.MaxMarkerPerimeterRate;
            PolygonalApproxAccuracyRate = p.PolygonalApproxAccuracyRate;
            MinCornerDistanceRate = p.MinCornerDistanceRate;
            MinDistanceToBorder = p.MinDistanceToBorder;
            MinMarkerDistanceRate = p.MinMarkerDistanceRate;
            CornerRefinementMethod = p.CornerRefinementMethod;
            CornerRefinementWinSize = p.CornerRefinementWinSize;
            CornerRefinementMaxIterations = p.CornerRefinementMaxIterations;
            CornerRefinementMinAccuracy = p.CornerRefinementMinAccuracy;
            MarkerBorderBits = p.MarkerBorderBits;
            PerspectiveRemovePixelPerCell = p.PerspectiveRemovePixelPerCell;
            PerspectiveRemoveIgnoredMarginPerCell = p.PerspectiveRemoveIgnoredMarginPerCell;
            MaxErroneousBitsInBorderRate = p.MaxErroneousBitsInBorderRate;
            MinOtsuStdDev = p.MinOtsuStdDev;
            ErrorCorrectionRate = p.ErrorCorrectionRate;
            DetectInvertedMarker = p.DetectInvertedMarker;
            UseAruco3Detection = p.UseAruco3Detection;
            MinSideLengthCanonicalImg = p.MinSideLengthCanonicalImg;
            MinMarkerLengthRatioOriginalImg = p.MinMarkerLengthRatioOriginalImg;
        }
    }
}