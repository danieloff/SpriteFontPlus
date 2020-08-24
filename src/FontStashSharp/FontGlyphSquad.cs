using System.Runtime.InteropServices;

namespace FontStashSharp
{
	[StructLayout(LayoutKind.Sequential)]
	struct FontGlyphSquad
	{
		public double X0;
		public double Y0;
		public double S0;
		public double T0;
		public double X1;
		public double Y1;
		public double S1;
		public double T1;
	}
}
