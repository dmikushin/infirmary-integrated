﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using II;
using II.Localization;

namespace II_Windows {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PatientEditor : Window {

        // Define WPF UI commands for binding
        private ICommand icLoadFile, icSaveFile;
        public ICommand IC_LoadFile { get { return icLoadFile; } }
        public ICommand IC_SaveFile { get { return icSaveFile; } }

        public PatientEditor () {
            InitializeComponent ();
            DataContext = this;
            App.Patient_Editor = this;

            InitLanguage ();
            InitInterface ();
            InitPatient ();

            if (App.Start_Args.Length > 0)
                LoadOpen (App.Start_Args [0]);

            // For debugging. Automatically open window being worked on, hide patient editor.
            //InitDeviceECG ();
            //WindowState = WindowState.Minimized;
        }

        private void InitLanguage () {
            string setLang = Properties.Settings.Default.Language;

            if (setLang == null || setLang == ""
                || !Enum.TryParse<Languages.Values>(setLang, out App.Language.Value)) {
                App.Language = new Languages ();
                DialogLanguage ();
            }
        }

        private void InitInterface () {
            // Initiate ICommands for KeyBindings
            icLoadFile = new ActionCommand (() => LoadFile ());
            icSaveFile = new ActionCommand (() => SaveFile ());

            // Populate UI strings per language selection
            Languages.Values l = App.Language.Value;

            wdwPatientEditor.Title = Strings.Lookup (l, "PE:WindowTitle");
            menuFile.Header = Strings.Lookup (l, "PE:MenuFile");
            menuLoad.Header = Strings.Lookup (l, "PE:MenuLoadSimulation");
            menuSave.Header = Strings.Lookup (l, "PE:MenuSaveSimulation");
            menuExit.Header = Strings.Lookup (l, "PE:MenuExitProgram");

            menuSettings.Header = Strings.Lookup (l, "PE:MenuSettings");
            menuSetLanguage.Header = Strings.Lookup (l, "PE:MenuSetLanguage");

            menuHelp.Header = Strings.Lookup (l, "PE:MenuHelp");
            menuAbout.Header = Strings.Lookup (l, "PE:MenuAboutProgram");

            lblGroupDevices.Content = Strings.Lookup (l, "PE:Devices");
            lblDeviceMonitor.Content = Strings.Lookup (l, "PE:CardiacMonitor");
            lblDevice12LeadECG.Content = Strings.Lookup (l, "PE:12LeadECG");
            lblDeviceDefibrillator.Content = Strings.Lookup (l, "PE:Defibrillator");
            lblDeviceVentilator.Content = Strings.Lookup (l, "PE:Ventilator");
            lblDeviceIABP.Content = Strings.Lookup (l, "PE:IABP");
            lblDeviceCardiotocograph.Content = Strings.Lookup (l, "PE:Cardiotocograph");
            lblDeviceIVPump.Content = Strings.Lookup (l, "PE:IVPump");
            lblDeviceLabResults.Content = Strings.Lookup (l, "PE:LabResults");

            lblGroupVitalSigns.Content = Strings.Lookup (l, "PE:VitalSigns");
            lblHR.Content = String.Format ("{0}:", Strings.Lookup (l, "PE:HeartRate"));
            lblNIBP.Content = String.Format ("{0}:", Strings.Lookup (l, "PE:BloodPressure"));
            lblRR.Content = String.Format ("{0}:", Strings.Lookup (l, "PE:RespiratoryRate"));
            lblSPO2.Content = String.Format ("{0}:", Strings.Lookup (l, "PE:PulseOximetry"));
            lblT.Content = String.Format("{0}:", Strings.Lookup (l, "PE:Temperature"));
            lblCardiacRhythm.Content = String.Format ("{0}:", Strings.Lookup (l, "PE:CardiacRhythm"));
            lblRespiratoryRhythm.Content = String.Format ("{0}:", Strings.Lookup (l, "PE:RespiratoryRhythm"));
            checkDefaultVitals.Content = Strings.Lookup (l, "PE:UseDefaultVitalSignRanges");

            lblGroupHemodynamics.Content = Strings.Lookup (l, "PE:AdvancedHemodynamics");
            lblETCO2.Content = String.Format ("{0}:", Strings.Lookup (l, "PE:EndTidalCO2"));
            lblCVP.Content = String.Format ("{0}:", Strings.Lookup (l, "PE:CentralVenousPressure"));
            lblASBP.Content = String.Format ("{0}:", Strings.Lookup (l, "PE:ArterialBloodPressure"));
            lblPSP.Content = String.Format ("{0}:", Strings.Lookup (l, "PE:PulmonaryArteryPressure"));

            lblGroupRespiratoryProfile.Content = Strings.Lookup (l, "PE:RespiratoryProfile");
            lblInspiratoryRatio.Content = String.Format ("{0}:", Strings.Lookup (l, "PE:InspiratoryExpiratoryRatio"));

            lblGroupCardiacProfile.Content = Strings.Lookup (l, "PE:CardiacProfile");
            grpSTSegmentElevation.Header = Strings.Lookup (l, "PE:STSegmentElevation");
            grpTWaveElevation.Header = Strings.Lookup (l, "PE:TWaveElevation");

            lblParametersApply.Content = Strings.Lookup (l, "BUTTON:ApplyChanges");
            lblParametersReset.Content = Strings.Lookup (l, "BUTTON:ResetParameters");

            List<string> stringsCardiacRhythms = new List<string> (),
                            stringsRespiratoryRhythms = new List<string> ();

            foreach (Cardiac_Rhythms.Values v in Enum.GetValues (typeof (Cardiac_Rhythms.Values)))
                stringsCardiacRhythms.Add (Strings.Lookup (l, Cardiac_Rhythms.LookupString (v)));
            comboCardiacRhythm.ItemsSource = stringsCardiacRhythms;

            foreach (Respiratory_Rhythms.Values v in Enum.GetValues (typeof (Respiratory_Rhythms.Values)))
                stringsRespiratoryRhythms.Add (Strings.Lookup (l, Respiratory_Rhythms.LookupString (v)));
            comboRespiratoryRhythm.ItemsSource = stringsRespiratoryRhythms;
        }

