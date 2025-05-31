using System;
using System.Collections.Generic;
using System.Linq; // Added for LINQ operations

namespace BMSSimulator
{
    /// <summary>
    /// Advanced battery model using Kalman filter for SoC estimation
    /// Based on real Li-ion battery characteristics
    /// </summary>
    public class BatteryModel
    {
        // Battery parameters (based on 18650 Li-ion cell scaled to capacity)
        private readonly double nominalVoltage = 3.7; // V
        private readonly double maxVoltage = 4.2; // V
        private readonly double minVoltage = 2.8; // V
        private readonly double internalResistance = 0.05; // Ohm, base resistance for the pack

        // Kalman filter state variables
        private double socEstimate = 100.0; // State estimate (SoC)
        private double processNoise = 0.01; // Process noise covariance (Q)
        private double measurementNoise = 0.1; // Measurement noise covariance (R)
        private double estimateCovariance = 1.0; // Estimate error covariance (P)
        
        // Battery characteristics from real data
        private Dictionary<double, double> socVoltageMap;
        private Dictionary<double, double> temperatureEfficiency;

        // Store last known operating conditions
        private double lastKnownTemperature = 25.0; // Default Celsius
        private int lastKnownCycleCount = 0;

        public double Capacity { get; }
        public double SoC => socEstimate;
        public double Voltage { get; private set; }
        public double InternalResistance => CalculateInternalResistance(socEstimate, lastKnownTemperature, lastKnownCycleCount);

        public BatteryModel(double capacity)
        {
            Capacity = capacity;
            // Initialize dictionaries directly to ensure they are not null
            socVoltageMap = new Dictionary<double, double>();
            temperatureEfficiency = new Dictionary<double, double>();
            InitializeSoCVoltageMap();
            InitializeTemperatureEfficiency();
            Reset(100.0); // Initialize with full SoC
        }

        private void InitializeSoCVoltageMap()
        {
            // Real Li-ion discharge curve data (SoC% -> Open Circuit Voltage)
            socVoltageMap = new Dictionary<double, double>
            {
                {100.0, 4.20}, {95.0, 4.15}, {90.0, 4.10}, {85.0, 4.05},
                {80.0, 4.00}, {75.0, 3.95}, {70.0, 3.92}, {65.0, 3.89},
                {60.0, 3.86}, {55.0, 3.83}, {50.0, 3.80}, {45.0, 3.77},
                {40.0, 3.74}, {35.0, 3.71}, {30.0, 3.68}, {25.0, 3.65},
                {20.0, 3.62}, {15.0, 3.58}, {10.0, 3.52}, {5.0, 3.40},
                {0.0, 2.80}
            };
        }

        private void InitializeTemperatureEfficiency()
        {
            // Temperature effect on battery efficiency (°C -> efficiency factor)
            temperatureEfficiency = new Dictionary<double, double>
            {
                {-20, 0.6}, {-10, 0.7}, {0, 0.8}, {10, 0.9},
                {20, 0.98}, {25, 1.0}, {30, 0.99}, {40, 0.97},
                {50, 0.94}, {60, 0.90}
            };
        }

        /// <summary>
        /// Kalman filter update for SoC estimation.
        /// </summary>
        /// <param name="current">Current in Amperes (positive for discharge, negative for charge).</param>
        /// <param name="voltage">Measured terminal voltage in Volts.</param>
        /// <param name="temperature">Ambient temperature in Celsius.</param>
        /// <param name="cycleCount">Current battery cycle count.</param>
        /// <param name="deltaTime">Time step in seconds.</param>
        public void UpdateSoC(double current, double voltage, double temperature, int cycleCount, double deltaTime)
        {
            lastKnownTemperature = temperature;
            lastKnownCycleCount = cycleCount;

            // Prediction step
            double currentEfficiency = GetTemperatureEfficiency(temperature);
            double effectiveCapacity = GetEffectiveCapacity(cycleCount);
            if (effectiveCapacity <= 0) effectiveCapacity = Capacity; // Avoid division by zero if capacity is depleted

            double deltaCapacity = (current * deltaTime / 3600.0) * currentEfficiency; // Ah
            double socPrediction = socEstimate - (deltaCapacity / effectiveCapacity * 100.0);
            socPrediction = Math.Max(0, Math.Min(100, socPrediction)); // Constrain SoC

            // Update process covariance
            estimateCovariance += processNoise;

            // Measurement update (using voltage measurement)
            double resistanceAtPrediction = CalculateInternalResistance(socPrediction, temperature, cycleCount);
            double expectedVoltage = GetOpenCircuitVoltage(socPrediction) - (current * resistanceAtPrediction);
            // double innovation = voltage - expectedVoltage; // Measurement residual - can be used if KF is structured differently

            // Kalman gain
            double kalmanGain = estimateCovariance / (estimateCovariance + measurementNoise);

            // Update estimate using SoC derived from voltage
            double socFromVoltage = GetSoCFromVoltage(voltage, current, socPrediction, temperature, cycleCount);
            double measurementResidual = socFromVoltage - socPrediction; // Residual in terms of SoC
            socEstimate = socPrediction + kalmanGain * measurementResidual;

            // Update covariance
            estimateCovariance = (1 - kalmanGain) * estimateCovariance;

            // Constraints
            socEstimate = Math.Max(0, Math.Min(100, socEstimate));

            // Update terminal voltage based on the new SoC estimate
            double resistanceAtEstimate = CalculateInternalResistance(socEstimate, temperature, cycleCount);
            Voltage = GetOpenCircuitVoltage(socEstimate) - (current * resistanceAtEstimate);
            Voltage = Math.Max(minVoltage - 0.5, Math.Min(maxVoltage + 0.5, Voltage)); // Practical voltage limits
        }

