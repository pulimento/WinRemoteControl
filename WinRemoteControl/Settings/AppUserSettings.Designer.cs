﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WinRemoteControl.Settings {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.3.0.0")]
    internal sealed partial class AppUserSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static AppUserSettings defaultInstance = ((AppUserSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new AppUserSettings())));
        
        public static AppUserSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool LAUNCH_AT_LOGON {
            get {
                return ((bool)(this["LAUNCH_AT_LOGON"]));
            }
            set {
                this["LAUNCH_AT_LOGON"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool AUTO_CONNECT_AT_STARTUP {
            get {
                return ((bool)(this["AUTO_CONNECT_AT_STARTUP"]));
            }
            set {
                this["AUTO_CONNECT_AT_STARTUP"] = value;
            }
        }
    }
}