        private void InitPatient () {
            App.Patient = new Patient ();

            App.Timer_Main.Tick += App.Patient.Timers_Process;
            App.Patient.PatientEvent += FormUpdateFields;
            FormUpdateFields (this, new Patient.PatientEvent_Args (App.Patient, Patient.PatientEvent_Args.EventTypes.Vitals_Change));
        }

        private void InitDeviceMonitor () {
            if (App.Device_Monitor == null || !App.Device_Monitor.IsLoaded)
                App.Device_Monitor = new DeviceMonitor ();

            App.Device_Monitor.Activate ();
            App.Device_Monitor.Show ();

            App.Patient.PatientEvent += App.Device_Monitor.OnPatientEvent;
        }

        private void InitDeviceECG () {
            if (App.Device_ECG == null || !App.Device_ECG.IsLoaded)
                App.Device_ECG = new DeviceECG ();

            App.Device_ECG.Activate ();
            App.Device_ECG.Show ();

            App.Patient.PatientEvent += App.Device_ECG.OnPatientEvent;
        }

        private void DialogLanguage(bool reloadUI = false) {
            App.Dialog_Language = new DialogLanguage ();
            App.Dialog_Language.Activate ();
            App.Dialog_Language.ShowDialog ();

            if (reloadUI)
                InitInterface ();
        }

        private void DialogAbout (bool reloadUI = false) {
            App.Dialog_About = new DialogAbout();
            App.Dialog_About.Activate ();
            App.Dialog_About.ShowDialog ();
        }

        private void LoadOpen (string fileName) {
            if (File.Exists (fileName)) {
                Stream s = new FileStream (fileName, FileMode.Open);
                LoadInit (s);
            } else {
                LoadFail ();
            }
        }

        private void LoadInit (Stream incFile) {
            StreamReader sr = new StreamReader (incFile);

            /* Read savefile metadata indicating data formatting
                * Multiple data formats for forward compatibility
                */
            string metadata = sr.ReadLine ();
            if (metadata.StartsWith (".ii:t1"))
                LoadValidateT1 (sr);
            else
                LoadFail ();
        }

        private void LoadValidateT1 (StreamReader sr) {
            /* Savefile type 1: validated and obfuscated, not encrypted or data protected
                * Line 1 is metadata (.ii:t1)
                * Line 2 is hash for validation (hash taken of raw string data, unobfuscated)
                * Line 3 is savefile data obfuscated by Base64 encoding
                */

            string hash = sr.ReadLine ().Trim ();
            string file = Utility.UnobfuscateB64 (sr.ReadToEnd ().Trim ());
            sr.Close ();

            if (hash == Utility.HashMD5 (file))
                LoadProcess (file);
            else
                LoadFail ();
        }

