using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TopoHelper.Autocad;
using TopoHelper.Model.Results;

namespace TopoHelper.Model.Calculations
{
    public static class SurveyCorrecting
    {
        #region Private Fields

        private static readonly object _AcadLock = 1;
        private static readonly object _ArrayLock = 1;

        #endregion

        #region Internal Methods

        internal static IEnumerable<CalculateDisplacementSectionResult> CalculateDisplacement(IEnumerable<Point3d> leftRailPoints, IEnumerable<Point3d> rightRailPoints)
        {
            var itemCount = leftRailPoints.Count();
            var result = new CalculateDisplacementSectionResult[itemCount];

            Parallel.For(0, itemCount, (i) =>
            //for (int i = 0; i < itemCount; i++)
            {
                var rrp = rightRailPoints.ElementAt(i);
                var lrp = leftRailPoints.ElementAt(i);

                var h = Math.Abs(rrp.Z - lrp.Z);
                var gauge = rrp.DistanceTo(lrp);
                // no need to calculate if there is no cant
                if (h <= Properties.Settings.Default.CSD_MIN_CANT_VAL)
                {
                    lock (_ArrayLock)
                        result[i] = new CalculateDisplacementSectionResult
                        {
                            LeftRailPoint = lrp,
                            RightRailPoint = rrp,
                            OriginalLeftRailPoint = lrp,
                            OriginalRightRailPoint = rrp,
                            Cant = h,
                            Gauge = gauge,
                            DisplacementSectionXY = 0,
                            DisplacementSectionZ = 0
                        };
                    return;
                    //continue;
                }

                // The displacement-point returned is a 2d point that needs to
                // be interpreted whenever returned, the Y value is the
                // displacement in Z 3d-space, the X value is the displacement
                // in the xy-plane, in the direction of the vector between the
                // left and the right rail.

                var displacement = CalculateDisplacementForSurveyCorrecting(h, gauge);
                var zDisp = Math.Abs(displacement.Y);
                var xyDisp = Math.Abs(displacement.X);

                double new_lrp_Z, new_rrp_Z;
                // Low rail needs to go up, high rail needs to go down
                if (lrp.Z < rrp.Z)
                {
                    new_lrp_Z = lrp.Z + zDisp;
                    new_rrp_Z = rrp.Z - zDisp;
                }
                else
                {
                    new_lrp_Z = lrp.Z - zDisp;
                    new_rrp_Z = rrp.Z + zDisp;
                }

                // Calculate new x and y values by using the projected xy line
                using (var line = new Line2d(lrp.T2d(), rrp.T2d()))
                {
                    Point2d projLPoint, projRpoint;
                    lock (_AcadLock)
                    {
                        projLPoint = line.EvaluatePoint(line.GetParameterAtLength(0, -xyDisp, true));
                        projRpoint = line.EvaluatePoint(line.GetParameterAtLength(1, -xyDisp, true));
                    }

                    lock (_ArrayLock)
                        result[i] = new CalculateDisplacementSectionResult
                        {
                            LeftRailPoint = projLPoint.T3d(new_lrp_Z),
                            RightRailPoint = projRpoint.T3d(new_rrp_Z),
                            OriginalLeftRailPoint = lrp,
                            OriginalRightRailPoint = rrp,
                            Gauge = gauge,
                            Cant = h,
                            DisplacementSectionZ = zDisp,
                            DisplacementSectionXY = xyDisp,
                        };
                }
            });

            var chain = 0.0;
            for (int i = 0; i < itemCount; i++)
            {
                // calculate chainage to the previously calculated point in the
                // 2-dimensional space.
                if (i != 0)
                    chain += result[i - 1].LeftRailPoint.T2d()
                        .GetDistanceTo(result[i].LeftRailPoint.T2d());

                // Set chainage on section object
                result[i].Chainage = chain;
            }

            return result;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This function will calculate the displacement needed to rectify the
        /// fault caused by rotating the angle around the lower rail to be able
        /// to measure the tracks cant
        /// </summary>
        /// <param name="h">        
        /// This is the measured cant. In Meters
        /// </param>
        /// <param name="railGauge"> This is the rail gauge. In Meters </param>
        /// <returns> The displacement to execute for rectifying. </returns>
        private static Point CalculateDisplacementForSurveyCorrecting(double h, double railGauge)
        {
            var a = new Point();

            // pt(a) is low rail
            const double RR = 0.0796;

            var c_t = new Point { X = a.X, Y = a.Y - RR };
            var α = Math.Asin(h / railGauge);

            var c = Point.RotatePoint(c_t, a, -α /* Negative for CW */);
            var a_new = Point.RotatePoint(a, c, α /* Positive for CW */);

            return a_new;
        }

        #endregion

        #region Private Classes

        private class Point
        {
            #region Public Fields

            public double X;
            public double Y;

            #endregion

            #region Public Constructors

            public Point()
            {
                X = 0; Y = 0;
            }

            public Point(double X, double Y)
            {
                this.X = X;
                this.Y = Y;
            }

            #endregion

            #region Public Methods

            public static Point RotatePoint(Point pnt, Point origin, double angle)
            {
                var p = new Point { X = pnt.X, Y = pnt.Y };

                // If we rotate need to rotate clockwise angel is negative
                if (angle < 0)
                    angle = (Math.PI * 2) - Math.Abs(angle);

                double s = Math.Sin(angle);
                double c = Math.Cos(angle);
                // translate point back to origin:
                p.X -= origin.X;
                p.Y -= origin.Y;
                // rotate point
                double Xnew = p.X * c - p.Y * s;
                double Ynew = p.X * s + p.Y * c;
                // translate point back:
                p.X = Xnew + origin.X;
                p.Y = Ynew + origin.Y;
                return p;
            }

            public Point Print(string prefix = "", string suffix = "")
            {
                Console.WriteLine($"{prefix}{X},{Y}{suffix}");
                return this;
            }

            #endregion

            #region Internal Methods

            internal static double CalculatePoweredDistance(double x1, double y1, double z1, double x2, double y2, double z2)
            {
                return Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2) + Math.Pow(z2 - z1, 2);
            }

            #endregion
        }

        #endregion
    }
}