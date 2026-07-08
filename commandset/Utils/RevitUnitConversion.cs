using Autodesk.Revit.DB;

namespace RevitMCPCommandSet.Utils
{
    public static class RevitUnitConversion
    {
        public static double ToMillimeters(double internalLength)
        {
#if REVIT2022_OR_GREATER
            return UnitUtils.ConvertFromInternalUnits(internalLength, UnitTypeId.Millimeters);
#else
            return UnitUtils.ConvertFromInternalUnits(internalLength, DisplayUnitType.DUT_MILLIMETERS);
#endif
        }

        public static double ToSquareMeters(double internalArea)
        {
#if REVIT2022_OR_GREATER
            return UnitUtils.ConvertFromInternalUnits(internalArea, UnitTypeId.SquareMeters);
#else
            return UnitUtils.ConvertFromInternalUnits(internalArea, DisplayUnitType.DUT_SQUARE_METERS);
#endif
        }

        public static double ToCubicMeters(double internalVolume)
        {
#if REVIT2022_OR_GREATER
            return UnitUtils.ConvertFromInternalUnits(internalVolume, UnitTypeId.CubicMeters);
#else
            return UnitUtils.ConvertFromInternalUnits(internalVolume, DisplayUnitType.DUT_CUBIC_METERS);
#endif
        }
    }
}