        private void LoadProcess (string incFile) {
            StringReader sRead = new StringReader (incFile);
            string line, pline;
            StringBuilder pbuffer;

            try {
                while ((line = sRead.ReadLine ()) != null) {
                    if (line == "> Begin: Patient") {
                        pbuffer = new StringBuilder ();
                        while ((pline = sRead.ReadLine ()) != null && pline != "> End: Patient")
                            pbuffer.AppendLine (pline);
                        App.Patient.Load_Process (pbuffer.ToString ());

                    } else if (line == "> Begin: Editor") {
                        pbuffer = new StringBuilder ();
                        while ((pline = sRead.ReadLine ()) != null && pline != "> End: Editor")
                            pbuffer.AppendLine (pline);
                        this.LoadOptions (pbuffer.ToString ());

                    } else if (line == "> Begin: Cardiac Monitor") {
                        pbuffer = new StringBuilder ();
                        while ((pline = sRead.ReadLine ()) != null && pline != "> End: Cardiac Monitor")
                            pbuffer.AppendLine (pline);

                        InitDeviceMonitor ();
                        App.Device_Monitor.Load_Process (pbuffer.ToString ());
                    }
                }
            } catch {
                LoadFail ();
            }
            sRead.Close ();
        }

        private void LoadFail () {
            MessageBox.Show (
                "The selected file was unable to be loaded. Perhaps the file was damaged or edited outside of Infirmary Integrated.",
                "Unable to Load File", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void SaveT1 (Stream s) {
            StringBuilder sb = new StringBuilder ();

            sb.AppendLine ("> Begin: Patient");
            sb.Append (App.Patient.Save ());
            sb.AppendLine ("> End: Patient");

            sb.AppendLine ("> Begin: Editor");
            sb.Append (this.SaveOptions ());
            sb.AppendLine ("> End: Editor");

            // Imp: Reference cardiac monitor for save data

            if (App.Device_Monitor != null && App.Device_Monitor.IsLoaded) {
                sb.AppendLine ("> Begin: Cardiac Monitor");
                sb.Append (App.Device_Monitor.Save ());
                sb.AppendLine ("> End: Cardiac Monitor");
            }


            StreamWriter sw = new StreamWriter (s);
            sw.WriteLine (".ii:t1");                                // Metadata (type 1 savefile)
            sw.WriteLine (Utility.HashMD5 (sb.ToString ().Trim ()));      // Hash for validation
            sw.Write (Utility.ObfuscateB64 (sb.ToString ().Trim ()));     // Savefile data obfuscated with Base64
            sw.Close ();
            s.Close ();
        }

        private void LoadOptions (string inc) {
            StringReader sRead = new StringReader (inc);

            try {
                string line;
                while ((line = sRead.ReadLine ()) != null) {
                    if (line.Contains (":")) {
                        string pName = line.Substring (0, line.IndexOf (':')),
                                pValue = line.Substring (line.IndexOf (':') + 1);
                        switch (pName) {
                            default: break;
                            case "checkDefaultVitals": checkDefaultVitals.IsChecked = bool.Parse (pValue); break;
                        }
                    }
                }
            } catch {
                sRead.Close ();
                return;
            }

            sRead.Close ();
        }

        private string SaveOptions () {
            StringBuilder sWrite = new StringBuilder ();

            sWrite.AppendLine (String.Format ("{0}:{1}", "checkDefaultVitals", checkDefaultVitals.IsChecked));

            return sWrite.ToString ();
        }

        private void LoadFile () {
            Stream s;
            Microsoft.Win32.OpenFileDialog dlgLoad = new Microsoft.Win32.OpenFileDialog ();

            dlgLoad.Filter = "Infirmary Integrated simulation files (*.ii)|*.ii|All files (*.*)|*.*";
            dlgLoad.FilterIndex = 1;
            dlgLoad.RestoreDirectory = true;

            if (dlgLoad.ShowDialog () == true) {
                if ((s = dlgLoad.OpenFile ()) != null) {
                    LoadInit (s);
                    s.Close ();
                }
            }
        }

        private void SaveFile () {
            Stream s;
            Microsoft.Win32.SaveFileDialog dlgSave = new Microsoft.Win32.SaveFileDialog ();

            dlgSave.Filter = "Infirmary Integrated simulation files (*.ii)|*.ii|All files (*.*)|*.*";
            dlgSave.FilterIndex = 1;
            dlgSave.RestoreDirectory = true;

            if (dlgSave.ShowDialog () == true) {
                if ((s = dlgSave.OpenFile ()) != null) {
                    SaveT1 (s);
                }
            }
        }

        public bool RequestExit () {
            Application.Current.Shutdown ();
            return true;
        }

        public Patient RequestNewPatient () {
            InitPatient ();
            return App.Patient;
        }

        private void MenuLoadFile_Click (object s, RoutedEventArgs e) => LoadFile ();
        private void MenuSaveFile_Click (object s, RoutedEventArgs e) => SaveFile ();
        private void MenuExit_Click (object s, RoutedEventArgs e) => RequestExit ();
        private void MenuSetLanguage_Click (object s, RoutedEventArgs e) => DialogLanguage (true);
        private void MenuAbout_Click (object s, RoutedEventArgs e) => DialogAbout ();
        private void ButtonDeviceMonitor_Click (object s, RoutedEventArgs e) => InitDeviceMonitor ();
        private void ButtonDeviceECG_Click (object s, RoutedEventArgs e) => InitDeviceECG ();
        private void ButtonResetParameters_Click (object s, RoutedEventArgs e) => RequestNewPatient ();

        private void ButtonApplyParameters_Click (object sender, RoutedEventArgs e) {
            App.Patient.UpdateVitals (
                (int)numHR.Value,
                (int)numRR.Value,
                (int)numSPO2.Value,
                (int)numT.Value,
                (int)numCVP.Value,
                (int)numETCO2.Value,

                (int)numNSBP.Value,
                (int)numNDBP.Value,
                Patient.CalculateMAP ((int)numNSBP.Value, (int)numNDBP.Value),

                (int)numASBP.Value,
                (int)numADBP.Value,
                Patient.CalculateMAP ((int)numASBP.Value, (int)numADBP.Value),

                (int)numPSP.Value,
                (int)numPDP.Value,
                Patient.CalculateMAP ((int)numPSP.Value, (int)numPDP.Value),

                new double [] {
                (double)numSTE_I.Value, (double)numSTE_II.Value, (double)numSTE_III.Value,
                (double)numSTE_aVR.Value, (double)numSTE_aVL.Value, (double)numSTE_aVF.Value,
                (double)numSTE_V1.Value, (double)numSTE_V2.Value, (double)numSTE_V3.Value,
                (double)numSTE_V4.Value, (double)numSTE_V5.Value, (double)numSTE_V6.Value
                },
                new double [] {
                (double)numTWE_I.Value, (double)numTWE_II.Value, (double)numTWE_III.Value,
                (double)numTWE_aVR.Value, (double)numTWE_aVL.Value, (double)numTWE_aVF.Value,
                (double)numTWE_V1.Value, (double)numTWE_V2.Value, (double)numTWE_V3.Value,
                (double)numTWE_V4.Value, (double)numTWE_V5.Value, (double)numTWE_V6.Value
                },

                (Cardiac_Rhythms.Values)Enum.GetValues(typeof(Cardiac_Rhythms.Values)).GetValue(comboCardiacRhythm.SelectedIndex),
                (Respiratory_Rhythms.Values)Enum.GetValues (typeof (Respiratory_Rhythms.Values)).GetValue (comboRespiratoryRhythm.SelectedIndex),

                (int)numInspiratoryRatio.Value,
                (int)numExpiratoryRatio.Value
            );
        }

        private void FormUpdateFields (object sender, Patient.PatientEvent_Args e) {
            if (e.EventType == Patient.PatientEvent_Args.EventTypes.Vitals_Change) {
                numHR.Value = e.Patient.HR;
                numRR.Value = e.Patient.RR;
                numSPO2.Value = e.Patient.SPO2;
                numT.Value = (decimal)e.Patient.T;
                numCVP.Value = e.Patient.CVP;
                numETCO2.Value = e.Patient.ETCO2;

                numNSBP.Value = e.Patient.NSBP;
                numNDBP.Value = e.Patient.NDBP;
                numASBP.Value = e.Patient.ASBP;
                numADBP.Value = e.Patient.ADBP;
                numPSP.Value = e.Patient.PSP;
                numPDP.Value = e.Patient.PDP;

                comboCardiacRhythm.SelectedIndex = (int)e.Patient.Cardiac_Rhythm.Value;

                comboRespiratoryRhythm.SelectedIndex = (int)e.Patient.Respiratory_Rhythm.Value;
                numInspiratoryRatio.Value = e.Patient.Respiratory_IERatio_I;
                numExpiratoryRatio.Value = e.Patient.Respiratory_IERatio_E;


                numSTE_I.Value = (decimal)e.Patient.ST_Elevation [(int)Leads.Values.ECG_I];
                numSTE_II.Value = (decimal)e.Patient.ST_Elevation [(int)Leads.Values.ECG_II];
                numSTE_III.Value = (decimal)e.Patient.ST_Elevation [(int)Leads.Values.ECG_III];
                numSTE_aVR.Value = (decimal)e.Patient.ST_Elevation [(int)Leads.Values.ECG_AVR];
                numSTE_aVL.Value = (decimal)e.Patient.ST_Elevation [(int)Leads.Values.ECG_AVL];
                numSTE_aVF.Value = (decimal)e.Patient.ST_Elevation [(int)Leads.Values.ECG_AVF];
                numSTE_V1.Value = (decimal)e.Patient.ST_Elevation [(int)Leads.Values.ECG_V1];
                numSTE_V2.Value = (decimal)e.Patient.ST_Elevation [(int)Leads.Values.ECG_V2];
                numSTE_V3.Value = (decimal)e.Patient.ST_Elevation [(int)Leads.Values.ECG_V3];
                numSTE_V4.Value = (decimal)e.Patient.ST_Elevation [(int)Leads.Values.ECG_V4];
                numSTE_V5.Value = (decimal)e.Patient.ST_Elevation [(int)Leads.Values.ECG_V5];
                numSTE_V6.Value = (decimal)e.Patient.ST_Elevation [(int)Leads.Values.ECG_V6];

                numTWE_I.Value = (decimal)e.Patient.T_Elevation [(int)Leads.Values.ECG_I];
                numTWE_II.Value = (decimal)e.Patient.T_Elevation [(int)Leads.Values.ECG_II];
                numTWE_III.Value = (decimal)e.Patient.T_Elevation [(int)Leads.Values.ECG_III];
                numTWE_aVR.Value = (decimal)e.Patient.T_Elevation [(int)Leads.Values.ECG_AVR];
                numTWE_aVL.Value = (decimal)e.Patient.T_Elevation [(int)Leads.Values.ECG_AVL];
                numTWE_aVF.Value = (decimal)e.Patient.T_Elevation [(int)Leads.Values.ECG_AVF];
                numTWE_V1.Value = (decimal)e.Patient.T_Elevation [(int)Leads.Values.ECG_V1];
                numTWE_V2.Value = (decimal)e.Patient.T_Elevation [(int)Leads.Values.ECG_V2];
                numTWE_V3.Value = (decimal)e.Patient.T_Elevation [(int)Leads.Values.ECG_V3];
                numTWE_V4.Value = (decimal)e.Patient.T_Elevation [(int)Leads.Values.ECG_V4];
                numTWE_V5.Value = (decimal)e.Patient.T_Elevation [(int)Leads.Values.ECG_V5];
                numTWE_V6.Value = (decimal)e.Patient.T_Elevation [(int)Leads.Values.ECG_V6];
            }
        }

        private void OnRhythmSelected (object sender, SelectionChangedEventArgs e) {
            if ((bool)checkDefaultVitals.IsChecked && App.Patient != null) {

                int si = comboCardiacRhythm.SelectedIndex;
                Array ev = Enum.GetValues (typeof (Cardiac_Rhythms.Values));
                if (si < 0 || si > ev.Length - 1)
                    return;

                Cardiac_Rhythms.Default_Vitals v = Cardiac_Rhythms.DefaultVitals (
                    (Cardiac_Rhythms.Values)ev.GetValue (si));

                numHR.Value = (int)Utility.Clamp ((double)numHR.Value, v.HRMin, v.HRMax);
                numSPO2.Value = (int)Utility.Clamp ((double)numSPO2.Value, v.SPO2Min, v.SPO2Max);
                numETCO2.Value = (int)Utility.Clamp ((double)numETCO2.Value, v.ETCO2Min, v.ETCO2Max);
                numNSBP.Value = (int)Utility.Clamp ((double)numNSBP.Value, v.SBPMin, v.SBPMax);
                numNDBP.Value = (int)Utility.Clamp ((double)numNDBP.Value, v.DBPMin, v.DBPMax);
                numASBP.Value = (int)Utility.Clamp ((double)numASBP.Value, v.SBPMin, v.SBPMax);
                numADBP.Value = (int)Utility.Clamp ((double)numADBP.Value, v.DBPMin, v.DBPMax);
                numPSP.Value = (int)Utility.Clamp ((double)numPSP.Value, v.PSPMin, v.PSPMax);
                numPDP.Value = (int)Utility.Clamp ((double)numPDP.Value, v.PDPMin, v.PDPMax);
            }
        }

        private void OnClosed (object sender, EventArgs e) => RequestExit ();

    }
}