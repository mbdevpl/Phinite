﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18034
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Phinite.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "11.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("SumatraPDF\\SumatraPDF.exe")]
        public string PdfViewerInternal {
            get {
                return ((string)(this["PdfViewerInternal"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("SumatraPDF\\SumatraPDF.exe")]
        public string PdfViewer {
            get {
                return ((string)(this["PdfViewer"]));
            }
            set {
                this["PdfViewer"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MiKTeX\\miktex\\bin\\pdflatex")]
        public string Pdflatex {
            get {
                return ((string)(this["Pdflatex"]));
            }
            set {
                this["Pdflatex"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int PdflatexInUse {
            get {
                return ((int)(this["PdflatexInUse"]));
            }
            set {
                this["PdflatexInUse"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MiKTeX\\miktex\\bin\\pdflatex")]
        public string PdflatexInternal {
            get {
                return ((string)(this["PdflatexInternal"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int PdfViewerInUse {
            get {
                return ((int)(this["PdfViewerInUse"]));
            }
            set {
                this["PdfViewerInUse"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("20")]
        public int PdflatexTimeoutDefault {
            get {
                return ((int)(this["PdflatexTimeoutDefault"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("pdflatex")]
        public string PdflatexExternal {
            get {
                return ((string)(this["PdflatexExternal"]));
            }
            set {
                this["PdflatexExternal"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int PdflatexTimeoutInUse {
            get {
                return ((int)(this["PdflatexTimeoutInUse"]));
            }
            set {
                this["PdflatexTimeoutInUse"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("15")]
        public int PdflatexTimeout {
            get {
                return ((int)(this["PdflatexTimeout"]));
            }
            set {
                this["PdflatexTimeout"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int LayoutCreationFrequency {
            get {
                return ((int)(this["LayoutCreationFrequency"]));
            }
            set {
                this["LayoutCreationFrequency"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("20")]
        public int LayoutCreationFrequencyDefault {
            get {
                return ((int)(this["LayoutCreationFrequencyDefault"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int LayoutCreationFrequencyInUse {
            get {
                return ((int)(this["LayoutCreationFrequencyInUse"]));
            }
            set {
                this["LayoutCreationFrequencyInUse"] = value;
            }
        }
    }
}
