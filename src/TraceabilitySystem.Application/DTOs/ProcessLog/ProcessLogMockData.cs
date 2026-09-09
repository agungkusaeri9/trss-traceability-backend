using System;
using System.Collections.Generic;
using System.Linq;

namespace TraceabilitySystem.Application.DTOs.ProcessLog;

/// <summary>
/// In-memory Mock Data Source for Process Logs.
/// Native C# data definitions referencing documentation specifications.
/// </summary>
public static class ProcessLogMockData
{
    public static List<ProcessLogMockDto> GetMockRecords()
    {
        return new List<ProcessLogMockDto>
        {
            // Record 1: All Stations Passed
            new()
            {
                Id = 1,
                Timestamp = "2026-09-04 14:10:22",
                SerialNumberClinching = "CC20260907001",
                SerialNumberMFan = "MF20260907001",
                CoreAsmValue = "20260907001",
                UpperTankAsmValue = "20260907002",
                LowerTankAsmValue = "20260907003",
                ORingSetResult = 1,
                NgBoxSensorShortSideValue = "ON",
                ClinchingHeightValues = new double[] { 12.44, 12.51, 12.48, 12.53, 12.47, 12.50, 12.49, 12.48, 12.52, 12.50, 12.49, 12.51, 12.48, 12.50, 12.49, 12.51, 12.48, 12.50 },
                ClinchingHeightStatus = true,
                EndPlateWidthResults = Enumerable.Repeat(1, 60).ToArray(),
                EndPlateWidthStatus = true,
                NgBoxSensorLongSideValue = "ON",
                CapTypePositionResult = 1,
                LeakResult = 1,
                LeakLastLeakageValue = 0.05,
                LotFanAsmResult = "20260907004",
                LotMotorAsmResult = "20260907005",
                LotGuideAsmResult = "20260907006",
                BoltTightenValue = "ON",
                BoltTightenQtyValue = "4",
                NutTightenValue = "ON",
                MFanInspectionRotationSpeedMaxValue = 2850,
                MFanInspectionRotationSpeedMinValue = 2790,
                MFanInspectionAmpereMaxValue = 4.8,
                MFanInspectionAmpereMinValue = 4.2,
                MFanInspectionWindDirectionValue = "CW",
                MFanTestResult = 1,
                NgBoxSensorMFanInspectionValue = "ON",
                RadCoreAsmNameLabelResult = 1,
                MotorFanAssyLabelResult = 1,
                EcmAssyBoltTightenValue = 45.67,
                EcmAssyBoltTightenQtyValue = 4,
                NgBoxSensorEcmAssyValue = "ON",
                FinalInspectionRadCoreAsmNameLabelResult = 1,
                CheckPoints = Enumerable.Repeat(1, 20).ToArray(),
                CheckPointStatus = true,
                NgBoxSensorFinalInspectionValue = "ON",
                OverallStatus = true
            },

            // Record 2: All Stations Passed
            new()
            {
                Id = 2,
                Timestamp = "2026-09-04 14:12:05",
                SerialNumberClinching = "CC20260907002",
                SerialNumberMFan = "MF20260907002",
                CoreAsmValue = "20260907007",
                UpperTankAsmValue = "20260907008",
                LowerTankAsmValue = "20260907009",
                ORingSetResult = 1,
                NgBoxSensorShortSideValue = "ON",
                ClinchingHeightValues = new double[] { 12.46, 12.50, 12.48, 12.52, 12.49, 12.51, 12.50, 12.47, 12.53, 12.49, 12.50, 12.48, 12.50, 12.52, 12.49, 12.51, 12.48, 12.50 },
                ClinchingHeightStatus = true,
                EndPlateWidthResults = Enumerable.Repeat(1, 60).ToArray(),
                EndPlateWidthStatus = true,
                NgBoxSensorLongSideValue = "ON",
                CapTypePositionResult = 1,
                LeakResult = 1,
                LeakLastLeakageValue = 0.04,
                LotFanAsmResult = "20260907010",
                LotMotorAsmResult = "20260907011",
                LotGuideAsmResult = "20260907012",
                BoltTightenValue = "ON",
                BoltTightenQtyValue = "4",
                NutTightenValue = "ON",
                MFanInspectionRotationSpeedMaxValue = 2840,
                MFanInspectionRotationSpeedMinValue = 2800,
                MFanInspectionAmpereMaxValue = 4.7,
                MFanInspectionAmpereMinValue = 4.3,
                MFanInspectionWindDirectionValue = "CW",
                MFanTestResult = 1,
                NgBoxSensorMFanInspectionValue = "ON",
                RadCoreAsmNameLabelResult = 1,
                MotorFanAssyLabelResult = 1,
                EcmAssyBoltTightenValue = 45.67,
                EcmAssyBoltTightenQtyValue = 4,
                NgBoxSensorEcmAssyValue = "ON",
                FinalInspectionRadCoreAsmNameLabelResult = 1,
                CheckPoints = Enumerable.Repeat(1, 20).ToArray(),
                CheckPointStatus = true,
                NgBoxSensorFinalInspectionValue = "ON",
                OverallStatus = true
            },

            // Record 3: Finished Line with Defects (Height, EndPlate P-03/P-15, CheckPoint CP-07)
            new()
            {
                Id = 3,
                Timestamp = "2026-09-04 14:15:10",
                SerialNumberClinching = "CC20260907003",
                SerialNumberMFan = "MF20260907003",
                CoreAsmValue = "20260907013",
                UpperTankAsmValue = "20260907014",
                LowerTankAsmValue = "20260907015",
                ORingSetResult = 1,
                NgBoxSensorShortSideValue = "ON",
                ClinchingHeightValues = new double[] { 12.45, 12.82, 12.49, 12.50, 12.48, 12.51, 12.50, 12.47, 12.53, 12.49, 12.50, 12.48, 12.50, 12.52, 12.49, 12.51, 12.48, 12.50 },
                ClinchingHeightStatus = false,
                EndPlateWidthResults = Enumerable.Range(0, 60).Select(i => (i != 2 && i != 14) ? 1 : 0).ToArray(),
                EndPlateWidthStatus = false,
                NgBoxSensorLongSideValue = "ON",
                CapTypePositionResult = 1,
                LeakResult = 1,
                LeakLastLeakageValue = 0.05,
                LotFanAsmResult = "20260907016",
                LotMotorAsmResult = "20260907017",
                LotGuideAsmResult = "20260907018",
                BoltTightenValue = "ON",
                BoltTightenQtyValue = "4",
                NutTightenValue = "ON",
                MFanInspectionRotationSpeedMaxValue = 2860,
                MFanInspectionRotationSpeedMinValue = 2780,
                MFanInspectionAmpereMaxValue = 4.9,
                MFanInspectionAmpereMinValue = 4.1,
                MFanInspectionWindDirectionValue = "CW",
                MFanTestResult = 1,
                NgBoxSensorMFanInspectionValue = "ON",
                RadCoreAsmNameLabelResult = 1,
                MotorFanAssyLabelResult = 1,
                EcmAssyBoltTightenValue = 45.67,
                EcmAssyBoltTightenQtyValue = 4,
                NgBoxSensorEcmAssyValue = "ON",
                FinalInspectionRadCoreAsmNameLabelResult = 1,
                CheckPoints = Enumerable.Range(0, 20).Select(i => i != 6 ? 1 : 0).ToArray(),
                CheckPointStatus = false,
                NgBoxSensorFinalInspectionValue = "ON",
                OverallStatus = false
            },

            // Record 4: Stopped Early at Clinching Long Side (EndPlate Defect, No Downstream MF/ECM/Final Data)
            new()
            {
                Id = 4,
                Timestamp = "2026-09-04 14:16:35",
                SerialNumberClinching = "CC20260907004",
                SerialNumberMFan = null,
                CoreAsmValue = "20260907019",
                UpperTankAsmValue = "20260907020",
                LowerTankAsmValue = "20260907021",
                ORingSetResult = 1,
                NgBoxSensorShortSideValue = "ON",
                ClinchingHeightValues = new double[] { 12.48, 12.50, 12.49, 12.51, 12.47, 12.50, 12.49, 12.48, 12.52, 12.50, 12.49, 12.51, 12.48, 12.50, 12.49, 12.51, 12.48, 12.50 },
                ClinchingHeightStatus = true,
                EndPlateWidthResults = Enumerable.Range(0, 60).Select(i => (i != 2 && i != 14) ? 1 : 0).ToArray(),
                EndPlateWidthStatus = false,
                NgBoxSensorLongSideValue = "ON",
                CapTypePositionResult = null,
                LeakResult = null,
                LeakLastLeakageValue = null,
                LotFanAsmResult = null,
                LotMotorAsmResult = null,
                LotGuideAsmResult = null,
                BoltTightenValue = null,
                BoltTightenQtyValue = null,
                NutTightenValue = null,
                MFanInspectionRotationSpeedMaxValue = null,
                MFanInspectionRotationSpeedMinValue = null,
                MFanInspectionAmpereMaxValue = null,
                MFanInspectionAmpereMinValue = null,
                MFanInspectionWindDirectionValue = null,
                MFanTestResult = null,
                NgBoxSensorMFanInspectionValue = null,
                RadCoreAsmNameLabelResult = null,
                MotorFanAssyLabelResult = null,
                EcmAssyBoltTightenValue = null,
                EcmAssyBoltTightenQtyValue = null,
                NgBoxSensorEcmAssyValue = null,
                FinalInspectionRadCoreAsmNameLabelResult = null,
                CheckPoints = null,
                CheckPointStatus = null,
                NgBoxSensorFinalInspectionValue = null,
                OverallStatus = false
            }
        };
    }
}
