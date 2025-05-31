# Advanced Battery Management System (BMS) Simulator

A professional Windows Presentation Foundation (WPF) application that simulates comprehensive battery State of Charge (SoC) and State of Health (SoH) estimation using advanced battery management techniques with real-time visualization, thermal modeling, and intelligent fault detection.

## 🚀 Key Features

### 🔋 Real-Time Battery Monitoring
- **State of Charge (SoC)**: Real-time calculation using Coulomb counting with visual progress indicators
- **State of Health (SoH)**: Battery degradation tracking based on cycle count (0.1% per cycle)
- **Voltage Simulation**: Dynamic voltage calculation based on SoC levels with safety thresholds
- **Temperature Modeling**: Advanced thermal simulation based on current load and battery state
- **Power Consumption**: Real-time power calculation (P = V × I) with consumption tracking

### 📊 Professional Dashboard
- **5 Real-Time Status Cards**: SoC, SoH, Voltage, Temperature, and Power monitoring
- **Dynamic Color Coding**: Visual status indicators that change based on battery conditions
- **Progress Bars**: Visual SoC and SoH progress indicators for quick assessment
- **Live Statistics**: Runtime, cycle count, and current load display

### 📈 Advanced Data Visualization
- **4 Real-Time Charts**: 
  - State of Charge trend
  - Voltage monitoring over time
  - Temperature tracking and alerts
  - Power consumption analysis
- **60-Second Rolling Window**: Optimized performance with recent data focus
- **Synchronized Time Series**: All charts update simultaneously for comprehensive analysis
  - State of Health (SoH)
  - Cycle count
- **Voltage Simulation**: Models voltage drop based on SoC level

### Stage 3 - Advanced Real-Time Simulation and Fault Detection
- **Dynamic Current Loads**: Randomized current loads between 50-150% of base value
- **Real-Time Charts**: Live visualization of SoC and voltage using LiveCharts.Wpf
- **Fault Detection System**: Automated alerts for:
  - Low SoC warning (below 10%)
  - Overvoltage alert (above 4.2V)
  - Undervoltage alert (below 3.0V)
  - Critical SoH warning (below 80%)
- **Enhanced Data Visualization**: Dual-panel chart display with 60-second rolling window

## Technical Specifications

- **Framework**: .NET 6 with Windows-specific targeting
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Charting Library**: LiveCharts.Wpf v0.9.7
- **IDE**: Visual Studio 2022 (Community, Professional, or Enterprise)
- **OS Requirements**: Windows 10 or 11
- **Hardware**: Minimum 4GB RAM

## Getting Started

### Prerequisites

1. Windows 10 or 11
2. Visual Studio 2022 with .NET 6 SDK
3. WPF workload installed in Visual Studio

### Running the Application

1. Open the solution in Visual Studio 2022
2. Build the solution (Ctrl+Shift+B)
3. Run the application (F5 or Ctrl+F5)

### Using the Simulator

1. **Set Parameters**:
   - **Capacity (Ah)**: Battery capacity in Ampere-hours
   - **Initial SoC (%)**: Starting State of Charge percentage (0-100)
   - **Current (A)**: Base discharge current in Amperes (will be randomized during simulation)

2. **Start Simulation**: Click "Start Simulation" to begin real-time calculations

3. **Monitor Progress**: Watch real-time updates:
   - **SoC**: Decreases based on dynamic discharge rate using Coulomb counting
   - **SoH**: Degrades by 0.1% per cycle (every 10 seconds)
   - **Charts**: Live visualization of SoC and voltage trends
   - **Current Load**: Dynamic current displayed in window title (varies 50-150% of base)

4. **Fault Monitoring**: Automatic alerts for:
   - Low battery warnings
   - Voltage anomalies
   - Battery health degradation

5. **Data Logging**: CSV files are automatically created with timestamped filenames:
   - Format: `battery_log_YYYYMMDD_HHMMSS.csv`
   - Contains comprehensive battery state data with dynamic current values

6. **Stop Simulation**: Click "Stop Simulation" to halt the process or wait for automatic stop at 0% SoC

## Calculation Methods

### SoC Calculation (Coulomb Counting)
```
SoC = Initial SoC - (Current × Time / 3600) / Capacity × 100
```

### SoH Calculation (Cycle-based Degradation)
```
SoH = 100 - (Cycle Count × 0.1)
```

### Voltage Simulation
```
Voltage = 3.7 - (0.3 × (100 - SoC) / 100)
```

Where:
- **Current**: Discharge current in Amperes (A)
- **Time**: Elapsed time in seconds (converted to hours)
- **Capacity**: Battery capacity in Ampere-hours (Ah)
- **SoC**: State of Charge as a percentage (%)
- **SoH**: State of Health as a percentage (%)
- **Cycle Count**: Number of battery cycles (incremented every 10 seconds)

## Project Structure

```
BMS_Simulator/
├── App.xaml                 # Application configuration
├── App.xaml.cs              # Application code-behind
├── MainWindow.xaml          # Main UI layout
├── MainWindow.xaml.cs       # Main window logic and SoC calculation
├── BMSSimulator.csproj      # Project file
├── README.md                # This file
└── .github/
    └── copilot-instructions.md  # GitHub Copilot instructions
```

## Future Enhancements

This is Stage 1 of the BMS Simulator. Future stages may include:
- Temperature effects on battery performance
- Different battery chemistries and models
- Charging simulation
- Battery health estimation
- Data logging and visualization
- Multi-cell battery pack simulation

## License

This project is created for educational purposes.
