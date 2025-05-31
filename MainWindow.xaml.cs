using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using Microsoft.Win32;

namespace BMSSimulator
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Advanced battery simulation components
        private BatteryModel batteryModel = null!;
        private RealWorldDataProvider dataProvider = null!;
        
        // Simulation parameters
        private double capacity;
        private double initialSoC;
        private double timeElapsed;
        private int cycleCount = 0;
        private double temperature = 25.0;
        private double current = 0.0;
        private string selectedLoadProfile = "EV City Driving";
        
        // UI state management
        private DispatcherTimer timer = null!;
        private string logFilePath = null!;
        private DateTime simulationStartTime;
        private bool alertShown = false;
        
        // Chart data properties
        public ChartValues<double> SoCValues { get; set; } = null!;
        public ChartValues<double> VoltageValues { get; set; } = null!;
        public ChartValues<double> TemperatureValues { get; set; } = null!;
        public ChartValues<double> PowerValues { get; set; } = null!;
        public string[] TimeLabelsSoC { get; set; } = new string[0];
        public string[] TimeLabelsVoltage { get; set; } = new string[0];
        public string[] TimeLabelsTemperature { get; set; } = new string[0];
        public string[] TimeLabelsPower { get; set; } = new string[0];        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        public MainWindow()
        {
            InitializeComponent();
            InitializeTimer();
            InitializeCharts();
            InitializeDataProvider();
            InitializeLoadProfiles();
        }

        private void InitializeDataProvider()
        {
            dataProvider = new RealWorldDataProvider();
        }
          private void InitializeLoadProfiles()
        {
            // Populate load profile combo box
            var profiles = dataProvider?.GetAvailableProfiles() ?? new List<string> { "EV City Driving", "Power Tool", "Smartphone/Tablet" };
            foreach (var profile in profiles)
            {
                LoadProfileComboBox.Items.Add(profile);
            }
            LoadProfileComboBox.SelectedIndex = 0;
            selectedLoadProfile = profiles[0];
            ProfileDescriptionText.Text = dataProvider?.GetProfileDescription(selectedLoadProfile) ?? "Real-world usage pattern";
        }
        
        private void InitializeCharts()
        {
            SoCValues = new ChartValues<double>();
            VoltageValues = new ChartValues<double>();
            TemperatureValues = new ChartValues<double>();
            PowerValues = new ChartValues<double>();
            
            // Set data context for binding
            DataContext = this;
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };            timer.Tick += Timer_Tick;
        }
        
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Parse input values
                capacity = double.Parse(CapacityTextBox.Text);
                initialSoC = double.Parse(InitialSoCTextBox.Text);

                // Validate inputs
                if (capacity <= 0 || initialSoC < 0 || initialSoC > 100)
                {
                    MessageBox.Show("Please enter valid values:\n" +
                                  "- Capacity must be greater than 0\n" +
                                  "- Initial SoC must be between 0-100%", 
                                  "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Initialize advanced battery model
                batteryModel = new BatteryModel(capacity);
                batteryModel.Reset(initialSoC);
                
                // Reset simulation state
                timeElapsed = 0;
                cycleCount = 0;
                temperature = 25.0;
                alertShown = false;                simulationStartTime = DateTime.Now;
                
                // Reset data provider
                dataProvider?.Reset();
                
                // Clear chart data
                SoCValues.Clear();
                VoltageValues.Clear();
                TemperatureValues.Clear();
                PowerValues.Clear();
                
                // Initialize log file
                InitializeLogFile();
                
                timer.Start();

                // Update UI state
                StartButton.IsEnabled = false;
                StopButton.IsEnabled = true;
                ExportButton.IsEnabled = true;
                CapacityTextBox.IsEnabled = false;
                InitialSoCTextBox.IsEnabled = false;
                LoadProfileComboBox.IsEnabled = false;

                // Display initial values
                UpdateDisplay(initialSoC, batteryModel.Voltage, 25.0, 0.0);
            }
            catch (FormatException)
            {
                MessageBox.Show("Please enter valid numeric values.", "Input Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopSimulation();
        }
        
        private void StopSimulation()
        {
            timer.Stop();

            // Reset UI state
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            CapacityTextBox.IsEnabled = true;
            InitialSoCTextBox.IsEnabled = true;
            LoadProfileComboBox.IsEnabled = true;
            
            // Update title
            Title = "Advanced BMS Simulator - Simulation Stopped";
        }

        private void LoadProfileComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (LoadProfileComboBox.SelectedItem != null)
            {
                selectedLoadProfile = LoadProfileComboBox.SelectedItem.ToString() ?? "EV City Driving";
                ProfileDescriptionText.Text = dataProvider?.GetProfileDescription(selectedLoadProfile) ?? "Real-world usage pattern";
            }
        }
          private void Timer_Tick(object? sender, EventArgs e)
        {
            timeElapsed++;

            // Get realistic current load based on selected profile
            current = dataProvider?.GetRealisticCurrent(selectedLoadProfile, 1.0) ?? 10.0; // Default fallback
            
            // Get realistic temperature
            temperature = dataProvider?.GetRealisticTemperature(25.0) ?? 25.0; // Default fallback

            // Simulate a cycle every 30 seconds (more realistic than 10 seconds)
            // This cycle counting is for SoH degradation. The BatteryModel might have its own internal cycle counting or equivalent aging mechanism.
            if ((int)timeElapsed % 30 == 0 && timeElapsed > 0) // Ensure cycle count increments after first tick if condition met
            {
                cycleCount++;
            }

            // Add realistic voltage noise to the *actual* battery voltage before feeding to KF
            // The batteryModel.Voltage is the *estimated* terminal voltage from the *previous* step or initial state.
            // For a more realistic simulation, we should predict OCV, then subtract I*R, then add noise.
            // However, the current structure uses batteryModel.Voltage as the basis for noise addition.            // Let's assume batteryModel.Voltage is the ideal terminal voltage before measurement noise.
            double idealTerminalVoltage = batteryModel.Voltage; // Voltage from previous estimate or reset state
            double measuredVoltageWithNoise = dataProvider?.AddVoltageNoise(idealTerminalVoltage) ?? idealTerminalVoltage;

            // Update Kalman filter with measurements
            // Pass deltaTime as 1.0 because timer interval is 1 second.
            batteryModel.UpdateSoC(current, measuredVoltageWithNoise, temperature, cycleCount, 1.0); 

            // Get current battery state *after* Kalman filter update
            double currentSoC = batteryModel.SoC;
            double currentVoltage = batteryModel.Voltage; // This is the new estimated terminal voltage
            double soh = batteryModel.GetStateOfHealth(cycleCount); // SoH from BatteryModel

            // Check for battery depletion
            if (currentSoC <= 0.1) 
            {
                StopSimulation();
                if (!alertShown)
                {
                    MessageBox.Show("🔋 Battery Protection Activated!\n\nSoC has reached minimum safe level (0.1%).\nSimulation stopped to prevent battery damage.", 
                                  "Battery Protection", MessageBoxButton.OK, MessageBoxImage.Warning);
                    alertShown = true;
                }
                return;
            }

            // Calculate power consumption
            double power = currentVoltage * current; // Use the updated voltage            // Get EIS data for advanced analysis
            var eisData = dataProvider?.GetEISData(currentSoC, temperature, cycleCount) ?? 
                         new EISData { SeriesResistance = 0.05, ChargeTransferResistance = 0.02, DoubleLayerCapacitance = 1500, WarburgCoefficient = 0.005, Frequency = 1000 };

            // Enhanced fault detection with EIS
            CheckForFaults(currentSoC, currentVoltage, temperature, soh, eisData);

            // Update all displays
            UpdateDisplay(currentSoC, currentVoltage, temperature, power, soh, eisData);

            // Update charts
            UpdateCharts(currentSoC, currentVoltage, temperature, power);

            // Log enhanced data to CSV file
            LogData(currentSoC, currentVoltage, temperature, power, soh, current, eisData);

            // Update window title with runtime info
            TimeSpan elapsed = TimeSpan.FromSeconds(timeElapsed);
            Title = $"Advanced BMS Simulator - Running: {elapsed:hh\\:mm\\:ss} | Profile: {selectedLoadProfile} | SoC: {currentSoC:F1}% | V: {currentVoltage:F2}V";
        }        // Overloaded method for backward compatibility
        private void UpdateDisplay(double soc, double voltage, double temperature, double power)
        {
            var eisData = dataProvider?.GetEISData(soc, temperature, cycleCount) ?? 
                         new EISData { SeriesResistance = 0.05, ChargeTransferResistance = 0.02, DoubleLayerCapacitance = 1500, WarburgCoefficient = 0.005, Frequency = 1000 };
            double soh = batteryModel.GetStateOfHealth(cycleCount);
            UpdateDisplay(soc, voltage, temperature, power, soh, eisData);
        }

        private void UpdateDisplay(double soc, double voltage, double temperature, double power, double soh, EISData eisData)
        {
            // Update SoC display with Kalman filter indication
            SoCDisplay.Text = $"{soc:F1}%";
            SoCProgressBar.Value = soc;
            SoCMethodText.Text = "Kalman Filter"; // This is now consistently true
            
            // Color coding for SoC
            if (soc <= 10)
                SoCDisplay.Foreground = new SolidColorBrush(Colors.Red);
            else if (soc <= 25)
                SoCDisplay.Foreground = new SolidColorBrush(Colors.Orange);
            else
                SoCDisplay.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green

            // Update SoH display with capacity fade info
            SoHDisplay.Text = $"{soh:F1}%";
            SoHProgressBar.Value = soh;
            // double capacityFade = 100 - soh; // SoH is already direct representation of health
            CapacityFadeText.Text = soh < 99.9 ? $"Health: {soh:F1}%" : "Optimal Health";
            
            // Color coding for SoH
            if (soh < 80)
                SoHDisplay.Foreground = new SolidColorBrush(Colors.Red);
            else if (soh < 90)
                SoHDisplay.Foreground = new SolidColorBrush(Colors.Orange);
            else
                SoHDisplay.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange

            // Update voltage display with internal resistance
            VoltageDisplay.Text = $"{voltage:F2}V";
            double internalResistancemOhms = batteryModel.InternalResistance * 1000; // Convert to mΩ
            InternalResistanceText.Text = $"Rint: {internalResistancemOhms:F1} mΩ"; // Changed label for clarity
            
            if (voltage < 3.0)
            {
                VoltageDisplay.Foreground = new SolidColorBrush(Colors.Red);
                VoltageStatus.Text = "Undervoltage";
                VoltageStatus.Foreground = new SolidColorBrush(Colors.Red);
            }
            else if (voltage > 4.2)
            {
                VoltageDisplay.Foreground = new SolidColorBrush(Colors.Red);
                VoltageStatus.Text = "Overvoltage";
                VoltageStatus.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                VoltageDisplay.Foreground = new SolidColorBrush(Color.FromRgb(156, 39, 176)); // Purple
                VoltageStatus.Text = "Normal";
                VoltageStatus.Foreground = new SolidColorBrush(Colors.Green);
            }

            // Update temperature display with efficiency
            TemperatureDisplay.Text = $"{temperature:F1}°C";
            // The GetTemperatureEfficiency method in MainWindow was a simplified local version.
            // The BatteryModel now has a more sophisticated GetTemperatureEfficiency.
            // We should decide if this UI display refers to the battery model's internal efficiency or a general one.            // For now, let's use the one from BatteryModel if accessible, or remove if redundant.
            double uiTempEfficiency = GetTemperatureEfficiency(temperature) * 100;
            TempEfficiencyText.Text = $"UI Eff: {uiTempEfficiency:F0}%";
            
            if (temperature > 45)
            {
                TemperatureDisplay.Foreground = new SolidColorBrush(Colors.Red);
                TemperatureStatus.Text = "Hot";
                TemperatureStatus.Foreground = new SolidColorBrush(Colors.Red);
            }
            else if (temperature < 5)
            {
                TemperatureDisplay.Foreground = new SolidColorBrush(Colors.Blue);
                TemperatureStatus.Text = "Cold";
                TemperatureStatus.Foreground = new SolidColorBrush(Colors.Blue);
            }
            else
            {
                TemperatureDisplay.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                TemperatureStatus.Text = "Normal";
                TemperatureStatus.Foreground = new SolidColorBrush(Colors.Green);
            }

            // Update power display with load profile info
            PowerDisplay.Text = $"{power:F1}W";
            CurrentDisplay.Text = $"Current: {current:F1}A";
            LoadProfileText.Text = $"Profile: {selectedLoadProfile.Split(' ')[0]}";

            // Update EIS display
            double totalImpedance = eisData.GetTotalImpedance(1000); // At 1kHz
            EISDisplay.Text = $"{totalImpedance * 1000:F1}"; // Convert to mΩ
            EISStatus.Text = "Impedance (mΩ)";
            EISFrequencyText.Text = "@ 1kHz";

            // Update time and cycle displays
            TimeSpan elapsed = TimeSpan.FromSeconds(timeElapsed);
            TimeDisplay.Text = $"Time: {elapsed:hh\\:mm\\:ss}";
            CycleDisplay.Text = $"Cycles: {cycleCount}";
        }        // This is a local UI helper, distinct from BatteryModel's internal version.
        private double GetTemperatureEfficiency(double currentTemperature)
        {
            // Temperature efficiency curve (simplified for UI display)
            if (currentTemperature < 0) return 0.8;
            if (currentTemperature >= 20 && currentTemperature <= 30) return 1.0; // Optimal range
            if (currentTemperature < 20) return 0.9 + (currentTemperature / 100.0); // e.g. 0.9 at 0C, 0.95 at 10C, 0.99 at 19C
            if (currentTemperature > 40) return 0.95; // Hot conditions
            // Between 30-40°C: gradual decrease from 1.0 to 0.95
            return 1.0 - ((currentTemperature - 30) / 200.0); // e.g. 0.975 at 35C, 0.95 at 40C
        }

        private void CheckForFaults(double soc, double voltage, double temperature, double soh, EISData eisData) // Added SoH parameter
        {
            // Use throttling to prevent spam alerts (only show alerts every 30 seconds)
            bool canShowAlert = (timeElapsed > 0 && timeElapsed % 30 == 0); // Ensure alerts don't fire at time 0

            if (!canShowAlert) return;

            // Low SoC fault detection
            if (soc < 10 && soc > 0.1)
            {
                ShowAlert($"⚠️ Low SoC Alert!\n\nBattery level: {soc:F1}%\nEstimation method: Kalman Filter\nConsider charging the battery soon.", 
                         "Low SoC Warning", MessageBoxImage.Warning);
            }

            // Critical SoC fault detection
            if (soc < 5 && soc > 0.1)
            {
                ShowAlert($"🚨 Critical SoC Alert!\n\nBattery level: {soc:F1}%\nKalman filter confidence: High\nImmediate charging required!", 
                         "Critical SoC Warning", MessageBoxImage.Error);
            }

            // Voltage fault detection with improved accuracy
            if (voltage > 4.25) // Slightly higher threshold for Li-ion
            {
                ShowAlert($"⚡ Overvoltage Alert!\n\nTerminal voltage: {voltage:F3}V\nSafe limit: 4.20V\nRisk of battery damage!", 
                         "Overvoltage Warning", MessageBoxImage.Error);
            }

            if (voltage < 2.8) // Realistic Li-ion cutoff
            {
                ShowAlert($"🔋 Undervoltage Alert!\n\nTerminal voltage: {voltage:F3}V\nSafe limit: 2.80V\nBattery protection required!", 
                         "Undervoltage Warning", MessageBoxImage.Error);
            }

            // Advanced SoH fault detection based on capacity fade
            // double soh = batteryModel.GetStateOfHealth(cycleCount); // SoH is now passed as a parameter
            if (soh < 80)
            {
                ShowAlert($"🔧 Battery Health Alert!\n\nSoH: {soh:F1}%\nCapacity fade detected\nCycle count: {cycleCount}\nBattery performance significantly degraded!", 
                         "SoH Warning", MessageBoxImage.Warning);
            }

            // Temperature fault detection with efficiency impact
            if (temperature > 50)
            {
                ShowAlert($"🌡️ High Temperature Alert!\n\nTemperature: {temperature:F1}°C\nEfficiency impact: Reduced performance\nBattery overheating risk!", 
                         "Temperature Warning", MessageBoxImage.Error);
            }
            else if (temperature < -5)
            {
                ShowAlert($"❄️ Low Temperature Alert!\n\nTemperature: {temperature:F1}°C\nEfficiency: Significantly reduced\nBattery performance impacted!", 
                         "Temperature Warning", MessageBoxImage.Warning);
            }

            // EIS-based fault detection (Advanced diagnostics)
            double totalImpedance = eisData.GetTotalImpedance(1000); // At 1kHz
            if (totalImpedance > 0.15) // 150 mΩ threshold
            {
                ShowAlert($"🔬 EIS Diagnostic Alert!\n\nTotal impedance: {totalImpedance * 1000:F1} mΩ\nThreshold: 150 mΩ\nPossible causes:\n- Internal resistance increase\n- Electrolyte degradation\n- Active material loss", 
                         "EIS Diagnostic Warning", MessageBoxImage.Warning);
            }

            // Internal resistance monitoring
            double internalResistance = batteryModel.InternalResistance * 1000; // mΩ
            if (internalResistance > 100) // 100 mΩ threshold
            {
                ShowAlert($"⚙️ Internal Resistance Alert!\n\nInternal resistance: {internalResistance:F1} mΩ\nNormal range: < 100 mΩ\nIndicates battery aging or damage!", 
                         "Resistance Warning", MessageBoxImage.Warning);
            }
        }

        private void ShowAlert(string message, string title, MessageBoxImage icon)
        {
            // Show alert in a non-blocking way to prevent simulation interruption
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, icon);
            }), DispatcherPriority.Background);
        }
        
        private void UpdateCharts(double soc, double voltage, double temperature, double power)
        {
            // Add new data points to charts
            SoCValues.Add(soc);
            VoltageValues.Add(voltage);
            TemperatureValues.Add(temperature);
            PowerValues.Add(power);

            // Keep only last 60 data points for better visualization
            int maxPoints = 60;
            if (SoCValues.Count > maxPoints)
            {
                SoCValues.RemoveAt(0);
            }
            if (VoltageValues.Count > maxPoints)
            {
                VoltageValues.RemoveAt(0);
            }
            if (TemperatureValues.Count > maxPoints)
            {
                TemperatureValues.RemoveAt(0);
            }
            if (PowerValues.Count > maxPoints)
            {
                PowerValues.RemoveAt(0);
            }

            // Update time labels (show last N seconds based on data points)
            var currentPointsCount = SoCValues.Count; // Assuming all charts have the same number of points
            var timeLabels = new string[currentPointsCount];
            long firstTimestampOnChart = Math.Max(1, (long)timeElapsed - currentPointsCount + 1);

            for (int i = 0; i < currentPointsCount; i++)
            {
                timeLabels[i] = (firstTimestampOnChart + i).ToString();
            }
            
            TimeLabelsSoC = timeLabels;
            TimeLabelsVoltage = timeLabels;
            TimeLabelsTemperature = timeLabels;
            TimeLabelsPower = timeLabels;

            // Notify property change for chart updates
            OnPropertyChanged(nameof(TimeLabelsSoC)); // Property name should match XAML binding
            OnPropertyChanged(nameof(TimeLabelsVoltage));
            OnPropertyChanged(nameof(TimeLabelsTemperature));
            OnPropertyChanged(nameof(TimeLabelsPower));
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private void InitializeLogFile()
        {
            try
            {
                // Create log file with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                logFilePath = $"battery_log_{timestamp}.csv";
                  // Create CSV header with enhanced data
                using (StreamWriter writer = new StreamWriter(logFilePath))
                {
                    writer.WriteLine("Timestamp,Time_Elapsed(s),SoC(%),Voltage(V),Temperature(°C),Power(W),Current(A),SoH(%),Cycle_Count,EIS_Impedance(mΩ),Internal_Resistance(mΩ),Load_Profile");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating log file: {ex.Message}", "Logging Error", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }        // Enhanced LogData method with EIS data
        private void LogData(double soc, double voltage, double temperature, double power, double soh, double current, EISData eisData)
        {
            try
            {
                if (!string.IsNullOrEmpty(logFilePath))
                {
                    using (StreamWriter writer = new StreamWriter(logFilePath, true))
                    {
                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        double totalImpedance = eisData.GetTotalImpedance(1000) * 1000; // mΩ
                        double internalResistance = batteryModel.InternalResistance * 1000; // mΩ
                        writer.WriteLine($"{timestamp},{timeElapsed},{soc:F2},{voltage:F3},{temperature:F1},{power:F2},{current:F2},{soh:F2},{cycleCount},{totalImpedance:F1},{internalResistance:F1},{selectedLoadProfile}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log errors silently to avoid disrupting simulation
                System.Diagnostics.Debug.WriteLine($"Logging error: {ex.Message}");
            }
        }        // Legacy LogData method for backward compatibility
        private void LogData(double soc, double voltage, double temperature, double power, double soh)
        {
            var eisData = dataProvider?.GetEISData(soc, temperature, cycleCount) ?? 
                         new EISData { SeriesResistance = 0.05, ChargeTransferResistance = 0.02, DoubleLayerCapacitance = 1500, WarburgCoefficient = 0.005, Frequency = 1000 };
            LogData(soc, voltage, temperature, power, soh, current, eisData);
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = "csv",
                    FileName = $"BMS_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Copy current log file to selected location
                    if (File.Exists(logFilePath))
                    {
                        File.Copy(logFilePath, saveDialog.FileName, true);
                        MessageBox.Show($"Data exported successfully to:\n{saveDialog.FileName}", 
                                      "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("No simulation data available to export.", 
                                      "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting data: {ex.Message}", "Export Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            timer?.Stop();
            base.OnClosed(e);
        }
    }
}
