using Microsoft.Xna.Framework;
using System;

namespace MajorasTerraria.API {
	// <summary>
	///     A struct for a color represented using the Hue-Saturation-Value format.
	/// </summary>
	public partial struct HSVA {
		public double H;
		public double S;
		public double V;
		public byte A;

		public const double HueRed = 0d;
		public const double HueOrange = 30d;
		public const double HueYellow = 60d;
		public const double HueLimeGreen = 90d;
		public const double HueGreen = 120d;
		public const double HueTeal = 150d;
		public const double HueCyan = 180d;
		public const double HueLightBlue = 210d;
		public const double HueBlue = 240d;
		public const double HuePurple = 270d;
		public const double HueMagenta = 300d;
		public const double HueHotPink = 330d;
		public const double HueRed2 = 360d;

		public HSVA(double h, double s, double v, byte a) {
			if (h > 360 || h < 0 || s > 1 || s < 0 || v > 1 || v < 0 || a > 255 || a < 0)
				throw new BadHSVAValuesException(
					$"Given constructor parameters contained invalid values: {{{h}, {s}, {v}, {a}}}");
			H = h;
			S = s;
			V = v;
			A = a;
		}

		public override string ToString() {
			return $"{{H: {H}, S: {S}, V: {V}, A:{A}}}";
		}

		public Color ToColor() {
			return new(ToColorVector4());
		}

		public Vector3 ToColorVector3() {
			H %= 360f;

			double chroma = V * S;

			double h = H / 60d;
			double x = chroma * (1 - Math.Abs((h % 2) - 1));
			double r, g, b;

			//Possible if R > B > G
			if (x < 0)
				x += 6;

			if (0 <= h && h <= 1) {
				r = chroma;
				g = x;
				b = 0;
			} else if (1 < h && h <= 2) {
				r = x;
				g = chroma;
				b = 0;
			} else if (2 < h && h <= 3) {
				r = 0;
				g = chroma;
				b = x;
			} else if (3 < h && h <= 4) {
				r = 0;
				g = x;
				b = chroma;
			} else if (4 < h && h <= 5) {
				r = x;
				g = 0;
				b = chroma;
			} else if (5 < h && h <= 6) {
				r = chroma;
				g = 0;
				b = x;
			} else {
				r = 0;
				g = 0;
				b = 0;
			}

			double m = V - chroma;

			return new Vector3((float)(r + m), (float)(g + m), (float)(b + m));
		}

		public Vector4 ToColorVector4() {
			return new(ToColorVector3(), A / 255f);
		}
	}

	public class BadHSVAValuesException : Exception {
		public BadHSVAValuesException(string message) : base(message) { }
	}
}
