﻿using ViGo.Models.GoogleMaps;
using ViGo.Utilities.Data;

namespace ViGo.Utilities
{
    public static class MapsUtilities
    {
        private static System.Drawing.PointF[] hcmBoundaries = DataFileUtilities.GetHcmCityBoundaries();

        public static bool IsInRegion(System.Drawing.PointF point)
        {
            int result = hcmBoundaries.Zip(hcmBoundaries.Skip(1).Concat(hcmBoundaries),
                (a, b) =>
                {
                    if (a.Y == point.Y && b.Y == point.Y)
                    {
                        return (a.X <= point.X && point.X <= b.X)
                        || (b.X <= point.X && point.X <= a.X)
                        ? 0 : 1;
                    }
                    return a.Y <= b.Y
                        ? point.Y <= a.Y || b.Y < point.Y ? 1 : Math.Sign((a.X - point.X) * (b.Y - point.Y) - (a.Y - point.Y) * (b.X - point.X))
                        : point.Y <= b.Y || a.Y < point.Y ? 1 : Math.Sign((b.X - point.X) * (a.Y - point.Y) - (b.Y - point.Y) * (a.X - point.X));

                }).Aggregate(-1, (r, v) => r * v);

            return (result == 1 || result == 0) ? true : false;
        }

        public static System.Drawing.PointF ToPointF(this GoogleMapPoint googleMapPoint)
        {
            return new System.Drawing.PointF(
                Convert.ToSingle(googleMapPoint.Latitude),
                Convert.ToSingle(googleMapPoint.Longitude));
        }
    }

    //public class MapsPoint
    //{
    //    public double X { get; set; }
    //    public double Y { get; set; }
    //}
}
