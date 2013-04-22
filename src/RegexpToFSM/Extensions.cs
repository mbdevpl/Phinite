﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Phinite
{
	public static class Extensions
	{

		public static readonly double RadiansToDegrees = 180.0 / Math.PI;

		public static readonly double DegreesToRadians = Math.PI / 180;

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
		/// Computes angle from this point to some other point. If the point are at the same location, zero is returned.
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
				return 0.0;
			}
			if (thisPoint.Y == point.Y)
			{
				if (thisPoint.X < point.X)
					return 90.0;
				if (thisPoint.X > point.X)
					return 270.0;
				return 0.0;
			}

			double distX = point.X - thisPoint.X;
			double distY = point.Y - thisPoint.Y;
			//double dist = thisPoint.Distance(point);
			//double ratio = distY / dist;

			//double degrees = Math.Atan(ratio) * RadiansToDegrees;
			double degrees = Math.Atan2(distY, distX) * RadiansToDegrees + 90;

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
	}

}
