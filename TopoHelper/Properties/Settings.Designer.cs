﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TopoHelper.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.6.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1E-06")]
        public double @__APP_EPSILON {
            get {
                return ((double)(this["__APP_EPSILON"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.01")]
        public double DataValidation_LeftrailToRightRail_Tolerance {
            get {
                return ((double)(this["DataValidation_LeftrailToRightRail_Tolerance"]));
            }
            set {
                this["DataValidation_LeftrailToRightRail_Tolerance"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("IAMTopo_2D-")]
        public string LayerNamePrefix_2DObjects {
            get {
                return ((string)(this["LayerNamePrefix_2DObjects"]));
            }
            set {
                this["LayerNamePrefix_2DObjects"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("IAMTopo_3D-")]
        public string LayerNamePrefix_3DObjects {
            get {
                return ((string)(this["LayerNamePrefix_3DObjects"]));
            }
            set {
                this["LayerNamePrefix_3DObjects"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("811")]
        public string Rails2RailwayCenterLine_LayerNameCenterline {
            get {
                return ((string)(this["Rails2RailwayCenterLine_LayerNameCenterline"]));
            }
            set {
                this["Rails2RailwayCenterLine_LayerNameCenterline"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("253")]
        public short Rails2RailwayCenterLine_LayerColorOfCenterline3DPolyLine {
            get {
                return ((short)(this["Rails2RailwayCenterLine_LayerColorOfCenterline3DPolyLine"]));
            }
            set {
                this["Rails2RailwayCenterLine_LayerColorOfCenterline3DPolyLine"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool Rails2RailwayCenterLine_DrawCenterline3DPoints {
            get {
                return ((bool)(this["Rails2RailwayCenterLine_DrawCenterline3DPoints"]));
            }
            set {
                this["Rails2RailwayCenterLine_DrawCenterline3DPoints"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("811-Points")]
        public string Rails2RailwayCenterLine_LayerNameCenterLine3DPoints {
            get {
                return ((string)(this["Rails2RailwayCenterLine_LayerNameCenterLine3DPoints"]));
            }
            set {
                this["Rails2RailwayCenterLine_LayerNameCenterLine3DPoints"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("251")]
        public short Rails2RailwayCenterLine_LayerColorCenterline3DPoints {
            get {
                return ((short)(this["Rails2RailwayCenterLine_LayerColorCenterline3DPoints"]));
            }
            set {
                this["Rails2RailwayCenterLine_LayerColorCenterline3DPoints"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1.52")]
        public double DataValidation_LeftrailToRightRail_Maximum {
            get {
                return ((double)(this["DataValidation_LeftrailToRightRail_Maximum"]));
            }
            set {
                this["DataValidation_LeftrailToRightRail_Maximum"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool Rails2RailwayCenterLine_Draw2DPolyline_CenterLine {
            get {
                return ((bool)(this["Rails2RailwayCenterLine_Draw2DPolyline_CenterLine"]));
            }
            set {
                this["Rails2RailwayCenterLine_Draw2DPolyline_CenterLine"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool Rails2RailwayCenterLine_Draw3DPolyline_CenterLine {
            get {
                return ((bool)(this["Rails2RailwayCenterLine_Draw3DPolyline_CenterLine"]));
            }
            set {
                this["Rails2RailwayCenterLine_Draw3DPolyline_CenterLine"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("c:\\Data\\track.Rails2RailwayCenterLine.csv")]
        public string Rails2RailwayCenterLine_PathToCSVFile {
            get {
                return ((string)(this["Rails2RailwayCenterLine_PathToCSVFile"]));
            }
            set {
                this["Rails2RailwayCenterLine_PathToCSVFile"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool Rails2RailwayCenterLine_Use_CalculateSurveyCorrection {
            get {
                return ((bool)(this["Rails2RailwayCenterLine_Use_CalculateSurveyCorrection"]));
            }
            set {
                this["Rails2RailwayCenterLine_Use_CalculateSurveyCorrection"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool CalculateSurveyCorrection_Draw3DPolyline_Rails {
            get {
                return ((bool)(this["CalculateSurveyCorrection_Draw3DPolyline_Rails"]));
            }
            set {
                this["CalculateSurveyCorrection_Draw3DPolyline_Rails"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("IAMTopo_CalculateSurveyCorrection_Rail")]
        public string CalculateSurveyCorrection_LayerNamePolylines_Rails {
            get {
                return ((string)(this["CalculateSurveyCorrection_LayerNamePolylines_Rails"]));
            }
            set {
                this["CalculateSurveyCorrection_LayerNamePolylines_Rails"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool Rails2RailwayCenterLine_DrawCenterline2DPoints {
            get {
                return ((bool)(this["Rails2RailwayCenterLine_DrawCenterline2DPoints"]));
            }
            set {
                this["Rails2RailwayCenterLine_DrawCenterline2DPoints"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool Rails2RailwayCenterLine_WriteResultToCSVFile {
            get {
                return ((bool)(this["Rails2RailwayCenterLine_WriteResultToCSVFile"]));
            }
            set {
                this["Rails2RailwayCenterLine_WriteResultToCSVFile"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(";")]
        public string Rails2RailwayCenterLine_CSVFile_Delimiter {
            get {
                return ((string)(this["Rails2RailwayCenterLine_CSVFile_Delimiter"]));
            }
            set {
                this["Rails2RailwayCenterLine_CSVFile_Delimiter"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("110")]
        public short CalculateSurveyCorrection_LayerColorPolyline_Rails {
            get {
                return ((short)(this["CalculateSurveyCorrection_LayerColorPolyline_Rails"]));
            }
            set {
                this["CalculateSurveyCorrection_LayerColorPolyline_Rails"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool CalculateSurveyCorrection_Draw3DPoints_Rails {
            get {
                return ((bool)(this["CalculateSurveyCorrection_Draw3DPoints_Rails"]));
            }
            set {
                this["CalculateSurveyCorrection_Draw3DPoints_Rails"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("IAMTopo_CalculateSurveyCorrection_Rail-Points")]
        public string CalculateSurveyCorrection_LayerNamePoints_Rails {
            get {
                return ((string)(this["CalculateSurveyCorrection_LayerNamePoints_Rails"]));
            }
            set {
                this["CalculateSurveyCorrection_LayerNamePoints_Rails"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("115")]
        public short CalculateSurveyCorrection_LayerColorPoints_Rails {
            get {
                return ((short)(this["CalculateSurveyCorrection_LayerColorPoints_Rails"]));
            }
            set {
                this["CalculateSurveyCorrection_LayerColorPoints_Rails"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool JoinPolyline_DeleteSelectedEntities {
            get {
                return ((bool)(this["JoinPolyline_DeleteSelectedEntities"]));
            }
            set {
                this["JoinPolyline_DeleteSelectedEntities"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.005")]
        public double PointsTo3DPolyline_MinimumPointDistance {
            get {
                return ((double)(this["PointsTo3DPolyline_MinimumPointDistance"]));
            }
            set {
                this["PointsTo3DPolyline_MinimumPointDistance"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.5")]
        public double PointsTo3DPolyline_MaximumPointDistance {
            get {
                return ((double)(this["PointsTo3DPolyline_MaximumPointDistance"]));
            }
            set {
                this["PointsTo3DPolyline_MaximumPointDistance"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("c:\\Data\\track.CalculateSurveyCorrection.csv")]
        public string CalculateSurveyCorrection_PathToCsvFile {
            get {
                return ((string)(this["CalculateSurveyCorrection_PathToCsvFile"]));
            }
            set {
                this["CalculateSurveyCorrection_PathToCsvFile"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(";")]
        public string CalculateSurveyCorrection_CSVFile_Delimiter {
            get {
                return ((string)(this["CalculateSurveyCorrection_CSVFile_Delimiter"]));
            }
            set {
                this["CalculateSurveyCorrection_CSVFile_Delimiter"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("c:\\Data\\track.DistanceBetween2Polylines.csv")]
        public string DistanceBetween2Polylines_PathToCsvFile {
            get {
                return ((string)(this["DistanceBetween2Polylines_PathToCsvFile"]));
            }
            set {
                this["DistanceBetween2Polylines_PathToCsvFile"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(";")]
        public string DistanceBetween2Polylines_CSVFile_Delimiter {
            get {
                return ((string)(this["DistanceBetween2Polylines_CSVFile_Delimiter"]));
            }
            set {
                this["DistanceBetween2Polylines_CSVFile_Delimiter"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(",")]
        public string NumberDecimalSeperator_ForAllCSVFiles {
            get {
                return ((string)(this["NumberDecimalSeperator_ForAllCSVFiles"]));
            }
            set {
                this["NumberDecimalSeperator_ForAllCSVFiles"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.001")]
        public double CalculateSurveyCorrection_MinimumCantValue {
            get {
                return ((double)(this["CalculateSurveyCorrection_MinimumCantValue"]));
            }
            set {
                this["CalculateSurveyCorrection_MinimumCantValue"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.005")]
        public double SYMPPL_TOLERANCE {
            get {
                return ((double)(this["SYMPPL_TOLERANCE"]));
            }
            set {
                this["SYMPPL_TOLERANCE"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool SYMPPL_HIGH_PRICISION {
            get {
                return ((bool)(this["SYMPPL_HIGH_PRICISION"]));
            }
            set {
                this["SYMPPL_HIGH_PRICISION"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("5")]
        public double WeedPolyline_MinDistance {
            get {
                return ((double)(this["WeedPolyline_MinDistance"]));
            }
            set {
                this["WeedPolyline_MinDistance"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("WeededPolyline")]
        public string WeedPolyline_LayerName {
            get {
                return ((string)(this["WeedPolyline_LayerName"]));
            }
            set {
                this["WeedPolyline_LayerName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("100")]
        public short WeedPolyline_LayerColor {
            get {
                return ((short)(this["WeedPolyline_LayerColor"]));
            }
            set {
                this["WeedPolyline_LayerColor"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Angle1")]
        public string AlignAngleOfBlock_DynamicPropertyName {
            get {
                return ((string)(this["AlignAngleOfBlock_DynamicPropertyName"]));
            }
            set {
                this["AlignAngleOfBlock_DynamicPropertyName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public double AlignAngleOfBlock_AddAngleValue_TimesPI {
            get {
                return ((double)(this["AlignAngleOfBlock_AddAngleValue_TimesPI"]));
            }
            set {
                this["AlignAngleOfBlock_AddAngleValue_TimesPI"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("HM 45 000~100")]
        public string IncrementAttribute_Pattern {
            get {
                return ((string)(this["IncrementAttribute_Pattern"]));
            }
            set {
                this["IncrementAttribute_Pattern"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("HM_XX_XXX")]
        public string IncrementAttribute_Name {
            get {
                return ((string)(this["IncrementAttribute_Name"]));
            }
            set {
                this["IncrementAttribute_Name"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IncrementAttribute_ChainAlign {
            get {
                return ((bool)(this["IncrementAttribute_ChainAlign"]));
            }
            set {
                this["IncrementAttribute_ChainAlign"] = value;
            }
        }
    }
}