        /// <summary>
        /// Get open circuit voltage based on SoC using interpolation.
        /// </summary>
        private double GetOpenCircuitVoltage(double soc)
        {
            if (socVoltageMap == null || !socVoltageMap.Any()) return nominalVoltage; // Fallback

            var sortedMap = socVoltageMap.OrderBy(kvp => kvp.Key).ToList();

            if (soc <= sortedMap.First().Key) return sortedMap.First().Value;
            if (soc >= sortedMap.Last().Key) return sortedMap.Last().Value;

            var upper = sortedMap.First(kvp => kvp.Key >= soc);
            var lower = sortedMap.Last(kvp => kvp.Key <= soc);

            if (upper.Key == lower.Key) return lower.Value;

            double ratio = (soc - lower.Key) / (upper.Key - lower.Key);
            return lower.Value + ratio * (upper.Value - lower.Value);
        }

        /// <summary>
        /// Estimate SoC from voltage measurement (inverse lookup with interpolation).
        /// </summary>
        private double GetSoCFromVoltage(double measuredVoltage, double current, double socForResistanceHint, double temperature, int cycleCount)
        {
            if (socVoltageMap == null || !socVoltageMap.Any()) return 50.0; // Fallback

            // Compensate for internal resistance to estimate OCV
            double resistance = CalculateInternalResistance(socForResistanceHint, temperature, cycleCount);
            double estimatedOcv = measuredVoltage + (current * resistance);

            var sortedMap = socVoltageMap.OrderBy(kvp => kvp.Value).ToList(); // Order by voltage for inverse lookup

            if (estimatedOcv <= sortedMap.First().Value) return sortedMap.First().Key;
            if (estimatedOcv >= sortedMap.Last().Value) return sortedMap.Last().Key;

            var upper = sortedMap.First(kvp => kvp.Value >= estimatedOcv);
            var lower = sortedMap.Last(kvp => kvp.Value <= estimatedOcv);

            if (upper.Value == lower.Value) return lower.Key;

            double ratio = (estimatedOcv - lower.Value) / (upper.Value - lower.Value);
            return lower.Key + ratio * (upper.Key - lower.Key);
        }

        /// <summary>
        /// Calculate internal resistance based on SoC, temperature, and SoH (cycle count).
        /// </summary>
        private double CalculateInternalResistance(double forSoC, double temperature, int cycleCount)
        {
            double baseR = this.internalResistance;

            // SoC Factor
            double socFactor;
            if (forSoC < 10 || forSoC > 95) socFactor = 1.8; // Higher resistance at extremes
            else if (forSoC < 20 || forSoC > 80) socFactor = 1.3;
            else socFactor = 1.0;

            // Temperature Factor
            double tempFactor;
            if (temperature < 0) tempFactor = 1.7;
            else if (temperature < 10) tempFactor = 1.4; // Cold
            else if (temperature > 50) tempFactor = 1.6; // Very Hot
            else if (temperature > 40) tempFactor = 1.2; // Hot
            else tempFactor = 1.0; // Optimal range 10-40 C

            // SoH Factor (Aging)
            double soh = GetStateOfHealth(cycleCount); // SoH is 0-100
            // Resistance increases as SoH drops. Max 1.5x increase at SoH = 0% (e.g. 50% increase over fresh cell)
            double sohFactor = 1.0 + (100.0 - Math.Max(0, soh)) / 100.0 * 0.5; 

            return baseR * socFactor * tempFactor * sohFactor;
        }

        /// <summary>
        /// Get temperature efficiency factor using interpolation.
        /// </summary>
        private double GetTemperatureEfficiency(double temperature)
        {
            if (temperatureEfficiency == null || !temperatureEfficiency.Any()) return 1.0; // Fallback

            var sortedMap = temperatureEfficiency.OrderBy(kvp => kvp.Key).ToList();

            if (temperature <= sortedMap.First().Key) return sortedMap.First().Value;
            if (temperature >= sortedMap.Last().Key) return sortedMap.Last().Value;

            var upper = sortedMap.First(kvp => kvp.Key >= temperature);
            var lower = sortedMap.Last(kvp => kvp.Key <= temperature);

            if (upper.Key == lower.Key) return lower.Value;

            double ratio = (temperature - lower.Key) / (upper.Key - lower.Key);
            return lower.Value + ratio * (upper.Value - lower.Value);
        }
        
        /// <summary>
        /// Simulate aging effects on battery capacity and resistance
        /// </summary>
        public double GetEffectiveCapacity(int cycleCount)
        {
            // Capacity fade: 20% after 1000 cycles (typical for Li-ion)
            double capacityFadeRate = 0.0002; // 0.02% per cycle
            double retainedCapacity = Math.Max(0.6, 1.0 - (cycleCount * capacityFadeRate));
            return Capacity * retainedCapacity;
        }
        
        /// <summary>
        /// Get State of Health based on cycle count and capacity fade
        /// </summary>
        public double GetStateOfHealth(int cycleCount)
        {
            double effectiveCapacity = GetEffectiveCapacity(cycleCount);
            return (effectiveCapacity / Capacity) * 100.0;
        }
        
        /// <summary>
        /// Reset the filter state
        /// </summary>
        public void Reset(double initialSoC)
        {
            socEstimate = Math.Max(0, Math.Min(100, initialSoC));
            estimateCovariance = 1.0;
            // For initial voltage, assume ideal conditions for OCV lookup and resistance.
            lastKnownTemperature = 25.0; 
            lastKnownCycleCount = 0;
            Voltage = GetOpenCircuitVoltage(socEstimate) - (0 * CalculateInternalResistance(socEstimate, lastKnownTemperature, lastKnownCycleCount)); // Assume zero current at reset
        }
    }
}
