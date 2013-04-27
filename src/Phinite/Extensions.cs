﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Phinite
{
	/// <summary>
	/// Various custom extension methods used throughout Phinite.
	/// </summary>
	public static class Extensions
	{

		public static readonly double RadiansToDegrees = 180.0 / Math.PI;

		public static readonly double DegreesToRadians = Math.PI / 180;

		/// <summary>
		/// Makes a copy of this point.
		/// </summary>
		/// <param name="thisPoint"></param>
		/// <returns></returns>
		public static Point Copy(this Point thisPoint)
		{
			return new Point(thisPoint.X, thisPoint.Y);
		}

		public static Point MoveTo(this Point thisPoint, double angle, double distance)
		{
			double radiansAngle = angle * DegreesToRadians;
			//Point target = new Point();

			thisPoint.X += distance * Math.Sin(radiansAngle);
			thisPoint.Y -= distance * Math.Cos(radiansAngle);

			//return thisPoint.MoveTo(target, distance);
			return thisPoint;
		}

		public static Point MoveTo(this Point thisPoint, Point target, double distance)
		{
			double distX = target.X - thisPoint.X;
			double distY = target.Y - thisPoint.Y;

			if (distY == 0)
			{
				if (distX > 0)
					thisPoint.X += distance;
				else
					thisPoint.X -= distance;
			}
			else if (distX == 0)
			{
				if (distY > 0)
					thisPoint.Y += distance;
				else
					thisPoint.Y -= distance;
			}
			else
			{
				double ratio = distX / distY;

				double moveY = Math.Sqrt(Math.Pow(distance, 2) / (Math.Pow(ratio, 2) + 1));

				if (distY > 0)
					thisPoint.Y += moveY;
				else
					thisPoint.Y -= moveY;

				double moveX = Math.Abs(moveY * ratio);

				if (distX > 0)
					thisPoint.X += moveX;
				else
					thisPoint.X -= moveX;
			}

			return thisPoint;
		}

		/// <summary>
		/// Computes angle from this point to some other point. If the point are at the same location, <code>Double.NaN</code> is returned.
		/// </summary>
		/// <param name="thisPoint"></param>
		/// <param name="point"></param>
		/// <returns>angle in degrees</returns>
		public static double Angle(this Point thisPoint, Point point)
		{
			if (thisPoint.X == point.X)
			{
				if (thisPoint.Y < point.Y)
					return 180.0;
				if (thisPoint.Y > point.Y)
					return 0.0;
				return Double.NaN;
			}
			if (thisPoint.Y == point.Y)
			{
				if (thisPoint.X < point.X)
					return 90.0;
				if (thisPoint.X > point.X)
					return 270.0;
				return Double.NaN;
			}

			double distX = point.X - thisPoint.X;
			double distY = point.Y - thisPoint.Y;
			//double dist = thisPoint.Distance(point);
			//double ratio = distY / dist;

			//double degrees = Math.Atan(ratio) * RadiansToDegrees;
			double degrees = Math.Atan2(distY, distX) * RadiansToDegrees + 90;

			if (degrees < 0)
				degrees += 360;

			return degrees;
		}

		/// <summary>
		/// Computes the Euclidean distance between points.
		/// </summary>
		/// <param name="thisPoint"></param>
		/// <param name="point"></param>
		/// <returns></returns>
		public static double Distance(this Point thisPoint, Point point)
		{
			return Math.Sqrt(Math.Pow(point.X - thisPoint.X, 2)
				+ Math.Pow(point.Y - thisPoint.Y, 2));
		}

		/// <summary>
		/// Computes dot product of two vectors, AB * BC, where: A = this Point, B = point1, C = point2.
		/// </summary>
		/// <param name="thisPoint"></param>
		/// <param name="point1"></param>
		/// <param name="point2"></param>
		/// <returns></returns>
		public static double DotProduct(this Point thisPoint, Point point1, Point point2)
		{
			return (point1.X - thisPoint.X) * (point2.X - point1.X)
				+ (point1.Y - thisPoint.Y) * (point2.Y - point1.Y);
		}

		/// <summary>
		/// Computes cross product of two vectors, AB x AC, where: A = this Point, B = point1, C = point2.
		/// </summary>
		/// <param name="thisPoint"></param>
		/// <param name="point1"></param>
		/// <param name="point2"></param>
		/// <returns></returns>
		public static double CrossProduct(this Point thisPoint, Point point1, Point point2)
		{
			return (point1.X - thisPoint.X) * (point2.Y - thisPoint.Y)
				- (point1.Y - thisPoint.Y) * (point2.X - thisPoint.X);
		}

		/// <summary>
		/// Computes Euclidean distance from this point to a given line.
		/// </summary>
		/// <param name="thisPoint"></param>
		/// <param name="lineStart"></param>
		/// <param name="lineEnd"></param>
		/// <param name="segment"></param>
		/// <returns></returns>
		public static double DistanceToLine(this Point thisPoint, Point lineStart, Point lineEnd,
			bool segment = true)
		{
			if (segment)
			{
				if (lineStart.DotProduct(lineEnd, thisPoint) > 0)
					return thisPoint.Distance(lineEnd);
				if (lineEnd.DotProduct(lineStart, thisPoint) > 0)
					return thisPoint.Distance(lineStart);
			}
			return Math.Abs(lineStart.CrossProduct(lineEnd, thisPoint) / lineStart.Distance(lineEnd));
		}

		public static bool Intersects(this LineGeometry thisLine, LineGeometry line)
		{
			var p11 = thisLine.StartPoint;
			var p12 = thisLine.EndPoint;
			var p21 = line.StartPoint;
			var p22 = line.EndPoint;

			return CheckIfIntersects(p11, p12, p21, p22);
		}

		public static bool Intersects(this Point thisPoint, Point endPoint, Point otherStartPoint, Point otherEndPoint)
		{
			return CheckIfIntersects(thisPoint, endPoint, otherStartPoint, otherEndPoint);
		}

		private static bool CheckBoundsX(Point p1, Point p2, Point min, Point max)
		{
			// preliminary bounds are checked outside
			//if (p1.X <= min.X || p1.X >= max.X)
			//	return null;

			double ratio = (max.Y - min.Y) / (max.X - min.X);

			double d1 = max.Y - (max.X - p1.X) * ratio;
			bool below = p1.Y > d1;

			double d2 = max.Y - (max.X - p2.X) * ratio;
			bool above = p2.Y < d2;

			if (below != above)
				return false;

			if (p2.Equals(min) || p2.Equals(max))
			{
				// TODO: case that one endpoint is equal, but other lays on edge
				return false;
			}
			else if (p2.X >= min.X && p2.X <= max.X)
			{
				return true;
			}
			else if (p2.X < min.X)
			{
				double ratioAlt = (p1.Y - min.Y) / (p1.X - min.X);

				double dAlt = p1.Y - (p1.X - p2.X) * ratioAlt;
				bool aboveAlt = p2.Y < dAlt;

				if (below == aboveAlt)
					return true;
				else
					return false;
			}
			else
			{
				double ratioAlt = (max.Y - p1.Y) / (max.X - p1.X);

				double dAlt = max.Y - (max.X - p2.X) * ratioAlt;
				bool aboveAlt = p2.Y < dAlt;

				if (below == aboveAlt)
					return true;
				else
					return false;
			}

			throw new ArgumentException("please checked preliminary bounds outside of this method");
		}

		private static bool CheckBoundsY(Point p1, Point p2, Point min, Point max)
		{
			// TODO: thic can probably be merged with CheckBoundsX

			double ratio = (max.X - min.X) / (max.Y - min.Y);

			double d1 = max.Y - p1.Y;
			bool toRight = p1.X > max.X - d1 * ratio;

			double d2 = max.Y - p2.Y;
			bool toLeft = p2.X < max.X - d2 * ratio;

			if (toRight != toLeft)
				return false;

			if (p2.Equals(min) || p2.Equals(max))
			{
				return false;
			}
			else if (p2.Y >= min.Y && p2.Y <= max.Y)
			{
				return true;
			}
			else if (p2.Y < min.Y)
			{
				double ratioAlt = (p1.X - min.X) / (p1.Y - min.Y);

				double dAlt = p1.Y - p2.Y;
				bool toLeftAlt = p2.X < p1.X - dAlt * ratioAlt;

				if (toRight == toLeftAlt)
					return true;
				else
					return false;
			}
			else
			{
				double ratioAlt = (max.X - p1.X) / (max.Y - p1.Y);

				double dAlt = max.Y - p2.Y;
				bool toLeftAlt = p2.X < max.X - dAlt * ratioAlt;

				if (toRight == toLeftAlt)
					return true;
				else
					return false;
			}

			throw new ArgumentException("please checked preliminary bounds outside of this method");
		}

		private static bool CheckIfIntersects(Point p11, Point p12, Point p21, Point p22)
		{
			var p1Min = new Point(Math.Min(p11.X, p12.X), Math.Min(p11.Y, p12.Y));
			var p1Max = new Point(Math.Max(p11.X, p12.X), Math.Max(p11.Y, p12.Y));

			var p2Min = new Point(Math.Min(p21.X, p22.X), Math.Min(p21.Y, p22.Y));
			var p2Max = new Point(Math.Max(p21.X, p22.X), Math.Max(p21.Y, p22.Y));

			if (p2Min.X > p1Max.X || p2Max.X < p1Min.X || p2Min.Y > p1Max.Y || p2Max.Y < p1Min.Y)
				return false;

			// TODO: DRY this code...

			var p1MinX = p1Min.X == p11.X ? p11 : p12;
			var p1MaxX = p1Max.X == p11.X ? p11 : p12;

			if (p21.X > p1MinX.X && p21.X < p1MaxX.X)
				return CheckBoundsX(p21, p22, p1MinX, p1MaxX);

			if (p22.X > p1MinX.X && p22.X < p1MaxX.X)
				return CheckBoundsX(p22, p21, p1MinX, p1MaxX);

			var p1MinY = p1Min.Y == p11.Y ? p11 : p12;
			var p1MaxY = p1Max.Y == p11.Y ? p11 : p12;

			if (p21.Y > p1MinY.Y && p21.Y < p1MaxY.Y)
				return CheckBoundsY(p21, p22, p1MinY, p1MaxY);

			if (p22.Y > p1MinY.Y && p22.Y < p1MaxY.Y)
				return CheckBoundsY(p22, p21, p1MinY, p1MaxY);

			var p2MinX = p2Min.X == p21.X ? p21 : p22;
			var p2MaxX = p2Max.X == p21.X ? p21 : p22;

			if (p11.X > p2MinX.X && p11.X < p2MaxX.X)
				return CheckBoundsX(p11, p12, p2MinX, p2MaxX);

			if (p12.X > p2MinX.X && p12.X < p2MaxX.X)
				return CheckBoundsX(p12, p11, p2MinX, p2MaxX);

			var p2MinY = p2Min.Y == p21.Y ? p21 : p22;
			var p2MaxY = p2Max.Y == p21.Y ? p21 : p22;

			if (p11.Y > p2MinY.Y && p11.Y < p2MaxY.Y)
				return CheckBoundsX(p11, p12, p2MinY, p2MaxY);

			if (p12.Y > p2MinY.Y && p12.Y < p2MaxY.Y)
				return CheckBoundsX(p12, p11, p2MinY, p2MaxY);

			if ((p11.Equals(p21) && p12.Equals(p22)) || (p11.Equals(p22) && p12.Equals(p21)))
				return true;

			//if (p11.Equals(p21) || p11.Equals(p22) || p12.Equals(p21) || p12.Equals(p22))
			//	return true;

			return false;

			//throw new NotImplementedException("Not all cases of line intersection are handled");
		}

		public static Point FindIntersection(this Point thisPoint, Point endPoint,
			Point otherLineStartPoint, Point otherLineEndPoint, bool intersectionExistsForSure)
		{
			if(intersectionExistsForSure || thisPoint.Intersects(endPoint, otherLineStartPoint, otherLineEndPoint))
				return thisPoint.Copy().MoveTo(endPoint, thisPoint.DistanceToLine(otherLineStartPoint, otherLineEndPoint, false));

			throw new InvalidOperationException("these lines do not intersect");
		}

	}
}
