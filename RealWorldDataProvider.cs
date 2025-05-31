using System;
using System.Collections.Generic;
using System.Linq;

namespace BMSSimulator
{
    /// <summary>
    /// Provides realistic battery load profiles and environmental conditions
    /// Based on real-world usage patterns
    /// </summary>
    public class RealWorldDataProvider
    {
        private readonly Random random = new Random();
        private int timeIndex = 0;
        private List<LoadProfile> loadProfiles;

        public RealWorldDataProvider()
        {
            loadProfiles = new List<LoadProfile>();
            InitializeLoadProfiles();
        }
        
        private void InitializeLoadProfiles()
        {
            loadProfiles = new List<LoadProfile>
            {
                // Electric Vehicle driving pattern
                new LoadProfile
                {
                    Name = "EV City Driving",
                    BasePattern = new double[] { 15, 25, 35, 45, 30, 20, 15, 10, 20, 40, 35, 25, 15, 10, 5 },
                    Description = "Urban electric vehicle usage with stop-and-go traffic"
                },
                
                // Power tool usage
                new LoadProfile
                {
                    Name = "Power Tool",
                    BasePattern = new double[] { 0, 0, 45, 50, 55, 45, 0, 0, 35, 40, 30, 0, 0, 25, 20 },
                    Description = "Intermittent high-current power tool operation"
                },
                
                // Consumer electronics
                new LoadProfile
                {
                    Name = "Smartphone/Tablet",
                    BasePattern = new double[] { 2, 3, 4, 5, 3, 2, 1, 2, 4, 6, 5, 3, 2, 1, 1 },
                    Description = "Typical consumer device usage pattern"
                },
                
                // Energy storage system
                new LoadProfile
                {
                    Name = "Grid Storage",
                    BasePattern = new double[] { 10, 15, 20, 25, 30, 35, 30, 25, 20, 15, 10, 8, 6, 4, 2 },
                    Description = "Grid energy storage discharge pattern"
                },
                
                // Drone/UAV
                new LoadProfile
                {
                    Name = "Drone Flight",
                    BasePattern = new double[] { 0, 8, 12, 15, 18, 20, 18, 15, 12, 10, 8, 6, 4, 2, 0 },
                    Description = "Drone flight mission profile"
                }
            };
        }
        
        /// <summary>
        /// Get realistic current draw based on selected load profile
        /// </summary>
        public double GetRealisticCurrent(string profileName, double baseCurrentMultiplier = 1.0)
        {
            var profile = loadProfiles.FirstOrDefault(p => p.Name == profileName) 
                         ?? loadProfiles[0]; // Default to first profile
            
            int patternIndex = timeIndex % profile.BasePattern.Length;
            double baseCurrent = profile.BasePattern[patternIndex] * baseCurrentMultiplier;
            
            // Add realistic noise and variations (±15%)
            double noise = (random.NextDouble() - 0.5) * 0.3; // ±15% variation
            double realisticCurrent = baseCurrent * (1 + noise);
            
            // Add occasional spikes (regenerative braking, sudden acceleration, etc.)
            if (random.NextDouble() < 0.05) // 5% chance of spike
            {
                realisticCurrent *= (profileName.Contains("EV") ? 1.5 : 1.3);
            }
            
            timeIndex++;
            return Math.Max(0, realisticCurrent);
        }
        
        /// <summary>
        /// Get realistic ambient temperature based on time of day and season
        /// </summary>
        public double GetRealisticTemperature(double baseTemperature = 25.0)
        {
            // Simulate daily temperature variation
            double hourOfDay = (timeIndex % 86400) / 3600.0; // Convert to hours
            double dailyVariation = 8 * Math.Sin((hourOfDay - 6) * Math.PI / 12); // Peak at 2 PM
            
            // Add random weather variations
            double weatherNoise = (random.NextDouble() - 0.5) * 6; // ±3°C variation
            
            double temperature = baseTemperature + dailyVariation + weatherNoise;
            
            // Clamp to realistic ranges
            return Math.Max(-20, Math.Min(50, temperature));
        }
        
        /// <summary>
        /// Simulate realistic voltage noise and measurement errors
        /// </summary>
        public double AddVoltageNoise(double idealVoltage)
        {
            // ADC noise, sensor drift, electrical interference
            double noise = (random.NextDouble() - 0.5) * 0.02; // ±10mV
            return idealVoltage + noise;
        }
        
