using System;
using System.Windows;
using System.Windows.Media;

namespace Phinite
{
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

		public static bool Intersects(this LineGeometry thisLine, LineGeometry line)
		{
			var p11 = thisLine.StartPoint;
			var p12 = thisLine.EndPoint;
			var p21 = line.StartPoint;
			var p22 = line.EndPoint;

			return Intersects(p11, p12, p21, p22);
		}

		public static bool Intersects(Point p11, Point p12, Point p21, Point p22)
		{
			var p1Min = new Point(Math.Min(p11.X, p12.X), Math.Min(p11.Y, p12.Y));
			var p1Max = new Point(Math.Max(p11.X, p12.X), Math.Max(p11.Y, p12.Y));

			var p1MinX = p1Min.X == p11.X ? p11 : p12;
			var p1MinY = p1Min.Y == p11.Y ? p11 : p12;
			var p1MaxX = p1Max.X == p11.X ? p11 : p12;
			var p1MaxY = p1Max.Y == p11.Y ? p11 : p12;

			var p2Min = new Point(Math.Min(p21.X, p22.X), Math.Min(p21.Y, p22.Y));
			var p2Max = new Point(Math.Max(p21.X, p22.X), Math.Max(p21.Y, p22.Y));

			if (p2Min.X > p1Max.X || p2Max.X < p1Min.X || p2Min.Y > p1Max.Y || p2Max.Y < p1Min.Y)
				return false;

			if (p21.X > p1MinX.X && p21.X < p1MaxX.X)
			{
				double ratio = (p1MaxX.Y - p1MinX.Y) / (p1MaxX.X - p1MinX.X);

				double d1 = p1MaxX.X - p21.X;
				bool below = p21.Y > p1MaxX.Y - d1 * ratio;

				double d2 = p1MaxX.X - p22.X;
				bool above = p22.Y < p1MaxX.Y - d2 * ratio;

				if (below != above)
					return false;

				if (p22.X >= p1MinX.X && p22.X <= p1MaxX.X)
				{
					return true;
				}
				else if (p22.X < p1MinX.X)
				{
					double ratioAlt = (p21.Y - p1MinX.Y) / (p21.X - p1MinX.X);

					double dAlt = p21.X - p22.X;
					bool aboveAlt = p22.Y < p21.Y - dAlt * ratioAlt;

					if (below == aboveAlt)
						return true;
					else
						return false;
				}
				else
				{
					double ratioAlt = (p1MaxX.Y - p21.Y) / (p1MaxX.X - p21.X);

					double dAlt = p1MaxX.X - p22.X;
					bool aboveAlt = p22.Y < p1MaxX.Y - dAlt * ratioAlt;

					if (below == aboveAlt)
						return true;
					else
						return false;
				}
			}

			if (p21.Y > p1MinY.Y && p21.Y < p1MaxY.Y)
			{
				double ratio = (p1MaxY.X - p1MinY.X) / (p1MaxY.Y - p1MinY.Y);

				double d1 = p1MaxY.Y - p21.Y;
				bool toRight = p21.X > p1MaxY.X - d1 * ratio;

				double d2 = p1MaxY.Y - p22.Y;
				bool toLeft = p22.X < p1MaxY.X - d2 * ratio;

				if (toRight != toLeft)
					return false;

				if (p22.Y >= p1MinY.Y && p22.Y <= p1MaxY.Y)
				{
					return true;
				}
				else if (p22.Y < p1MinY.Y)
				{
					double ratioAlt = (p21.X - p1MinY.X) / (p21.Y - p1MinY.Y);

					double dAlt = p21.Y - p22.Y;
					bool toLeftAlt = p22.X < p21.X - dAlt * ratioAlt;

					if (toRight == toLeftAlt)
						return true;
					else
						return false;
				}
				else
				{
					double ratioAlt = (p1MaxY.X - p21.X) / (p1MaxY.Y - p21.Y);

					double dAlt = p1MaxY.Y - p22.Y;
					bool toLeftAlt = p22.X < p1MaxX.X - dAlt * ratioAlt;

					if (toRight == toLeftAlt)
						return true;
					else
						return false;
				}
			}

			if (p22.X > p1MinX.X && p22.X < p1MaxX.X)
			{
				double ratio = (p1MaxX.Y - p1MinX.Y) / (p1MaxX.X - p1MinX.X);

				double d2 = p1MaxX.X - p22.X;
				bool below = p22.Y > p1MaxX.Y - d2 * ratio;

				double d1 = p1MaxX.X - p21.X;
				bool above = p21.Y < p1MaxX.Y - d1 * ratio;

				if (below != above)
					return false;

				if (p21.X >= p1MinX.X && p21.X <= p1MaxX.X)
				{
					return true;
				}
				else if (p21.X < p1MinX.X)
				{
					double ratioAlt = (p22.Y - p1MinX.Y) / (p22.X - p1MinX.X);

					double dAlt = p22.X - p21.X;
					bool aboveAlt = p21.Y < p22.Y - dAlt * ratioAlt;

					if (below == aboveAlt)
						return true;
					else
						return false;
				}
				else
				{
					double ratioAlt = (p1MaxX.Y - p22.Y) / (p1MaxX.X - p22.X);

					double dAlt = p1MaxX.X - p21.X;
					bool aboveAlt = p21.Y < p1MaxX.Y - dAlt * ratioAlt;

					if (below == aboveAlt)
						return true;
					else
						return false;
				}
			}

			if (p22.Y > p1MinY.Y && p22.Y < p1MaxY.Y)
			{
				double ratio = (p1MaxY.X - p1MinY.X) / (p1MaxY.Y - p1MinY.Y);

				double d2 = p1MaxY.Y - p22.Y;
				bool toRight = p22.X > p1MaxY.X - d2 * ratio;

				double d1 = p1MaxY.Y - p21.Y;
				bool toLeft = p21.X < p1MaxY.X - d1 * ratio;

				if (toRight != toLeft)
					return false;

				if (p21.Y >= p1MinY.Y && p21.Y <= p1MaxY.Y)
				{
					return true;
				}
				else if (p21.Y < p1MinY.Y)
				{
					double ratioAlt = (p22.X - p1MinY.X) / (p22.Y - p1MinY.Y);

					double dAlt = p22.Y - p21.Y;
					bool toLeftAlt = p21.X < p22.X - dAlt * ratioAlt;

					if (toRight == toLeftAlt)
						return true;
					else
						return false;
				}
				else
				{
					double ratioAlt = (p1MaxY.X - p22.X) / (p1MaxY.Y - p22.Y);

					double dAlt = p1MaxY.Y - p21.Y;
					bool toLeftAlt = p21.X < p1MaxX.X - dAlt * ratioAlt;

					if (toRight == toLeftAlt)
						return true;
					else
						return false;
				}
			}

			return Intersects(p21, p22, p11, p12);

			//var p2MinX = p2Min.X == p21.X ? p21 : p22;
			//var p2MinY = p2Min.Y == p21.Y ? p21 : p22;
			//var p2MaxX = p2Max.X == p21.X ? p21 : p22;
			//var p2MaxY = p2Max.Y == p21.Y ? p21 : p22;

			//if ((p1MinX.Y > p1MaxX.Y && p2MinX.Y < p2MaxX.Y) || (p1MinX.Y < p1MaxX.Y && p2MinX.Y > p2MaxX.Y))
			//	return true;

			//if (p21.X <= p1MinX.X)
			//{
				//if(p21.Y <= p1MinX.Y)
			//}
			//else if (p21.X >= p1Max.X)
			//{
			//}

			//throw new NotImplementedException("Not all cases of line intersection are handled");
		}

	}

}
