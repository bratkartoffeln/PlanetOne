namespace PlanetOne.Core
{
    // Fixed solar analogue used by the existing orbit/climate reference model.
    // These are descriptive reference values, not newly simulated stellar evolution.
    public static class StellarReference
    {
        public const string SpectralType="G2 V";
        public const double MassSolar=1,RadiusSolar=1,LuminositySolar=1;
        public const int RadiusKm=695700,EffectiveTemperatureKelvin=5770;
    }
}