        /// <summary>
        /// Simulate EIS (Electrochemical Impedance Spectroscopy) data
        /// </summary>
        public EISData GetEISData(double soc, double temperature, int cycleCount)
        {
            // Real EIS parameters for Li-ion batteries
            double baseSeriesResistance = 0.005; // Ohm (typical for a pack, lower than cell)
            double baseChargeTransferResistance = 0.02; // Ohm
            double baseDoubleLayerCapacitance = 1500; // F (scaled for pack)
            double baseWarburgCoefficient = 0.005; // Adjusted for pack level

            // SoC effects
            double socFactorRct; // Charge transfer resistance is highly SoC dependent
            if (soc < 10) socFactorRct = 2.5;       // Very low SoC, high Rct
            else if (soc < 20) socFactorRct = 1.8;
            else if (soc > 90) socFactorRct = 1.5;  // Very high SoC, slightly higher Rct
            else if (soc > 80) socFactorRct = 1.2;
            else socFactorRct = 1.0;               // Mid-SoC, optimal Rct

            double socFactorCdl = (soc < 20 || soc > 80) ? 0.8 : 1.0; // Capacitance slightly lower at extremes

            // Temperature effects (Arrhenius relationship for resistances, different for capacitance)
            // Using a simplified exponential factor for demonstration
            double tempFactorResistance = Math.Exp(2000 * (1.0 / (temperature + 273.15) - 1.0 / 298.15)); // Resistance increases at low temp
            double tempFactorCapacitance = Math.Exp(-500 * (1.0 / (temperature + 273.15) - 1.0 / 298.15)); // Capacitance decreases at low temp

            // Aging effects (resistance increases, capacitance decreases)
            double cycleDegradationFactor = 0.0002; // Slower degradation for EIS params compared to capacity
            double agingFactorResistance = 1.0 + (cycleCount * cycleDegradationFactor * 1.5); // Resistance increases more with age
            double agingFactorCapacitance = Math.Max(0.5, 1.0 - (cycleCount * cycleDegradationFactor)); // Capacitance decreases, floor at 50%

            return new EISData
            {
                SeriesResistance = baseSeriesResistance * tempFactorResistance * agingFactorResistance,
                ChargeTransferResistance = baseChargeTransferResistance * socFactorRct * tempFactorResistance * agingFactorResistance,
                DoubleLayerCapacitance = baseDoubleLayerCapacitance * socFactorCdl * tempFactorCapacitance * agingFactorCapacitance,
                WarburgCoefficient = baseWarburgCoefficient * Math.Sqrt(tempFactorResistance * agingFactorResistance), // Warburg often related to diffusion, affected by temp/age
                Frequency = 1000.0 // Hz, default measurement frequency for this data point
            };
        }
        
        /// <summary>
        /// Get available load profiles
        /// </summary>
        public List<string> GetAvailableProfiles()
        {
            return loadProfiles.Select(p => p.Name).ToList();
        }
        
        /// <summary>
        /// Get profile description
        /// </summary>
        public string GetProfileDescription(string profileName)
        {
            var profile = loadProfiles.FirstOrDefault(p => p.Name == profileName);
            return profile?.Description ?? "Unknown profile";
        }
        
        /// <summary>
        /// Reset time index for repeatable simulations
        /// </summary>
        public void Reset()
        {
            timeIndex = 0;
        }
    }
    
    /// <summary>
    /// Load profile definition
    /// </summary>
    public class LoadProfile
    {
        public string Name { get; set; } = "";
        public double[] BasePattern { get; set; } = new double[0];
        public string Description { get; set; } = "";
    }
    
    /// <summary>
    /// Electrochemical Impedance Spectroscopy data
    /// </summary>
    public class EISData
    {
        public double SeriesResistance { get; set; }        // Rs (Ohm)
        public double ChargeTransferResistance { get; set; } // Rct (Ohm)
        public double DoubleLayerCapacitance { get; set; }   // Cdl (F)
        public double WarburgCoefficient { get; set; }       // Warburg impedance
        public double Frequency { get; set; }               // Measurement frequency (Hz)
        
        /// <summary>
        /// Calculate total impedance at given frequency
        /// </summary>
        public double GetTotalImpedance(double frequencyToCalcAt)
        {
            if (DoubleLayerCapacitance <= 0 || frequencyToCalcAt <= 0) 
            {
                // Avoid division by zero or invalid calculations
                return SeriesResistance + ChargeTransferResistance; 
            }

            double omega = 2 * Math.PI * frequencyToCalcAt;
            
            // Impedance of Cdl (Z_Cdl = 1 / (j*omega*Cdl))
            // For magnitude, |Z_Cdl| = 1 / (omega*Cdl)
            double zCdlMagnitude = 1.0 / (omega * DoubleLayerCapacitance);

            // Warburg impedance (Z_W = W / sqrt(omega) * (1-j) for semi-infinite)
            // Magnitude |Z_W| = W * sqrt(2) / sqrt(omega) if considering complex part, or simply W/sqrt(omega) for real part contribution in some models
            // For simplicity, let's use a common form for its contribution to magnitude in series with Rct or Cdl
            double warburgMagnitude = WarburgCoefficient / Math.Sqrt(omega); 

            // Simplified Randles circuit: Rs + (Rct || (Z_Cdl + Z_Warburg_simplified_real_part))
            // This is a common simplification. A more accurate model would use complex numbers.
            // Z_parallel = 1 / (1/Rct + 1/(Z_Cdl_mag + Z_Warburg_mag)) - this is not entirely correct for AC series components
            // Correct parallel combination for impedances Z1 and Z2 is (Z1*Z2)/(Z1+Z2)
            // Here, we are dealing with magnitudes in a simplified way. Let's assume Rct is in parallel with (Cdl in series with Warburg element)
            // Z_Cdl_Warburg_series_magnitude = sqrt( (1/(omega*Cdl))^2 + (W/sqrt(omega))^2 ) - this is not how it's typically combined with Rct in parallel

            // More common simplification for total impedance magnitude of a Randles cell (Rs + Rct || Cdl) + Warburg (if Warburg is in series with Rct||Cdl)
            // Or Rs in series with (Rct parallel with (Cdl in series with Z_Warburg_element))
            // Let's use a common approximation for the impedance of (Rct || Cdl) part:
            // Z_Rct_Cdl_parallel_real = Rct / (1 + (omega * Rct * Cdl)^2)
            // Z_Rct_Cdl_parallel_imag = -(omega * Rct^2 * Cdl) / (1 + (omega * Rct * Cdl)^2)
            // Magnitude_Rct_Cdl = Rct / sqrt(1 + (omega * Rct * Cdl)^2)

            // For total impedance, a common simplification for magnitude:
            // Z_total = Rs + sqrt(Rct^2 + (1/(omega*Cdl) - Warburg_imag_part)^2) - this is too complex without full complex math.

            // Let's use a simpler, more common representation for the magnitude of the parallel Rct and Cdl part:
            // Z_parallel_Rct_Cdl = Rct / sqrt(1 + (omega * Rct * DoubleLayerCapacitance)^2)
            // And then add Warburg. The way Warburg is added depends on the specific equivalent circuit model.
            // If Warburg is considered in series with Rct||Cdl:
            // Z_total = SeriesResistance + Z_parallel_Rct_Cdl + WarburgMagnitude (scalar addition of magnitudes is an approximation)

            // A common way to represent the impedance of Rct || Cdl:
            // Z_RC_parallel_real = Rct / (1 + Math.Pow(omega * Rct * DoubleLayerCapacitance, 2));
            // Z_RC_parallel_imag = (-omega * Math.Pow(Rct, 2) * DoubleLayerCapacitance) / (1 + Math.Pow(omega * Rct * DoubleLayerCapacitance, 2));
            // Total impedance Z = Rs + Z_RC_parallel_real + Warburg_real_part + j*(Z_RC_parallel_imag + Warburg_imag_part)
            // Warburg_real = Warburg_imag = WarburgCoefficient / Math.Sqrt(omega)

            double z_rct_parallel_real = ChargeTransferResistance / (1 + Math.Pow(omega * ChargeTransferResistance * DoubleLayerCapacitance, 2));
            double warburg_real = WarburgCoefficient / Math.Sqrt(omega);

            double total_real_part = SeriesResistance + z_rct_parallel_real + warburg_real;

            // Imaginary parts
            double z_rct_parallel_imag = (-omega * Math.Pow(ChargeTransferResistance, 2) * DoubleLayerCapacitance) / (1 + Math.Pow(omega * ChargeTransferResistance * DoubleLayerCapacitance, 2));
            double warburg_imag = -WarburgCoefficient / Math.Sqrt(omega); // Negative sign for capacitive-like Warburg

            double total_imag_part = z_rct_parallel_imag + warburg_imag;

            return Math.Sqrt(Math.Pow(total_real_part, 2) + Math.Pow(total_imag_part, 2));
        }
    }
}
